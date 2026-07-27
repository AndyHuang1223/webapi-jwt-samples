using JwtCourseApi.Advanced.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JwtCourseApi.Advanced.Tests;

public sealed class AdvancedApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"jwt-course-tests-{Guid.NewGuid():N}.db");

    public ManualTimeProvider Clock { get; } =
        new(DateTimeOffset.UtcNow);

    public IServiceScope CreateScope()
    {
        return Services.CreateScope();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AuthDatabase"] =
                    $"Data Source={_databasePath};Default Timeout=5",
                ["Jwt:SigningKey"] =
                    "integration-test-signing-key-at-least-32-characters",
                ["Jwt:Issuer"] = "JwtCourseAdvancedApi",
                ["Jwt:Audience"] = "JwtCourseClient",
                ["Jwt:ExpirationMinutes"] = "15",
                ["RefreshToken:ExpirationDays"] = "30",
                ["RefreshToken:CookieName"] = "__Secure-jwt-refresh"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}
