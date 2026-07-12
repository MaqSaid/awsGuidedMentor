using System.Reflection;
using GuidedMentor.Content.Application;
using GuidedMentor.Content.Domain;
using GuidedMentor.Content.Infrastructure;
using GuidedMentor.Engagement.Application;
using GuidedMentor.Engagement.Domain.Entities;
using GuidedMentor.Engagement.Infrastructure;
using GuidedMentor.Identity.Application;
using GuidedMentor.Identity.Domain.Entities;
using GuidedMentor.Identity.Infrastructure;
using GuidedMentor.Mentoring.Application;
using GuidedMentor.Mentoring.Domain.Entities;
using GuidedMentor.Mentoring.Infrastructure;
using GuidedMentor.Mentoring.Api.Endpoints;
using GuidedMentor.Identity.Api.Endpoints;
using GuidedMentor.SharedKernel;

namespace GuidedMentor.ArchitectureTests;

/// <summary>
/// Architecture tests enforcing Clean Architecture layer dependencies,
/// SOLID principles, naming conventions, and cross-context isolation.
/// </summary>
[Trait("Category", "Architecture")]
public sealed class CleanArchitectureTests
{
    // === Assembly References ===

    // Domain
    private static readonly Assembly IdentityDomainAssembly =
        typeof(User).Assembly;
    private static readonly Assembly MentoringDomainAssembly =
        typeof(MentorEntity).Assembly;
    private static readonly Assembly ContentDomainAssembly =
        typeof(SessionPlan).Assembly;
    private static readonly Assembly EngagementDomainAssembly =
        typeof(Notification).Assembly;

    // Application
    private static readonly Assembly IdentityApplicationAssembly =
        typeof(IdentityApplicationMarker).Assembly;
    private static readonly Assembly MentoringApplicationAssembly =
        typeof(MentoringApplicationMarker).Assembly;
    private static readonly Assembly ContentApplicationAssembly =
        typeof(ContentApplicationMarker).Assembly;
    private static readonly Assembly EngagementApplicationAssembly =
        typeof(EngagementApplicationMarker).Assembly;

    // Infrastructure
    private static readonly Assembly IdentityInfrastructureAssembly =
        typeof(IdentityInfrastructureMarker).Assembly;
    private static readonly Assembly MentoringInfrastructureAssembly =
        typeof(MentoringInfrastructureMarker).Assembly;
    private static readonly Assembly ContentInfrastructureAssembly =
        typeof(ContentInfrastructureMarker).Assembly;
    private static readonly Assembly EngagementInfrastructureAssembly =
        typeof(EngagementInfrastructureMarker).Assembly;

    // Api
    private static readonly Assembly IdentityApiAssembly =
        typeof(AuthEndpoints).Assembly;
    private static readonly Assembly MentoringApiAssembly =
        typeof(BrowseEndpoints).Assembly;
    private static readonly Assembly ContentApiAssembly =
        typeof(ContentApplicationMarker).Assembly;
    private static readonly Assembly EngagementApiAssembly =
        typeof(EngagementApplicationMarker).Assembly;

    // Namespace constants
    private const string IdentityApplicationNamespace = "GuidedMentor.Identity.Application";
    private const string IdentityInfrastructureNamespace = "GuidedMentor.Identity.Infrastructure";
    private const string IdentityApiNamespace = "GuidedMentor.Identity.Api";
    private const string MentoringApplicationNamespace = "GuidedMentor.Mentoring.Application";
    private const string MentoringInfrastructureNamespace = "GuidedMentor.Mentoring.Infrastructure";
    private const string MentoringApiNamespace = "GuidedMentor.Mentoring.Api";
    private const string ContentApplicationNamespace = "GuidedMentor.Content.Application";
    private const string ContentInfrastructureNamespace = "GuidedMentor.Content.Infrastructure";
    private const string ContentApiNamespace = "GuidedMentor.Content.Api";
    private const string EngagementApplicationNamespace = "GuidedMentor.Engagement.Application";
    private const string EngagementInfrastructureNamespace = "GuidedMentor.Engagement.Infrastructure";
    private const string EngagementApiNamespace = "GuidedMentor.Engagement.Api";

