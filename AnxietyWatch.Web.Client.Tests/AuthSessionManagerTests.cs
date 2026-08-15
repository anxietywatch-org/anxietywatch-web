using System.Net;
using System.Text;
using System.Text.Json;
using AnxietyWatch.Web.Client.Models.Auth;
using AnxietyWatch.Web.Client.Services;
using Xunit;

namespace AnxietyWatch.Web.Client.Tests;

public sealed class AuthSessionManagerTests
{
    [Fact]
    public async Task ConcurrentInitialization_SharesOneValidationRequest()
    {
        var handler = new SessionHandler(CreateSessionJson());
        var manager = CreateManager(handler, new StoredTokenStore());

        var results = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => manager.InitializeAsync()));

        Assert.All(results, Assert.True);
        Assert.Equal(1, handler.RequestCount);
        Assert.True(manager.IsAuthenticated);
    }

    [Fact]
    public async Task InvalidSessionResponse_FailsClosedWithVisibleError()
    {
        var handler = new SessionHandler("{\"token\":\"\",\"expiresAt\":\"0001-01-01T00:00:00Z\",\"user\":null}");
        var manager = CreateManager(handler, new StoredTokenStore());

        var result = await manager.InitializeAsync();

        Assert.False(result);
        Assert.False(manager.IsAuthenticated);
        Assert.Contains("sesión inválida", manager.InitializationError, StringComparison.OrdinalIgnoreCase);
    }

    private static AuthSessionManager CreateManager(HttpMessageHandler handler, ITokenStore store) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") },
            store,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private static string CreateSessionJson() => JsonSerializer.Serialize(new AuthResponse
    {
        Token = "validated-token",
        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
        User = new UserDto
        {
            Id = "user-1",
            FullName = "Test User",
            Email = "test@example.test",
            PlanId = "free"
        }
    });

    private sealed class SessionHandler(string responseJson) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            await Task.Delay(10, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class StoredTokenStore : ITokenStore
    {
        private readonly AuthResponse stored = new()
        {
            Token = "stored-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
            User = new UserDto
            {
                Id = "user-1",
                FullName = "Test User",
                Email = "test@example.test",
                PlanId = "free"
            }
        };

        public Task StoreAsync(AuthResponse session)
        {
            stored.Token = session.Token;
            stored.ExpiresAt = session.ExpiresAt;
            stored.User = session.User;
            return Task.CompletedTask;
        }

        public Task RestoreFromStorageAsync() => Task.CompletedTask;
        public Task ClearAsync() => Task.CompletedTask;
        public string? GetAccessToken() => stored.Token;
        public UserDto? GetUser() => stored.User;
        public DateTimeOffset? GetExpiresAt() => stored.ExpiresAt;
    }
}
