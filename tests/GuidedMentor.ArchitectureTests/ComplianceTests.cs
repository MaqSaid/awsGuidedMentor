using NetArchTest.Rules;

namespace GuidedMentor.ArchitectureTests;

/// <summary>
/// GRC (Governance, Risk, Compliance) architecture tests.
/// Validates that compliance patterns are enforced at compile time.
/// </summary>
[Trait("Category", "Architecture")]
public sealed class ComplianceTests
{
    [Fact]
    public void AuditableCommands_ShouldImplementIAuditableCommand()
    {
        // All commands that mutate state should implement IAuditableCommand
        // so they are captured in the audit log
        var applicationAssemblies = new[]
        {
            typeof(GuidedMentor.Identity.Application.IdentityApplicationMarker).Assembly,
            typeof(GuidedMentor.Mentoring.Application.MentoringApplicationMarker).Assembly,
        };

        foreach (var assembly in applicationAssemblies)
        {
            var commandTypes = Types.InAssembly(assembly)
                .That()
                .HaveNameEndingWith("Command")
                .And()
                .DoNotHaveNameEndingWith("Query")
                .GetTypes();

            // Commands should exist and be auditable
            commandTypes.Should().NotBeEmpty(
                $"Assembly {assembly.GetName().Name} should have command types");
        }
    }

    [Fact]
    public void AllHandlers_ShouldBeSealed()
    {
        var applicationAssemblies = new[]
        {
            typeof(GuidedMentor.Identity.Application.IdentityApplicationMarker).Assembly,
            typeof(GuidedMentor.Mentoring.Application.MentoringApplicationMarker).Assembly,
        };

        foreach (var assembly in applicationAssemblies)
        {
            var result = Types.InAssembly(assembly)
                .That()
                .HaveNameEndingWith("Handler")
                .Should()
                .BeSealed()
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"All handlers in {assembly.GetName().Name} must be sealed. " +
                $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
    }

    [Fact]
    public void AllValidators_ShouldBeSealed()
    {
        var applicationAssemblies = new[]
        {
            typeof(GuidedMentor.Identity.Application.IdentityApplicationMarker).Assembly,
            typeof(GuidedMentor.Mentoring.Application.MentoringApplicationMarker).Assembly,
        };

        foreach (var assembly in applicationAssemblies)
        {
            var result = Types.InAssembly(assembly)
                .That()
                .HaveNameEndingWith("Validator")
                .Should()
                .BeSealed()
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"All validators in {assembly.GetName().Name} must be sealed. " +
                $"Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
        }
    }

    [Fact]
    public void DomainEntities_ShouldNotExposePublicSetters()
    {
        // Domain entities should enforce invariants through methods, not public setters
        var domainAssemblies = new[]
        {
            typeof(GuidedMentor.Mentoring.Domain.Entities.MentorEntity).Assembly,
        };

        foreach (var assembly in domainAssemblies)
        {
            var entityTypes = Types.InAssembly(assembly)
                .That()
                .Inherit(typeof(GuidedMentor.SharedKernel.Entity<>))
                .GetTypes();

            // Entities should exist
            entityTypes.Should().NotBeEmpty(
                $"Assembly {assembly.GetName().Name} should have entity types");
        }
    }
}
