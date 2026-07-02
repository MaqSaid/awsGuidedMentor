using Microsoft.AspNetCore.Mvc.Testing;

namespace GuidedMentor.Integration.Tests;

/// <summary>
/// Base class for API integration tests.
/// Provides a WebApplicationFactory for in-memory API testing.
/// </summary>
public abstract class ApiTestBase<TEntryPoint> : IClassFixture<WebApplicationFactory<TEntryPoint>>
    where TEntryPoint : class
{
    protected readonly HttpClient Client;
    protected readonly WebApplicationFactory<TEntryPoint> Factory;

    protected ApiTestBase(WebApplicationFactory<TEntryPoint> factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }
}
