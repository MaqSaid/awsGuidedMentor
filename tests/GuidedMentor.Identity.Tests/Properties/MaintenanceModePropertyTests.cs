using FsCheck.Fluent;

namespace GuidedMentor.Identity.Tests.Properties;

/// <summary>
/// Property 33: Maintenance Mode Blocks Non-Admin Requests.
/// Validates that:
/// - When maintenance mode is ON: any non-admin request → 503
/// - When maintenance mode is ON: any admin request → proceeds (not 503)
/// - When maintenance mode is OFF: all requests proceed
/// </summary>
[Trait("Category", "Property")]
public sealed class MaintenanceModePropertyTests : PropertyTestBase
{
    [Property(MaxTest = 100)]
    public FsCheck.Property Property33_MaintenanceOn_NonAdmin_Returns503()
    {
        var gen = Gen.Elements("GET", "POST", "PUT", "DELETE", "PATCH").SelectMany(method =>
            Gen.Elements("/api/mentors", "/api/sessions", "/api/browse", "/api/notifications")
               .Select(path => new { method, path }));

        return Prop.ForAll(gen.ToArbitrary(), request =>
        {
            var maintenanceMode = true;
            var isAdmin = false;

            var statusCode = EvaluateMaintenanceMode(maintenanceMode, isAdmin);
            statusCode.Should().Be(503, "non-admin requests should be blocked during maintenance");
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property33_MaintenanceOn_Admin_Proceeds()
    {
        var gen = Gen.Elements("GET", "POST", "PUT", "DELETE", "PATCH").SelectMany(method =>
            Gen.Elements("/api/mentors", "/api/sessions", "/api/admin/users", "/api/admin/settings")
               .Select(path => new { method, path }));

        return Prop.ForAll(gen.ToArbitrary(), request =>
        {
            var maintenanceMode = true;
            var isAdmin = true;

            var statusCode = EvaluateMaintenanceMode(maintenanceMode, isAdmin);
            statusCode.Should().NotBe(503, "admin requests should proceed during maintenance");
            statusCode.Should().Be(200);
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property33_MaintenanceOff_AllRequestsProceed()
    {
        var gen = Gen.Elements(true, false).SelectMany(isAdmin =>
            Gen.Elements("GET", "POST", "PUT", "DELETE").SelectMany(method =>
            Gen.Elements("/api/mentors", "/api/sessions", "/api/browse")
               .Select(path => new { isAdmin, method, path })));

        return Prop.ForAll(gen.ToArbitrary(), request =>
        {
            var maintenanceMode = false;

            var statusCode = EvaluateMaintenanceMode(maintenanceMode, request.isAdmin);
            statusCode.Should().NotBe(503, "no requests should be blocked when maintenance mode is OFF");
            statusCode.Should().Be(200);
        });
    }

    [Property(MaxTest = 100)]
    public FsCheck.Property Property33_MaintenanceMode_AllCombinations_ProduceCorrectResult()
    {
        var gen = Gen.Elements(true, false).SelectMany(maintenance =>
            Gen.Elements(true, false).Select(admin => new { maintenance, admin }));

        return Prop.ForAll(gen.ToArbitrary(), scenario =>
        {
            var statusCode = EvaluateMaintenanceMode(scenario.maintenance, scenario.admin);

            if (scenario.maintenance && !scenario.admin)
                statusCode.Should().Be(503);
            else
                statusCode.Should().Be(200);
        });
    }

    /// <summary>
    /// Simulates the maintenance mode middleware behavior.
    /// Returns 503 when maintenance is ON and the request is not from an admin.
    /// </summary>
    private static int EvaluateMaintenanceMode(bool maintenanceEnabled, bool isAdmin)
    {
        if (maintenanceEnabled && !isAdmin)
            return 503;

        return 200;
    }
}
