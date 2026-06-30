using System.Text.Json;
using GuidedMentor.Content.Application.Plugins.Dtos;
using GuidedMentor.Content.Application.Services;
using GuidedMentor.Content.Domain;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace GuidedMentor.Content.Application.Plugins;

/// <summary>
/// Semantic Kernel plugin that generates personalised session plans using
/// the IChatClient abstraction (backed by Amazon Bedrock Converse API with Claude Sonnet 4).
/// 
/// Validates: Requirements 7.1, 7.2, 7.3, 7.4, 17.1, 17.2, 17.3, 17.5
/// </summary>
public sealed class SessionPlanPlugin
{
    /// <summary>
    /// Maximum number of generation attempts before giving up.
    /// </summary>
    public const int MaxAttempts = 3;

    private readonly IChatClient _chatClient;
    private readonly ILogger<SessionPlanPlugin> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Token usage from the last successful generation attempt.
    /// </summary>
    public int LastInputTokens { get; private set; }

    /// <summary>
    /// Token usage from the last successful generation attempt.
    /// </summary>
    public int LastOutputTokens { get; private set; }

    /// <summary>
    /// The Bedrock model version used for the last generation.
    /// Tracked per ISO 42001 requirement for AI decision audit trail.
    /// </summary>
    public string LastModelVersion { get; private set; } = string.Empty;

