using GuidedMentor.Content.Application.Services;
using GuidedMentor.Content.Domain;

namespace GuidedMentor.Content.Tests;

/// <summary>
/// Unit tests for OutputValidator — validates AI-generated output before persistence.
/// Ensures no PII present, no harmful content, and schema conformance.
/// Validates: Requirements 7.11, 7.12, 21.17
/// </summary>
public sealed class OutputValidatorTests
{
    private readonly OutputValidator _sut = new();

    // ── Helper Methods ──

    private static SessionPlan CreateValidPlan(
        string? sessionTitle = null,
        IReadOnlyList<AgendaItem>? agenda = null,
        IReadOnlyList<string>? preworkTasks = null,
        IReadOnlyList<string>? followUpTasks = null)
    {
        return new SessionPlan(
            sessionTitle ?? "AWS Lambda Best Practices Mentoring Session",
            agenda ?? CreateValidAgenda(),
            preworkTasks ?? ["Review AWS Lambda documentation", "Set up local dev environment"],
            followUpTasks ?? ["Complete hands-on lab exercise", "Write summary of key learnings"]);
    }

    private static IReadOnlyList<AgendaItem> CreateValidAgenda()
    {
        return
        [
            new AgendaItem("Introduction and Goal Setting", 5, "Discuss session objectives and mentee goals"),
            new AgendaItem("Deep Dive: Lambda Cold Starts", 10, "Explore cold start optimization techniques"),
            new AgendaItem("Hands-on: Provisioned Concurrency", 10, "Configure provisioned concurrency in a sample project"),
            new AgendaItem("Architecture Review", 5, "Review mentee current Lambda architecture"),
            new AgendaItem("Wrap-up and Action Items", 5, "Summarize learnings and define next steps")
        ];
    }

    // ── Valid Plan Tests ──

    [Fact]
    public void Validate_ValidPlan_ReturnsSuccess()
    {
        var plan = CreateValidPlan();

        var result = _sut.Validate(plan);

        result.IsValid.Should().BeTrue();
        result.Violations.Should().BeEmpty();
    }

    // ── Schema Conformance Tests ──

