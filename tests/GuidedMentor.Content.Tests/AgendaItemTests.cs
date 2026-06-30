using GuidedMentor.Content.Domain;

namespace GuidedMentor.Content.Tests;

public class AgendaItemTests
{
    [Fact]
    public void IsValid_WithValidItem_ReturnsTrue()
    {
        var item = new AgendaItem("Introduction", 5, "Kick off the session.");
        item.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithEmptyTitle_ReturnsFalse()
    {
        var item = new AgendaItem("", 5, "Description");
        item.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithDurationUnder3_ReturnsFalse()
    {
        var item = new AgendaItem("Title", 2, "Description");
        item.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithDurationExactly3_ReturnsTrue()
    {
        var item = new AgendaItem("Title", 3, "Description");
        item.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithEmptyDescription_ReturnsFalse()
    {
        var item = new AgendaItem("Title", 5, "");
        item.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithDescriptionExceeding500Chars_ReturnsFalse()
    {
        var item = new AgendaItem("Title", 5, new string('D', 501));
        item.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithDescriptionExactly500Chars_ReturnsTrue()
    {
        var item = new AgendaItem("Title", 5, new string('D', 500));
        item.IsValid().Should().BeTrue();
    }
}