    public SessionPlanPlugin(IChatClient chatClient, ILogger<SessionPlanPlugin> logger)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a structured session plan by invoking the AI model via IChatClient.
    /// Sanitizes all user-provided inputs, constructs a prompt with typed variables,
    /// parses the structured JSON response, and validates it against the SessionPlan schema.
    /// Retries up to <see cref="MaxAttempts"/> times on validation failure.
    /// </summary>
    /// <param name="mentee">The mentee's profile data.</param>
    /// <param name="mentor">The mentor's profile data.</param>
    /// <param name="goals">Additional mentee goals description.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A valid SessionPlan, or null if all attempts fail.</returns>
    public async Task<SessionPlan?> GeneratePlanAsync(
        MenteeProfileDto mentee,
        MentorProfileDto mentor,
        string goals,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(mentee);
        ArgumentNullException.ThrowIfNull(mentor);

        var sanitizedGoals = InputSanitizer.Sanitize(goals);
        var prompt = BuildPrompt(mentee, mentor, sanitizedGoals);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            _logger.LogInformation(
                "Generating session plan, attempt {Attempt}/{MaxAttempts}",
                attempt, MaxAttempts);

            try
            {
                var response = await InvokeChatClientAsync(prompt, ct);

                if (string.IsNullOrWhiteSpace(response))
                {
                    _logger.LogWarning(
                        "Empty response from AI model on attempt {Attempt}", attempt);
                    continue;
                }

                var plan = ParseResponse(response);

                if (plan is null)
                {
                    _logger.LogWarning(
                        "Failed to parse session plan JSON on attempt {Attempt}", attempt);
                    continue;
                }

                if (!plan.IsValid())
                {
                    _logger.LogWarning(
                        "Session plan failed validation on attempt {Attempt}. " +
                        "AgendaCount={AgendaCount}, AgendaSum={AgendaSum}",
                        attempt,
                        plan.Agenda.Count,
                        plan.Agenda.Sum(a => a.DurationMinutes));
                    continue;
                }

                _logger.LogInformation(
                    "Session plan generated successfully on attempt {Attempt}", attempt);
                return plan;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Exception during session plan generation on attempt {Attempt}",
                    attempt);

                if (attempt == MaxAttempts)
                    throw;
            }
        }

        _logger.LogError(
            "All {MaxAttempts} attempts to generate a valid session plan have failed",
            MaxAttempts);
        return null;
    }

    /// <summary>
    /// Builds the prompt with typed input variables, structured to produce a JSON session plan.
    /// </summary>
    internal static string BuildPrompt(
        MenteeProfileDto mentee,
        MentorProfileDto mentor,
        string sanitizedGoals)
    {
        return $"""
            You are an expert mentorship session planner for the AWS Community GuidedMentor platform.
            Generate a personalised 35-minute mentorship session plan based on the profiles below.

            ## Mentee Profile
            - Name: {InputSanitizer.Sanitize(mentee.DisplayName)}
            - Chapter: {InputSanitizer.Sanitize(mentee.Chapter)}
            - Skills: {string.Join(", ", mentee.Skills.Select(s => InputSanitizer.Sanitize(s)))}
            - Experience Level: {InputSanitizer.Sanitize(mentee.ExperienceLevel)}
            - Years of Experience: {mentee.YearsOfExperience}
            - Primary Goal: {InputSanitizer.Sanitize(mentee.PrimaryGoal)}
            - Goal Description: {InputSanitizer.Sanitize(mentee.GoalDescription)}
            - Preferred Duration: {InputSanitizer.Sanitize(mentee.PreferredDuration)}

            ## Mentor Profile
            - Name: {InputSanitizer.Sanitize(mentor.DisplayName)}
            - Chapter: {InputSanitizer.Sanitize(mentor.Chapter)}
            - Professional Title: {InputSanitizer.Sanitize(mentor.ProfessionalTitle)}
            - Company: {InputSanitizer.Sanitize(mentor.CompanyName)}
            - Expertise Areas: {string.Join(", ", mentor.ExpertiseAreas.Select(e => InputSanitizer.Sanitize(e)))}
            - Topics: {string.Join(", ", mentor.Topics.Select(t => InputSanitizer.Sanitize(t)))}
            - Years of Experience: {mentor.YearsOfExperience}

            ## Mentee Goals
            {sanitizedGoals}

            ## Instructions
            Generate a structured session plan as a JSON object with the following schema:
            - sessionTitle: A concise title for the session (max 100 characters)
            - agenda: An array of 3-7 items, each with:
              - title: Short title for the agenda item
              - durationMinutes: Integer minutes allocated (minimum 3 per item)
              - description: Brief description (max 500 characters)
            - preworkTasks: An array of 2-5 pre-work tasks for the mentee (each max 200 characters)
            - followUpTasks: An array of 2-5 follow-up tasks (each max 200 characters)

            CRITICAL CONSTRAINTS:
            - The sum of all agenda item durationMinutes MUST equal exactly 35 minutes.
            - Each agenda item MUST have a durationMinutes of at least 3.
            - The agenda MUST have between 3 and 7 items inclusive.
            - preworkTasks MUST have between 2 and 5 items.
            - followUpTasks MUST have between 2 and 5 items.

            Respond ONLY with the JSON object. No markdown formatting, no code blocks, no additional text.
            """;
    }

    /// <summary>
    /// Invokes the IChatClient with the constructed prompt and returns the raw response.
    /// Also captures token usage metadata and model version from the response.
    /// </summary>
    private async Task<string?> InvokeChatClientAsync(string prompt, CancellationToken ct)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, prompt)
        };

        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: ct);

        // Capture token usage from response metadata for CloudWatch metrics
        if (response.Usage is not null)
        {
            LastInputTokens = (int)(response.Usage.InputTokenCount ?? 0);
            LastOutputTokens = (int)(response.Usage.OutputTokenCount ?? 0);

            _logger.LogInformation(
                "Bedrock token usage — Input: {InputTokens}, Output: {OutputTokens}",
                LastInputTokens, LastOutputTokens);
        }

        // Capture model version for ISO 42001 compliance (model version tracking)
        LastModelVersion = response.ModelId ?? "unknown";

        _logger.LogInformation(
            "Bedrock model version used: {ModelVersion}",
            LastModelVersion);

        return response.Text;
    }

    /// <summary>
    /// Parses the AI response JSON into a SessionPlan domain object.
    /// Returns null if parsing fails.
    /// </summary>
    internal static SessionPlan? ParseResponse(string response)
    {
        try
        {
            // Strip any markdown code block wrappers the model may include
            var json = ExtractJson(response);

            var dto = JsonSerializer.Deserialize<SessionPlanJsonDto>(json, JsonOptions);

            if (dto is null)
                return null;

            var agendaItems = dto.Agenda
                .Select(a => new AgendaItem(
                    a.Title ?? string.Empty,
                    a.DurationMinutes,
                    a.Description ?? string.Empty))
                .ToList();

            return new SessionPlan(
                dto.SessionTitle ?? string.Empty,
                agendaItems,
                dto.PreworkTasks ?? [],
                dto.FollowUpTasks ?? []);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts JSON content from a response that may be wrapped in markdown code blocks.
    /// </summary>
    internal static string ExtractJson(string response)
    {
        var trimmed = response.Trim();

        // Handle ```json ... ``` wrapper
        if (trimmed.StartsWith("```"))
        {
            var firstNewLine = trimmed.IndexOf('\n');
            if (firstNewLine > 0)
                trimmed = trimmed[(firstNewLine + 1)..];

            if (trimmed.EndsWith("```"))
                trimmed = trimmed[..^3];

            return trimmed.Trim();
        }

        return trimmed;
    }

    /// <summary>
    /// Internal DTO for JSON deserialization from AI response.
    /// </summary>
    internal sealed class SessionPlanJsonDto
    {
        public string? SessionTitle { get; set; }
        public List<AgendaItemJsonDto> Agenda { get; set; } = [];
        public List<string> PreworkTasks { get; set; } = [];
        public List<string> FollowUpTasks { get; set; } = [];
    }

    /// <summary>
    /// Internal DTO for agenda item JSON deserialization.
    /// </summary>
    internal sealed class AgendaItemJsonDto
    {
        public string? Title { get; set; }
        public int DurationMinutes { get; set; }
        public string? Description { get; set; }
    }
}
