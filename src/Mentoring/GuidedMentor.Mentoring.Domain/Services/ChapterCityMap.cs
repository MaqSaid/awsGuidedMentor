using GuidedMentor.SharedKernel;

namespace GuidedMentor.Mentoring.Domain.Services;

/// <summary>
/// Maps each Australian chapter to its primary city for same-city detection.
/// Used by the matching algorithm to award +15 bonus when mentee and mentor
/// are in the same city but different chapters.
/// </summary>
public static class ChapterCityMap
{
    private static readonly Dictionary<AustralianChapter, string> ChapterToCity = new()
    {
        [AustralianChapter.Sydney] = "Sydney",
        [AustralianChapter.Melbourne] = "Melbourne",
        [AustralianChapter.Brisbane] = "Brisbane",
        [AustralianChapter.Perth] = "Perth",
        [AustralianChapter.Adelaide] = "Adelaide",
        [AustralianChapter.Canberra] = "Canberra",
        [AustralianChapter.Hobart] = "Hobart",
        [AustralianChapter.Darwin] = "Darwin",
        [AustralianChapter.GoldCoast] = "Gold Coast",
        [AustralianChapter.Newcastle] = "Newcastle",
        [AustralianChapter.Wollongong] = "Wollongong",
        [AustralianChapter.Geelong] = "Geelong",
        [AustralianChapter.Townsville] = "Townsville"
    };

    /// <summary>
    /// Gets the primary city name for a given Australian chapter.
    /// </summary>
    public static string GetCity(AustralianChapter chapter) => ChapterToCity[chapter];

    /// <summary>
    /// Determines whether two users are in the same city based on their chapter city mappings
    /// or explicit city fields (case-insensitive comparison).
    /// </summary>
    public static bool AreSameCity(AustralianChapter chapter1, string city1, AustralianChapter chapter2, string city2)
    {
        // Same chapter implies same city
        if (chapter1 == chapter2) return true;

        var chapterCity1 = ChapterToCity[chapter1];
        var chapterCity2 = ChapterToCity[chapter2];

        // Check if the chapter cities match
        if (string.Equals(chapterCity1, chapterCity2, StringComparison.OrdinalIgnoreCase))
            return true;

        // Check if the explicit city fields match
        if (!string.IsNullOrWhiteSpace(city1) && !string.IsNullOrWhiteSpace(city2) &&
            string.Equals(city1.Trim(), city2.Trim(), StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
}