    [Fact]
    public void Validate_EmptySessionTitle_ReturnsSchemaViolation()
    {
        var plan = CreateValidPlan(sessionTitle: "");

        var result = _sut.Validate(plan);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("SessionTitle"));
    }

    [Fact]
    public void Validate_SessionTitleTooLong_ReturnsSchemaViolation()
    {
        var plan = CreateValidPlan(sessionTitle: new string('A', 101));

        var result = _sut.Validate(plan);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("SessionTitle") && v.Contains("100"));
    }

    [Fact]
    public void Validate_AgendaTotalNot35Minutes_ReturnsSchemaViolation()
    {
        var invalidAgenda = new List<AgendaItem>
        {
            new("Item 1", 10, "Description one"),
            new("Item 2", 10, "Description two"),
            new("Item 3", 10, "Description three")
        };
        var plan = CreateValidPlan(agenda: invalidAgenda);

        var result = _sut.Validate(plan);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("35 minutes"));
    }

    [Fact]
    public void Validate_AgendaItemTooShort_ReturnsSchemaViolation()
    {
        var invalidAgenda = new List<AgendaItem>
        {
            new("Item 1", 2, "Too short duration"),
            new("Item 2", 18, "Description two"),
            new("Item 3", 15, "Description three")
        };
        var plan = CreateValidPlan(agenda: invalidAgenda);

        var result = _sut.Validate(plan);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("3 minutes"));
    }

    [Fact]
    public void Validate_TooFewAgendaItems_ReturnsSchemaViolation()
    {
        var invalidAgenda = new List<AgendaItem>
        {
            new("Item 1", 15, "Description"),
            new("Item 2", 20, "Description")
        };
        var plan = CreateValidPlan(agenda: invalidAgenda);

        var result = _sut.Validate(plan);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("3-7"));
    }

    [Fact]
    public void Validate_TooFewPreworkTasks_ReturnsSchemaViolation()
    {
        var plan = CreateValidPlan(preworkTasks: ["Only one task"]);

        var result = _sut.Validate(plan);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("PreworkTasks") && v.Contains("2-5"));
    }

    [Fact]
    public void Validate_TooManyFollowUpTasks_ReturnsSchemaViolation()
    {
        var plan = CreateValidPlan(followUpTasks:
        [
            "Task 1", "Task 2", "Task 3", "Task 4", "Task 5", "Task 6"
        ]);

        var result = _sut.Validate(plan);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("FollowUpTasks") && v.Contains("2-5"));
    }

    // ── PII Detection Tests ──

    [Fact]
    public void Validate_EmailInSessionTitle_ReturnsPiiViolation()
    {
        var plan = CreateValidPlan(sessionTitle: "Session with user@example.com");

        var result = _sut.Validate(plan);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("PII") && v.Contains("email"));
    }

    [Fact]
    public void Validate_PhoneInPreworkTask_ReturnsPiiViolation()
    {
        var plan = CreateValidPlan(preworkTasks:
        [
            "Call mentor at +61 412 345 678 to confirm",
            "Review Lambda documentation"
        ]);

        var result = _sut.Validate(plan);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("PII") && v.Contains("phone"));
    }

    [Fact]
    public void Validate_SsnInDescription_ReturnsPiiViolation()
    {
        var agenda = new List<AgendaItem>
        {
            new("Introduction", 5, "User SSN is 123-45-6789 for reference"),
            new("Deep Dive", 15, "Discuss Lambda architecture patterns"),
            new("Hands-on", 10, "Build a sample Lambda function"),
            new("Wrap-up", 5, "Review and next steps")
        };
        var plan = CreateValidPlan(agenda: agenda);

        var result = _sut.Validate(plan);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("PII") && v.Contains("SSN"));
    }

    [Fact]
    public void Validate_CreditCardInFollowUp_ReturnsPiiViolation()
    {
        var plan = CreateValidPlan(followUpTasks:
        [
            "Use card 4111 1111 1111 1111 for AWS billing",
            "Complete the hands-on lab"
        ]);

        var result = _sut.Validate(plan);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("PII") && v.Contains("credit card"));
    }

    [Fact]
    public void Validate_AddressInAgendaDescription_ReturnsPiiViolation()
    {
        var agenda = new List<AgendaItem>
        {
            new("Introduction", 5, "Meet at 123 Smith Street for the session"),
            new("Deep Dive", 15, "Discuss Lambda architecture patterns"),
            new("Hands-on", 10, "Build a sample Lambda function"),
            new("Wrap-up", 5, "Review and next steps")
        };
        var plan = CreateValidPlan(agenda: agenda);

        var result = _sut.Validate(plan);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("PII") && v.Contains("address"));
    }

    // ── Harmful Content Tests ──

    [Fact]
    public void Validate_HarmfulContentInTitle_ReturnsViolation()
    {
        var plan = CreateValidPlan(sessionTitle: "Session with explicit sexual content");

        var result = _sut.Validate(plan);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("Harmful content"));
    }

    [Fact]
    public void Validate_ThreatInPrework_ReturnsViolation()
    {
        var plan = CreateValidPlan(preworkTasks:
        [
            "Research bomb threat procedures",
            "Review AWS documentation"
        ]);

        var result = _sut.Validate(plan);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Contains("Harmful content"));
    }

    // ── Clean Content Tests ──

    [Fact]
    public void Validate_TechnicalContent_DoesNotFlagAsPii()
    {
        // Technical content like IP addresses, version numbers, etc. should not trigger false positives
        var plan = CreateValidPlan(sessionTitle: "AWS VPC CIDR Blocks and Subnet Design");

        var result = _sut.Validate(plan);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_AwsServiceNames_DoesNotFlagAsHarmful()
    {
        var plan = CreateValidPlan(
            sessionTitle: "AWS Security Hub and GuardDuty Integration",
            preworkTasks:
            [
                "Review AWS Security Hub documentation",
                "Enable GuardDuty in your test account"
            ]);

        var result = _sut.Validate(plan);

        result.IsValid.Should().BeTrue();
    }

    // ── Multiple Violations Test ──

    [Fact]
    public void Validate_MultipleViolations_ReturnsAllViolations()
    {
        var plan = CreateValidPlan(
            sessionTitle: "Contact user@test.com about kill yourself topics",
            preworkTasks: ["Single task only"]);

        var result = _sut.Validate(plan);

        result.IsValid.Should().BeFalse();
        // Should catch: PII (email), harmful content, and schema violation (too few prework)
        result.Violations.Count.Should().BeGreaterThanOrEqualTo(3);
    }
}
