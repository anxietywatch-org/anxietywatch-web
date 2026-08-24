using System.Net;
using System.Text;
using System.Text.Json;
using AnxietyWatch.Web.Client.Models.Api;
using AnxietyWatch.Web.Client.Models.Auth;
using AnxietyWatch.Web.Client.Services;
using Xunit;

namespace AnxietyWatch.Web.Client.Tests;

public sealed class ServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CheckEmailAvailabilityAsync_ReturnsAvailability(bool available)
    {
        const string email = "user@example.test";
        var service = new AuthService(
            new HttpClient(new StubHttpMessageHandler(async (request, cancellationToken) =>
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("/api/auth/email-availability", request.RequestUri!.PathAndQuery);

                await using var content = await request.Content!.ReadAsStreamAsync(cancellationToken);
                using var body = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
                Assert.Equal(email, body.RootElement.GetProperty("email").GetString());

                return Json(HttpStatusCode.OK, $"{{\"available\":{available.ToString().ToLowerInvariant()}}}");
            }))
            {
                BaseAddress = new Uri("https://api.mangoon.xyz/")
            },
            new StubSessionManager(),
            JsonOptions);

        var result = await service.CheckEmailAvailabilityAsync(email);

        Assert.Equal(available, result.Available);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DowngradeToFreeAsync_ReturnsChangedState(bool changed)
    {
        var service = new BillingService(
            new HttpClient(new StubHttpMessageHandler((request, _) =>
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("/api/billing/downgrade-to-free", request.RequestUri!.PathAndQuery);
                Assert.Null(request.Content);

                return Task.FromResult(Json(
                    HttpStatusCode.OK,
                    $"{{\"planId\":\"free\",\"previousPlanId\":\"professional\",\"changed\":{changed.ToString().ToLowerInvariant()},\"downgradedAt\":\"2026-08-23T12:00:00Z\"}}"));
            }))
            {
                BaseAddress = new Uri("https://api.mangoon.xyz/")
            },
            JsonOptions);

        var result = await service.DowngradeToFreeAsync();

        Assert.Equal("free", result.PlanId);
        Assert.Equal("professional", result.PreviousPlanId);
        Assert.Equal(changed, result.Changed);
        Assert.Equal(DateTimeOffset.Parse("2026-08-23T12:00:00Z"), result.DowngradedAt);
    }

    [Fact]
    public async Task RotateTokenAsync_ReturnsRotatedToken()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var service = new TokenService(
            new HttpClient(new StubHttpMessageHandler((request, _) =>
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal($"/api/tokens/{id:D}/rotate", request.RequestUri!.PathAndQuery);
                Assert.Null(request.Content);
                return Task.FromResult(Json(
                    HttpStatusCode.OK,
                    $"{{\"id\":\"{id:D}\",\"code\":\"NEW-CODE\",\"role\":\"self\",\"expiresAt\":\"2026-08-24T12:00:00Z\",\"status\":\"pending\"}}"));
            }))
            {
                BaseAddress = new Uri("https://api.mangoon.xyz/")
            },
            JsonOptions);

        var result = await service.RotateTokenAsync(id);

        Assert.Equal(id.ToString(), result.Id);
        Assert.Equal("NEW-CODE", result.Code);
        Assert.Equal("self", result.Role);
        Assert.Equal("pending", result.Status);
        Assert.Equal(DateTimeOffset.Parse("2026-08-24T12:00:00Z"), result.ExpiresAt);
    }

    [Fact]
    public async Task RotateTokenAsync_ConflictThrows409()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var service = new TokenService(
            new HttpClient(new StubHttpMessageHandler((request, _) =>
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal($"/api/tokens/{id:D}/rotate", request.RequestUri!.PathAndQuery);
                return Task.FromResult(Json(
                    HttpStatusCode.Conflict,
                    "{\"title\":\"Token state changed\",\"status\":409}"));
            }))
            {
                BaseAddress = new Uri("https://api.mangoon.xyz/")
            },
            JsonOptions);

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.RotateTokenAsync(id));

        Assert.Equal(409, exception.StatusCode);
    }

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string content) => new(statusCode)
    {
        Content = new StringContent(content, Encoding.UTF8, "application/json")
    };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => responder(request, cancellationToken);
    }

    private sealed class StubSessionManager : IAuthSessionManager
    {
        public UserDto? CurrentUser => null;
        public bool IsAuthenticated => false;
        public string? InitializationError => null;
        public event Action? SessionChanged { add { } remove { } }
        public Task<bool> InitializeAsync() => Task.FromResult(false);
        public Task<bool> RetryInitializationAsync() => Task.FromResult(false);
        public Task<AuthResponse?> RefreshSessionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<AuthResponse?>(null);
        public Task SetSessionAsync(AuthResponse session) => Task.CompletedTask;
        public Task UpdateUserAsync(UserDto user) => Task.CompletedTask;
        public Task ClearAsync() => Task.CompletedTask;
    }
}