    // Forbidden dependency namespaces
    private const string AwsSdkNamespace = "AWSSDK";
    private const string MediatRNamespace = "MediatR";
    private const string FluentValidationNamespace = "FluentValidation";

    // =========================================================================
    // 1. Domain Layer Independence
    // =========================================================================

    [Theory]
    [InlineData("Identity")]
    [InlineData("Mentoring")]
    [InlineData("Content")]
    [InlineData("Engagement")]
    public void Domain_ShouldNotReference_ApplicationLayer(string context)
    {
        var domainAssembly = GetDomainAssembly(context);
        var applicationNamespace = GetApplicationNamespace(context);

        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn(applicationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{context}.Domain should not reference {context}.Application. " +
            $"Violating types: {FormatFailingTypes(result)}");
    }

    [Theory]
    [InlineData("Identity")]
    [InlineData("Mentoring")]
    [InlineData("Content")]
    [InlineData("Engagement")]
    public void Domain_ShouldNotReference_InfrastructureLayer(string context)
    {
        var domainAssembly = GetDomainAssembly(context);
        var infrastructureNamespace = GetInfrastructureNamespace(context);

        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn(infrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{context}.Domain should not reference {context}.Infrastructure. " +
            $"Violating types: {FormatFailingTypes(result)}");
    }

    [Theory]
    [InlineData("Identity")]
    [InlineData("Mentoring")]
    [InlineData("Content")]
    [InlineData("Engagement")]
    public void Domain_ShouldNotReference_ApiLayer(string context)
    {
        var domainAssembly = GetDomainAssembly(context);
        var apiNamespace = GetApiNamespace(context);

        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn(apiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{context}.Domain should not reference {context}.Api. " +
            $"Violating types: {FormatFailingTypes(result)}");
    }

    [Theory]
    [InlineData("Identity")]
    [InlineData("Mentoring")]
    [InlineData("Content")]
    [InlineData("Engagement")]
    public void Domain_ShouldNotReference_AwsSdk(string context)
    {
        var domainAssembly = GetDomainAssembly(context);

        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn(AwsSdkNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{context}.Domain should not reference AWS SDK. " +
            $"Violating types: {FormatFailingTypes(result)}");
    }

    [Theory]
    [InlineData("Identity")]
    [InlineData("Mentoring")]
    [InlineData("Content")]
    [InlineData("Engagement")]
    public void Domain_ShouldNotReference_MediatR(string context)
    {
        var domainAssembly = GetDomainAssembly(context);

        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn(MediatRNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{context}.Domain should not reference MediatR. " +
            $"Violating types: {FormatFailingTypes(result)}");
    }

    [Theory]
    [InlineData("Identity")]
    [InlineData("Mentoring")]
    [InlineData("Content")]
    [InlineData("Engagement")]
    public void Domain_ShouldNotReference_FluentValidation(string context)
    {
        var domainAssembly = GetDomainAssembly(context);

        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn(FluentValidationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{context}.Domain should not reference FluentValidation. " +
            $"Violating types: {FormatFailingTypes(result)}");
    }

    // =========================================================================
    // 2. Application Layer Dependencies
    // =========================================================================

