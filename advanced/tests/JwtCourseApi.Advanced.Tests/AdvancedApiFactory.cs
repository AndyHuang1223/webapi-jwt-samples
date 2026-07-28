using JwtCourseApi.Advanced.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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
        if (disposing)
        {
            // 在 base.Dispose 之前先關閉資料庫連接
            try
            {
                using (var scope = Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
                    dbContext.Database.CloseConnection();
                }
            }
            catch
            {
                // 忽略錯誤，繼續清理
            }

            // 清除 SQLite 連接池，這在 Windows 上很重要
            SqliteConnection.ClearAllPools();
        }

        base.Dispose(disposing);

        if (disposing && File.Exists(_databasePath))
        {
            // 在 Windows 上，檔案可能仍被鎖定，所以重試幾次
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    File.Delete(_databasePath);
                    break;
                }
                catch (IOException) when (i < 2)
                {
                    Thread.Sleep(100);
                }
            }
        }
    }
}
