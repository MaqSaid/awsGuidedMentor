using FluentValidation;

namespace GuidedMentor.Identity.Application.Commands.Admin;

/// <summary>
/// Validates ToggleFeatureFlagCommand: adminId required, featureName must be one of allowed values,
/// reason 10-500 chars.
/// </summary>
public sealed class ToggleFeatureFlagCommandValidator : AbstractValidator<ToggleFeatureFlagCommand>
{
    /// <summary>
    /// The set of allowed feature flag names that can be toggled by Super Admins.
    /// </summary>
    public static readonly IReadOnlySet<string> AllowedFeatureFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "ai_help",
        "job_board",
        "meetup_calendar",
        "session_plans"
    };

    public ToggleFeatureFlagCommandValidator()
    {
        RuleFor(x => x.AdminId)
            .NotEmpty().WithMessage("Admin ID is required.");

        RuleFor(x => x.FeatureName)
            .NotEmpty().WithMessage("Feature name is required.")
            .Must(BeAnAllowedFeatureFlag)
                .WithMessage($"Feature name must be one of: {string.Join(", ", AllowedFeatureFlags)}.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A reason is required for feature flag changes.")
            .MinimumLength(10).WithMessage("Reason must be at least 10 characters.")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.");
    }

    private static bool BeAnAllowedFeatureFlag(string featureName) =>
        !string.IsNullOrWhiteSpace(featureName) && AllowedFeatureFlags.Contains(featureName);
}