    [Theory]
    [InlineData("Identity")]
    [InlineData("Mentoring")]
    [InlineData("Content")]
    [InlineData("Engagement")]
    public void Application_ShouldNotReference_InfrastructureLayer(string context)
    {
        var applicationAssembly = GetApplicationAssembly(context);
        var infrastructureNamespace = GetInfrastructureNamespace(context);

        var result = Types.InAssembly(applicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(infrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{context}.Application should not reference {context}.Infrastructure. " +
            $"Violating types: {FormatFailingTypes(result)}");
    }

    [Theory]
    [InlineData("Identity")]
    [InlineData("Mentoring")]
    [InlineData("Content")]
    [InlineData("Engagement")]
    public void Application_ShouldNotReference_ApiLayer(string context)
    {
        var applicationAssembly = GetApplicationAssembly(context);
        var apiNamespace = GetApiNamespace(context);

        var result = Types.InAssembly(applicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(apiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{context}.Application should not reference {context}.Api. " +
            $"Violating types: {FormatFailingTypes(result)}");
    }

    [Theory]
    [InlineData("Identity")]
    [InlineData("Mentoring")]
    [InlineData("Content")]
    [InlineData("Engagement")]
    public void Application_ShouldNotReference_AwsSdk(string context)
    {
        var applicationAssembly = GetApplicationAssembly(context);

        var result = Types.InAssembly(applicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(AwsSdkNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{context}.Application should not reference AWS SDK. " +
            $"Violating types: {FormatFailingTypes(result)}");
    }

    // =========================================================================
    // 3. Infrastructure Layer
    // =========================================================================

    [Theory]
    [InlineData("Identity")]
    [InlineData("Mentoring")]
    [InlineData("Content")]
    [InlineData("Engagement")]
    public void Infrastructure_ShouldNotReference_ApiLayer(string context)
    {
        var infrastructureAssembly = GetInfrastructureAssembly(context);
        var apiNamespace = GetApiNamespace(context);

        var result = Types.InAssembly(infrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(apiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{context}.Infrastructure should not reference {context}.Api. " +
            $"Violating types: {FormatFailingTypes(result)}");
    }

    // =========================================================================
    // 4. Api/Presentation Layer
    // =========================================================================

    [Theory]
    [InlineData("Identity")]
    [InlineData("Mentoring")]
    public void Api_ShouldNotReference_InfrastructureDirectly(string context)
    {
        var apiAssembly = GetApiAssembly(context);
        var infrastructureNamespace = GetInfrastructureNamespace(context);

        var result = Types.InAssembly(apiAssembly)
            .ShouldNot()
            .HaveDependencyOn(infrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{context}.Api should not reference {context}.Infrastructure directly. " +
            $"Violating types: {FormatFailingTypes(result)}");
    }

    // =========================================================================
    // 5. SOLID Principles
    // =========================================================================

    [Theory]
    [InlineData("Identity")]
    [InlineData("Mentoring")]
    [InlineData("Content")]
    [InlineData("Engagement")]
    public void Domain_NonAbstractClasses_ShouldBeSealed(string context)
    {
        var domainAssembly = GetDomainAssembly(context);

        var result = Types.InAssembly(domainAssembly)
            .That()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"{context}.Domain non-abstract classes should be sealed. " +
            $"Violating types: {FormatFailingTypes(result)}");
    }

    [Theory]
    [InlineData("Identity")]
    [InlineData("Mentoring")]
    [InlineData("Content")]
    [InlineData("Engagement")]
    public void RepositoryInterfaces_ShouldBeDefined_InApplicationOrDomainLayer(string context)
    {
        var applicationAssembly = GetApplicationAssembly(context);
        var domainAssembly = GetDomainAssembly(context);

        var applicationRepoInterfaces = Types.InAssembly(applicationAssembly)
            .That()
            .AreInterfaces()
            .And()
            .HaveNameEndingWith("Repository")
            .GetTypes();

        var domainRepoInterfaces = Types.InAssembly(domainAssembly)
            .That()
            .AreInterfaces()
            .And()
            .HaveNameEndingWith("Repository")
            .GetTypes();

        var allRepoInterfaces = applicationRepoInterfaces.Concat(domainRepoInterfaces);

        allRepoInterfaces.Should().NotBeEmpty(
            $"{context} should define repository interfaces in Application or Domain layer (Dependency Inversion).");
    }

    [Theory]
    [InlineData("Identity")]
    [InlineData("Mentoring")]
    [InlineData("Content")]
    [InlineData("Engagement")]
    public void RepositoryInterfaces_ShouldNotBeDefined_InInfrastructureLayer(string context)
    {
        var infrastructureAssembly = GetInfrastructureAssembly(context);

        var infraRepoInterfaces = Types.InAssembly(infrastructureAssembly)
            .That()
            .AreInterfaces()
            .And()
            .HaveNameEndingWith("Repository")
            .GetTypes();

        infraRepoInterfaces.Should().BeEmpty(
            $"{context}.Infrastructure should not define repository interfaces. " +
            $"They belong in Application or Domain layer (Dependency Inversion).");
    }

    // =========================================================================
    // 6. Naming Conventions
    // =========================================================================

    [Theory]
    [InlineData("Identity")]
    [InlineData("Mentoring")]
    [InlineData("Content")]
    [InlineData("Engagement")]
    public void CommandClasses_ShouldImplement_IRequest(string context)
    {
        var applicationAssembly = GetApplicationAssembly(context);

        var commandTypes = Types.InAssembly(applicationAssembly)
            .That()
            .HaveNameEndingWith("Command")
            .And()
            .AreClasses()
            .GetTypes()
            // Exclude manually-dispatched commands (not MediatR)
            .Where(t => !t.Namespace!.Contains(".Analytics"));

        foreach (var commandType in commandTypes)
        {
            var implementsIRequest = commandType.GetInterfaces()
                .Any(i => i.Name.StartsWith("IRequest"));

            implementsIRequest.Should().BeTrue(
                $"{commandType.FullName} ends with 'Command' but does not implement IRequest or IRequest<T>.");
        }
    }

    [Theory]
    [InlineData("Identity")]
    [InlineData("Mentoring")]
    [InlineData("Content")]
    [InlineData("Engagement")]
    public void QueryClasses_ShouldImplement_IRequest(string context)
    {
        var applicationAssembly = GetApplicationAssembly(context);

        var queryTypes = Types.InAssembly(applicationAssembly)
            .That()
            .HaveNameEndingWith("Query")
            .And()
            .AreClasses()
            .GetTypes();

        foreach (var queryType in queryTypes)
        {
            var implementsIRequest = queryType.GetInterfaces()
                .Any(i => i.Name.StartsWith("IRequest"));

            implementsIRequest.Should().BeTrue(
                $"{queryType.FullName} ends with 'Query' but does not implement IRequest or IRequest<T>.");
        }
    }

    [Theory]
    [InlineData("Identity")]
    [InlineData("Mentoring")]
    [InlineData("Content")]
    [InlineData("Engagement")]
    public void HandlerClasses_ShouldImplement_IRequestHandler(string context)
    {
        var applicationAssembly = GetApplicationAssembly(context);

        var handlerTypes = Types.InAssembly(applicationAssembly)
            .That()
            .HaveNameEndingWith("Handler")
            .And()
            .AreClasses()
            .GetTypes()
            .Where(t =>
            {
                var ns = t.Namespace ?? string.Empty;
                // Only enforce on MediatR command/query handlers (Commands/ or Queries/ namespaces)
                return ns.Contains(".Commands.") || ns.Contains(".Queries.");
            })
            .Where(t =>
            {
                // Exclude INotificationHandler implementors — they handle domain event notifications
                return !t.GetInterfaces().Any(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition().Name.StartsWith("INotificationHandler"));
            });

        foreach (var handlerType in handlerTypes)
        {
            var implementsHandler = handlerType.GetInterfaces()
                .Any(i => i.Name.StartsWith("IRequestHandler"));

            implementsHandler.Should().BeTrue(
                $"{handlerType.FullName} ends with 'Handler' but does not implement IRequestHandler<,>.");
        }
    }

    [Theory]
    [InlineData("Identity")]
    [InlineData("Mentoring")]
    [InlineData("Content")]
    [InlineData("Engagement")]
    public void ValidatorClasses_ShouldInheritFrom_AbstractValidator(string context)
    {
        var applicationAssembly = GetApplicationAssembly(context);

        var validatorTypes = Types.InAssembly(applicationAssembly)
            .That()
            .HaveNameEndingWith("Validator")
            .And()
            .AreClasses()
            .GetTypes()
            // Exclude non-FluentValidation classes (e.g., OutputValidator, IFileAccessValidator implementations)
            .Where(t => !t.Name.StartsWith("Output") && !t.IsInterface && !t.Namespace!.Contains(".Services"));

        foreach (var validatorType in validatorTypes)
        {
            var inheritsAbstractValidator = InheritsFromGenericType(validatorType, "AbstractValidator");

            inheritsAbstractValidator.Should().BeTrue(
                $"{validatorType.FullName} ends with 'Validator' but does not inherit from AbstractValidator<T>.");
        }
    }

    // =========================================================================
    // 7. Cross-Context Isolation
    // =========================================================================

    [Fact]
    public void IdentityDomain_ShouldNotReference_OtherContextDomains()
    {
        var result = Types.InAssembly(IdentityDomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "GuidedMentor.Mentoring.Domain",
                "GuidedMentor.Content.Domain",
                "GuidedMentor.Engagement.Domain")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Identity.Domain should not reference other bounded contexts. " +
            $"Violating types: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void MentoringDomain_ShouldNotReference_OtherContextDomains()
    {
        var result = Types.InAssembly(MentoringDomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "GuidedMentor.Identity.Domain",
                "GuidedMentor.Content.Domain",
                "GuidedMentor.Engagement.Domain")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Mentoring.Domain should not reference other bounded contexts. " +
            $"Violating types: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void ContentDomain_ShouldNotReference_OtherContextDomains()
    {
        var result = Types.InAssembly(ContentDomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "GuidedMentor.Identity.Domain",
                "GuidedMentor.Mentoring.Domain",
                "GuidedMentor.Engagement.Domain")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Content.Domain should not reference other bounded contexts. " +
            $"Violating types: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void EngagementDomain_ShouldNotReference_OtherContextDomains()
    {
        var result = Types.InAssembly(EngagementDomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "GuidedMentor.Identity.Domain",
                "GuidedMentor.Mentoring.Domain",
                "GuidedMentor.Content.Domain")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Engagement.Domain should not reference other bounded contexts. " +
            $"Violating types: {FormatFailingTypes(result)}");
    }

    // =========================================================================
    // Helper Methods
    // =========================================================================

    private Assembly GetDomainAssembly(string context) => context switch
    {
        "Identity" => IdentityDomainAssembly,
        "Mentoring" => MentoringDomainAssembly,
        "Content" => ContentDomainAssembly,
        "Engagement" => EngagementDomainAssembly,
        _ => throw new ArgumentException($"Unknown context: {context}")
    };

    private Assembly GetApplicationAssembly(string context) => context switch
    {
        "Identity" => IdentityApplicationAssembly,
        "Mentoring" => MentoringApplicationAssembly,
        "Content" => ContentApplicationAssembly,
        "Engagement" => EngagementApplicationAssembly,
        _ => throw new ArgumentException($"Unknown context: {context}")
    };

    private Assembly GetInfrastructureAssembly(string context) => context switch
    {
        "Identity" => IdentityInfrastructureAssembly,
        "Mentoring" => MentoringInfrastructureAssembly,
        "Content" => ContentInfrastructureAssembly,
        "Engagement" => EngagementInfrastructureAssembly,
        _ => throw new ArgumentException($"Unknown context: {context}")
    };

    private Assembly GetApiAssembly(string context) => context switch
    {
        "Identity" => IdentityApiAssembly,
        "Mentoring" => MentoringApiAssembly,
        "Content" => ContentApiAssembly,
        "Engagement" => EngagementApiAssembly,
        _ => throw new ArgumentException($"Unknown context: {context}")
    };

    private static string GetApplicationNamespace(string context) => context switch
    {
        "Identity" => IdentityApplicationNamespace,
        "Mentoring" => MentoringApplicationNamespace,
        "Content" => ContentApplicationNamespace,
        "Engagement" => EngagementApplicationNamespace,
        _ => throw new ArgumentException($"Unknown context: {context}")
    };

    private static string GetInfrastructureNamespace(string context) => context switch
    {
        "Identity" => IdentityInfrastructureNamespace,
        "Mentoring" => MentoringInfrastructureNamespace,
        "Content" => ContentInfrastructureNamespace,
        "Engagement" => EngagementInfrastructureNamespace,
        _ => throw new ArgumentException($"Unknown context: {context}")
    };

    private static string GetApiNamespace(string context) => context switch
    {
        "Identity" => IdentityApiNamespace,
        "Mentoring" => MentoringApiNamespace,
        "Content" => ContentApiNamespace,
        "Engagement" => EngagementApiNamespace,
        _ => throw new ArgumentException($"Unknown context: {context}")
    };

    private static bool InheritsFromGenericType(Type type, string genericTypeName)
    {
        var baseType = type.BaseType;
        while (baseType is not null)
        {
            if (baseType.IsGenericType && baseType.Name.StartsWith(genericTypeName))
                return true;
            baseType = baseType.BaseType;
        }
        return false;
    }

    private static string FormatFailingTypes(TestResult result)
    {
        if (result.IsSuccessful || result.FailingTypes is null)
            return string.Empty;

        return string.Join(", ", result.FailingTypes.Select(t => t.FullName ?? t.Name));
    }
}
