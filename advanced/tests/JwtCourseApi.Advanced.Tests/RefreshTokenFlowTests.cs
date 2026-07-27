using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JwtCourseApi.Advanced.Data;
using JwtCourseApi.Advanced.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JwtCourseApi.Advanced.Tests;

public sealed class RefreshTokenFlowTests
{
    [Fact]
    public async Task BodyLogin_ProvidesTokenPair_AndAccessTokenAuthorizesEndpoints()
    {
        using var factory = new AdvancedApiFactory();
        using var client = CreateClient(factory);

        var loginResponse = await LoginBodyAsync(client, "student", "Student123!");
        var tokens = await ReadTokenPairAsync(loginResponse);

        Assert.NotEmpty(tokens.AccessToken);
        Assert.NotEmpty(tokens.RefreshToken);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        Assert.Equal(
            HttpStatusCode.OK,
            (await client.GetAsync("/api/demo/profile")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await client.GetAsync("/api/demo/admin")).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await client.GetAsync("/api/demo/it-department")).StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/demo/profile")).StatusCode);
    }

    [Fact]
    public async Task InvalidLogin_ReturnsGenericUnauthorizedProblemDetails()
    {
        using var factory = new AdvancedApiFactory();
        using var client = CreateClient(factory);

        var response = await LoginBodyAsync(client, "student", "wrong-password");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("驗證失敗", json);
        Assert.DoesNotContain("wrong-password", json);
    }

    [Fact]
    public async Task Rotation_ReplayRevokesOnlyTheReplayedFamily()
    {
        using var factory = new AdvancedApiFactory();
        using var client = CreateClient(factory);

        var firstFamily = await ReadTokenPairAsync(
            await LoginBodyAsync(client, "student", "Student123!"));
        var otherFamily = await ReadTokenPairAsync(
            await LoginBodyAsync(client, "student", "Student123!"));

        var rotation = await RefreshBodyAsync(client, firstFamily.RefreshToken);
        var rotatedTokens = await ReadTokenPairAsync(rotation);

        Assert.NotEqual(firstFamily.RefreshToken, rotatedTokens.RefreshToken);

        var replay = await RefreshBodyAsync(client, firstFamily.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);

        var revokedWinner = await RefreshBodyAsync(client, rotatedTokens.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, revokedWinner.StatusCode);

        var unaffectedFamily = await RefreshBodyAsync(client, otherFamily.RefreshToken);
        Assert.Equal(HttpStatusCode.OK, unaffectedFamily.StatusCode);
    }

