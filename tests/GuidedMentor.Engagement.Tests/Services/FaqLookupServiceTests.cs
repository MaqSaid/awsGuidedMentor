using GuidedMentor.Engagement.Application.Services;

namespace GuidedMentor.Engagement.Tests.Services;

/// <summary>
/// Unit tests for the FaqLookupService covering keyword matching,
/// confidence scoring, threshold filtering, and input normalization.
/// </summary>
public sealed class FaqLookupServiceTests
{
    private readonly FaqLookupService _sut = new();

    [Fact]
    public void FindMatch_ExactKeyword_ReturnsCorrectFaqEntry()
    {
        // Arrange & Act — "reset password" and "forgot password" both appear
        var result = _sut.FindMatch("I forgot password, how do I reset password?");

        // Assert
        result.Should().NotBeNull();
        result!.FaqId.Should().Be("faq-001");
        result.Confidence.Should().BeGreaterThanOrEqualTo(0.5);
    }

    [Fact]
    public void FindMatch_MultipleKeywordsPresent_ReturnsHigherConfidence()
    {
        // Arrange & Act — "forgot password", "reset password", and "change password" are all keywords for faq-001
        var result = _sut.FindMatch("I forgot password and need to reset password or change password");

        // Assert
        result.Should().NotBeNull();
        result!.FaqId.Should().Be("faq-001");
        result.Confidence.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public void FindMatch_NoMatchingKeywords_ReturnsNull()
    {
        // Arrange & Act
        var result = _sut.FindMatch("Tell me about quantum physics");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FindMatch_BelowThreshold_ReturnsNull()
    {
        // Arrange — use a very high threshold that won't be met by a single keyword match
        // faq-009 has 5 keywords; matching 1 gives confidence 0.2
        var result = _sut.FindMatch("notification", threshold: 0.9);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FindMatch_CaseInsensitiveMatching_ReturnsMatch()
    {
        // Arrange & Act — two keywords matched case-insensitively
        var result = _sut.FindMatch("I FORGOT PASSWORD AND NEED A PASSWORD RESET");

        // Assert
        result.Should().NotBeNull();
        result!.FaqId.Should().Be("faq-001");
    }

    [Fact]
    public void FindMatch_PunctuationInMessage_DoesNotBreakMatching()
    {
        // Arrange & Act — punctuation stripped, two keywords should still match
        var result = _sut.FindMatch("I forgot password!!! How do I reset password???");

        // Assert
        result.Should().NotBeNull();
        result!.FaqId.Should().Be("faq-001");
    }

    [Fact]
    public void FindMatch_EmptyMessage_ReturnsNull()
    {
        // Arrange & Act
        var result = _sut.FindMatch("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FindMatch_WhitespaceOnlyMessage_ReturnsNull()
    {
        // Arrange & Act
        var result = _sut.FindMatch("   ");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FindMatch_MentorLockQuestion_ReturnsFaq004()
    {
        // Arrange & Act — "mentor lock", "15 minutes", and "lock timer" are keywords (3/5 = 0.6)
        var result = _sut.FindMatch("What is the mentor lock and the lock timer for 15 minutes?");

        // Assert
        result.Should().NotBeNull();
        result!.FaqId.Should().Be("faq-004");
    }

    [Fact]
    public void FindMatch_MatchingAlgorithmQuestion_ReturnsFaq003()
    {
        // Arrange & Act — "matching algorithm", "match score", "compatibility score" (3/5 = 0.6)
        var result = _sut.FindMatch("How does the matching algorithm calculate match score and compatibility score?");

        // Assert
        result.Should().NotBeNull();
        result!.FaqId.Should().Be("faq-003");
    }

    [Fact]
    public void FindMatch_KeyboardShortcuts_ReturnsFaq016()
    {
        // Arrange & Act — "keyboard shortcut" and "shortcuts" are keywords
        var result = _sut.FindMatch("What keyboard shortcut and shortcuts are available?");

        // Assert
        result.Should().NotBeNull();
        result!.FaqId.Should().Be("faq-016");
    }

    [Fact]
    public void FindMatch_ConfidenceIsWithinValidRange()
    {
        // Arrange & Act — use a message that matches multiple keywords
        var result = _sut.FindMatch("I forgot password and need to reset password");

        // Assert
        result.Should().NotBeNull();
        result!.Confidence.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public void FindMatch_CustomThreshold_FiltersCorrectly()
    {
        // Arrange — threshold 0.0 should always match if any keyword hits
        var result = _sut.FindMatch("notification", threshold: 0.0);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void FindMatch_PartialKeywordMatch_AboveThreshold_ReturnsEntry()
    {
        // "forgot password" + "change password" are keywords for faq-001 (4 total keywords)
        // matching 2/4 = 0.5 confidence which equals default threshold
        var result = _sut.FindMatch("I forgot password and want to change password");

        // Assert
        result.Should().NotBeNull();
        result!.FaqId.Should().Be("faq-001");
        result.Confidence.Should().BeGreaterThanOrEqualTo(0.5);
    }

    [Theory]
    [InlineData("I forgot password and need to reset password", "faq-001")]
    [InlineData("matching algorithm and match score and compatibility score", "faq-003")]
    [InlineData("switch role become mentor become mentee", "faq-005")]
    [InlineData("session plan and session agenda and generate plan", "faq-006")]
    [InlineData("data privacy and analytics consent and opt out", "faq-015")]
    public void FindMatch_VariousQuestions_ReturnsExpectedFaqId(string message, string expectedFaqId)
    {
        // Arrange & Act
        var result = _sut.FindMatch(message);

        // Assert
        result.Should().NotBeNull();
        result!.FaqId.Should().Be(expectedFaqId);
    }

    [Fact]
    public void NormalizeText_RemovesPunctuationAndLowercases()
    {
        // Arrange & Act
        var normalized = FaqLookupService.NormalizeText("Hello, World! How Are You?");

        // Assert
        normalized.Should().Be("hello world how are you");
    }

    [Fact]
    public void NormalizeText_TrimsWhitespace()
    {
        // Arrange & Act
        var normalized = FaqLookupService.NormalizeText("  some text  ");

        // Assert
        normalized.Should().Be("some text");
    }
}
