using System.Reflection;

namespace GuidedMentor.Identity.Application;

/// <summary>
/// Assembly marker for MediatR handler discovery and FluentValidation registration.
/// </summary>
public static class IdentityApplicationMarker
{
    public const string Version = "0.1.0";

    /// <summary>
    /// Returns the assembly for handler/validator auto-registration.
    /// </summary>
    public static Assembly Assembly => typeof(IdentityApplicationMarker).Assembly;
}
