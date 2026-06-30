using GuidedMentor.Content.Domain;

namespace GuidedMentor.Content.Tests;

public class SessionPlanTests
{
    private static AgendaItem ValidItem(int duration) =>
        new("Topic", duration, "A valid description for this agenda item.");

    private static SessionPlan CreateValidPlan() => new(
        sessionTitle: "AWS Lambda Deep Dive",
        agenda:
        [
            new AgendaItem("Introduction", 5, "Meet and greet, set expectations."),
            new AgendaItem("Core Topic", 15, "Deep dive into Lambda cold starts."),
            new AgendaItem("Hands-On", 10, "Walk through a sample deployment."),
            new AgendaItem("Wrap-Up", 5, "Summary and next steps.")
        ],
        preworkTasks: ["Review Lambda documentation", "Set up AWS CLI locally"],
        followUpTasks: ["Deploy sample function", "Write a blog post about learnings"]
    );

    [Fact]
    public void IsValid_WithValidPlan_ReturnsTrue()
    {
        var plan = CreateValidPlan();
        plan.IsValid().Should().BeTrue();
    }

    // --- SessionTitle validation ---

    [Fact]
    public void IsValid_WithEmptyTitle_ReturnsFalse()
    {
        var plan = CreateValidPlan() with { SessionTitle = "" };
        plan.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithWhitespaceTitle_ReturnsFalse()
    {
        var plan = CreateValidPlan() with { SessionTitle = "   " };
        plan.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithTitleExceeding100Chars_ReturnsFalse()
    {
        var plan = CreateValidPlan() with { SessionTitle = new string('A', 101) };
        plan.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithTitleExactly100Chars_ReturnsTrue()
    {
        var plan = CreateValidPlan() with { SessionTitle = new string('A', 100) };
        plan.IsValid().Should().BeTrue();
    }

    // --- Agenda count validation ---

    [Fact]
    public void IsValid_WithLessThan3AgendaItems_ReturnsFalse()
    {
        var plan = CreateValidPlan() with
        {
            Agenda = new List<AgendaItem>
            {
                ValidItem(17),
                ValidItem(18)
            }
        };
        plan.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithMoreThan7AgendaItems_ReturnsFalse()
    {
        var plan = CreateValidPlan() with
        {
            Agenda = new List<AgendaItem>
            {
                ValidItem(5), ValidItem(5), ValidItem(5), ValidItem(5),
                ValidItem(5), ValidItem(5), ValidItem(4), ValidItem(3)
            }
        };
        plan.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithExactly3AgendaItems_SummingTo35_ReturnsTrue()
    {
        var plan = CreateValidPlan() with
        {
            Agenda = new List<AgendaItem>
            {
                ValidItem(12),
                ValidItem(12),
                ValidItem(11)
            }
        };
        plan.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithExactly7AgendaItems_SummingTo35_ReturnsTrue()
    {
        var plan = CreateValidPlan() with
        {
            Agenda = new List<AgendaItem>
            {
                ValidItem(5), ValidItem(5), ValidItem(5), ValidItem(5),
                ValidItem(5), ValidItem(5), ValidItem(5)
            }
        };
        plan.IsValid().Should().BeTrue();
    }

    // --- Agenda duration sum validation ---

    [Fact]
    public void IsValid_WithAgendaSumNot35_ReturnsFalse()
    {
        var plan = CreateValidPlan() with
        {
            Agenda = new List<AgendaItem>
            {
                ValidItem(10),
                ValidItem(10),
                ValidItem(10)
            }
        };
        plan.IsValid().Should().BeFalse();
    }

    // --- Agenda per-item minimum duration ---

    [Fact]
    public void IsValid_WithAgendaItemUnder3Minutes_ReturnsFalse()
    {
        var plan = CreateValidPlan() with
        {
            Agenda = new List<AgendaItem>
            {
                ValidItem(2),   // Invalid: under 3 minutes
                ValidItem(18),
                ValidItem(15)
            }
        };
        plan.IsValid().Should().BeFalse();
    }

    // --- PreworkTasks validation ---

    [Fact]
    public void IsValid_WithLessThan2PreworkTasks_ReturnsFalse()
    {
        var plan = CreateValidPlan() with
        {
            PreworkTasks = new List<string> { "Only one task" }
        };
        plan.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithMoreThan5PreworkTasks_ReturnsFalse()
    {
        var plan = CreateValidPlan() with
        {
            PreworkTasks = new List<string>
            {
                "Task 1", "Task 2", "Task 3", "Task 4", "Task 5", "Task 6"
            }
        };
        plan.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithPreworkTaskExceeding200Chars_ReturnsFalse()
    {
        var plan = CreateValidPlan() with
        {
            PreworkTasks = new List<string>
            {
                new string('X', 201),
                "Valid task"
            }
        };
        plan.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithEmptyPreworkTask_ReturnsFalse()
    {
        var plan = CreateValidPlan() with
        {
            PreworkTasks = new List<string> { "", "Valid task" }
        };
        plan.IsValid().Should().BeFalse();
    }

    // --- FollowUpTasks validation ---

    [Fact]
    public void IsValid_WithLessThan2FollowUpTasks_ReturnsFalse()
    {
        var plan = CreateValidPlan() with
        {
            FollowUpTasks = new List<string> { "Only one" }
        };
        plan.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithMoreThan5FollowUpTasks_ReturnsFalse()
    {
        var plan = CreateValidPlan() with
        {
            FollowUpTasks = new List<string>
            {
                "Task 1", "Task 2", "Task 3", "Task 4", "Task 5", "Task 6"
            }
        };
        plan.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithFollowUpTaskExceeding200Chars_ReturnsFalse()
    {
        var plan = CreateValidPlan() with
        {
            FollowUpTasks = new List<string>
            {
                new string('Y', 201),
                "Valid follow-up"
            }
        };
        plan.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithEmptyFollowUpTask_ReturnsFalse()
    {
        var plan = CreateValidPlan() with
        {
            FollowUpTasks = new List<string> { "   ", "Valid task" }
        };
        plan.IsValid().Should().BeFalse();
    }
}
