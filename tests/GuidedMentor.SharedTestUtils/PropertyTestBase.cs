namespace GuidedMentor.SharedTestUtils;

/// <summary>
/// Base class for property-based tests providing shared configuration constants.
/// All property tests should inherit from this class to ensure consistent test parameters.
/// 
/// Usage with FsCheck.Xunit 3.x:
/// <code>
/// [Property(MaxTest = PropertyTestBase.MaxTests, StartSize = PropertyTestBase.StartSize, EndSize = PropertyTestBase.EndSize)]
/// public void MyProperty(int input) { ... }
/// </code>
/// </summary>
public abstract class PropertyTestBase
{
    /// <summary>Minimum number of test iterations for property tests.</summary>
    public const int MaxTests = 100;

    /// <summary>Starting size for generated values.</summary>
    public const int StartSize = 1;

    /// <summary>Ending size for generated values.</summary>
    public const int EndSize = 100;
}