    [Fact]
    public async Task LogoutAndExpiration_PreventFurtherRefresh()
    {
        using var factory = new AdvancedApiFactory();
        using var client = CreateClient(factory);

        var logoutTokens = await ReadTokenPairAsync(
            await LoginBodyAsync(client, "admin", "Admin123!"));

        var logout = await client.PostAsJsonAsync(
            "/api/auth/body/logout",
            new RefreshTokenRequest { RefreshToken = logoutTokens.RefreshToken });

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await RefreshBodyAsync(client, logoutTokens.RefreshToken)).StatusCode);

        var expiringTokens = await ReadTokenPairAsync(
            await LoginBodyAsync(client, "student", "Student123!"));
        factory.Clock.Advance(TimeSpan.FromDays(31));

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await RefreshBodyAsync(client, expiringTokens.RefreshToken)).StatusCode);
    }

    [Fact]
    public async Task DatabaseStoresOnlySha256HashOfRefreshToken()
    {
        using var factory = new AdvancedApiFactory();
        using var client = CreateClient(factory);

        var tokens = await ReadTokenPairAsync(
            await LoginBodyAsync(client, "student", "Student123!"));

        using var scope = factory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var storedToken = await dbContext.RefreshTokens.SingleAsync();
        var expectedHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(tokens.RefreshToken)));

        Assert.Equal(expectedHash, storedToken.TokenHash);
        Assert.NotEqual(tokens.RefreshToken, storedToken.TokenHash);
        Assert.Equal(64, storedToken.TokenHash.Length);
    }

    [Fact]
    public async Task CookieMode_UsesSecureCookieAndRequiresAntiforgeryHeader()
    {
        using var factory = new AdvancedApiFactory();
        using var client = CreateClient(factory);

        var login = await client.PostAsJsonAsync(
            "/api/auth/cookie/login",
            new LoginRequest
            {
                Username = "student",
                Password = "Student123!"
            });
        var responseJson = await login.Content.ReadAsStringAsync();
        var cookieTokens = JsonSerializer.Deserialize<CookieTokenResponse>(
            responseJson,
            JsonOptions)!;
        var jsonDocument = JsonDocument.Parse(responseJson);
        var setCookies = login.Headers.GetValues("Set-Cookie").ToArray();
        var refreshCookie = Assert.Single(
            setCookies,
            value => value.StartsWith(
                "__Secure-jwt-refresh=",
                StringComparison.Ordinal));

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.False(jsonDocument.RootElement.TryGetProperty("refreshToken", out _));
        Assert.Contains("httponly", refreshCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", refreshCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", refreshCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth/cookie", refreshCookie, StringComparison.OrdinalIgnoreCase);

        var missingCsrf = await client.PostAsync("/api/auth/cookie/refresh", null);
        await AssertStatusAsync(missingCsrf, HttpStatusCode.BadRequest);

        using var refreshRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/auth/cookie/refresh");
        refreshRequest.Headers.Add("X-CSRF-TOKEN", cookieTokens.CsrfToken);
        var refresh = await client.SendAsync(refreshRequest);

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var rotated = await refresh.Content.ReadFromJsonAsync<CookieTokenResponse>();
        Assert.NotNull(rotated);

        using var logoutRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/auth/cookie/logout");
        logoutRequest.Headers.Add("X-CSRF-TOKEN", rotated.CsrfToken);
        var logout = await client.SendAsync(logoutRequest);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Contains(
            logout.Headers.GetValues("Set-Cookie"),
            value =>
                value.StartsWith("__Secure-jwt-refresh=", StringComparison.Ordinal) &&
                value.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task BodyTokenCannotBeUsedThroughCookieEndpoint()
    {
        using var factory = new AdvancedApiFactory();
        using var bodyClient = CreateClient(factory);
        using var manualCookieClient = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false,
                HandleCookies = false
            });

        var bodyTokens = await ReadTokenPairAsync(
            await LoginBodyAsync(bodyClient, "student", "Student123!"));

        var csrfResponse = await manualCookieClient.GetAsync("/api/auth/cookie/csrf");
        var csrf = await csrfResponse.Content.ReadFromJsonAsync<CsrfTokenResponse>();
        var antiforgeryCookie = csrfResponse.Headers
            .GetValues("Set-Cookie")
            .Single(value =>
                value.StartsWith("__Host-jwt-antiforgery=", StringComparison.Ordinal))
            .Split(';', 2)[0];

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/auth/cookie/refresh");
        request.Headers.Add("X-CSRF-TOKEN", csrf!.CsrfToken);
        request.Headers.Add(
            "Cookie",
            $"{antiforgeryCookie}; __Secure-jwt-refresh={bodyTokens.RefreshToken}");

        var response = await manualCookieClient.SendAsync(request);
        await AssertStatusAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ConcurrentRefresh_AllowsOneRotationThenRevokesTheWinnerFamily()
    {
        using var factory = new AdvancedApiFactory();
        using var client = CreateClient(factory);

        var tokens = await ReadTokenPairAsync(
            await LoginBodyAsync(client, "student", "Student123!"));

        var refreshes = await Task.WhenAll(
            RefreshBodyAsync(client, tokens.RefreshToken),
            RefreshBodyAsync(client, tokens.RefreshToken));

        var success = Assert.Single(
            refreshes,
            response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(
            refreshes,
            response => response.StatusCode == HttpStatusCode.Unauthorized);

        var winnerTokens = await ReadTokenPairAsync(success);
        var winnerRefresh = await RefreshBodyAsync(
            client,
            winnerTokens.RefreshToken);

        Assert.Equal(HttpStatusCode.Unauthorized, winnerRefresh.StatusCode);
    }

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private static HttpClient CreateClient(AdvancedApiFactory factory)
    {
        return factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false,
                HandleCookies = true
            });
    }

    private static Task<HttpResponseMessage> LoginBodyAsync(
        HttpClient client,
        string username,
        string password)
    {
        return client.PostAsJsonAsync(
            "/api/auth/body/login",
            new LoginRequest
            {
                Username = username,
                Password = password
            });
    }

    private static Task<HttpResponseMessage> RefreshBodyAsync(
        HttpClient client,
        string refreshToken)
    {
        return client.PostAsJsonAsync(
            "/api/auth/body/refresh",
            new RefreshTokenRequest { RefreshToken = refreshToken });
    }

    private static async Task<TokenPairResponse> ReadTokenPairAsync(
        HttpResponseMessage response)
    {
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Expected 200 OK but received {(int)response.StatusCode} {response.StatusCode}.{Environment.NewLine}{responseBody}");
        var tokens = await response.Content.ReadFromJsonAsync<TokenPairResponse>();
        return Assert.IsType<TokenPairResponse>(tokens);
    }

    private static async Task AssertStatusAsync(
        HttpResponseMessage response,
        HttpStatusCode expected)
    {
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode == expected,
            $"Expected {(int)expected} {expected} but received {(int)response.StatusCode} {response.StatusCode}.{Environment.NewLine}{responseBody}");
    }
}
