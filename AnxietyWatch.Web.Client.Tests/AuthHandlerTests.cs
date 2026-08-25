using System.Net;
using AnxietyWatch.Web.Client.Models.Auth;
using AnxietyWatch.Web.Client.Services;
using Xunit;

namespace AnxietyWatch.Web.Client.Tests;

public sealed class AuthHandlerTests
{
    private static readonly Uri BaseAddress = new("http://127.0.0.1:5222/");

    [Fact]
    public async Task RelativePublicRoute_DoesNotThrowOrAttachToken()
    {
        var terminal = new RecordingHandler();
        var handler = CreateHandler(terminal);
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/plans");

        using var response = await handler.InvokeAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(terminal.Authorization);
    }

    [Fact]
    public async Task RelativeProtectedRoute_AttachesValidToken()
    {
        var terminal = new RecordingHandler();
        var handler = CreateHandler(terminal);
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/dashboard/summary");

        using var response = await handler.InvokeAsync(request);

        Assert.Equal("Bearer test-token", terminal.Authorization);
    }

    [Theory]
    [InlineData("api/billing/simulate-payment")]
    [InlineData("api/billing/summary")]
    [InlineData("api/billing/transactions")]
    public async Task BillingRoutes_AttachValidToken(string route)
    {
        var terminal = new RecordingHandler();
        var handler = CreateHandler(terminal);
        using var request = new HttpRequestMessage(HttpMethod.Get, route);

        using var response = await handler.InvokeAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Bearer test-token", terminal.Authorization);
    }

    [Fact]
    public async Task ExternalAbsoluteRoute_DoesNotLeakToken()
    {
        var terminal = new RecordingHandler();
        var handler = CreateHandler(terminal);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api/dashboard/summary");

        using var response = await handler.InvokeAsync(request);

        Assert.Null(terminal.Authorization);
    }

    [Fact]
    public async Task RelativeProtectedRoute_DoesNotAttachExpiredToken()
    {
        var terminal = new RecordingHandler();
        var handler = new TestableAuthHandler(
            new StubTokenStore("expired-sensitive-token", DateTimeOffset.UtcNow.AddMinutes(-1)),
            BaseAddress)
        {
            InnerHandler = terminal
        };
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/dashboard/summary");

        using var response = await handler.InvokeAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(terminal.Authorization);
        Assert.False(request.Headers.Contains("Authorization"));
    }

    private static TestableAuthHandler CreateHandler(HttpMessageHandler terminal) =>
        new(new StubTokenStore(), BaseAddress) { InnerHandler = terminal };

    private sealed class TestableAuthHandler(ITokenStore tokenStore, Uri baseAddress)
        : AuthHandler(tokenStore, baseAddress)
    {
        public Task<HttpResponseMessage> InvokeAsync(HttpRequestMessage request) =>
            base.SendAsync(request, CancellationToken.None);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class StubTokenStore(
        string token = "test-token",
        DateTimeOffset? expiresAt = null) : ITokenStore
    {
        public Task StoreAsync(AuthResponse session) => Task.CompletedTask;
        public Task RestoreFromStorageAsync() => Task.CompletedTask;
        public Task ClearAsync() => Task.CompletedTask;
        public string? GetAccessToken() => token;
        public UserDto? GetUser() => null;
        public DateTimeOffset? GetExpiresAt() => expiresAt ?? DateTimeOffset.UtcNow.AddMinutes(5);
    }
}
