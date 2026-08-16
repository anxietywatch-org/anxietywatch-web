using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AnxietyWatch.Web.Client.Models.Api;
using AnxietyWatch.Web.Client.Models.Auth;
using AnxietyWatch.Web.Client.Services;
using Moq;
using Xunit;

namespace AnxietyWatch.Web.Client.Tests;

public sealed class ServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task LoginAsync_StoresSuccessfulSession()
    {
        var sessionManager = new Mock<IAuthSessionManager>();
        var service = new AuthService(CreateClient((request, _) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/auth/login", request.RequestUri!.PathAndQuery);
            return Json(HttpStatusCode.OK, SessionJson);
        }), sessionManager.Object, JsonOptions);

        var result = await service.LoginAsync(new LoginRequest { Email = "user@example.com", Password = "password123" });

        Assert.Equal("token-1", result.Token);
        sessionManager.Verify(manager => manager.SetSessionAsync(It.Is<AuthResponse>(session => session.Token == "token-1")), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_InvalidCredentials_Throws401()
    {
        var service = new AuthService(
            CreateClient((_, _) => Json(HttpStatusCode.Unauthorized, ProblemJson(401))),
            Mock.Of<IAuthSessionManager>(),
            JsonOptions);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.LoginAsync(new LoginRequest { Email = "user@example.com", Password = "wrong" }));

        Assert.Equal(401, exception.StatusCode);
    }

    [Fact]
    public async Task GetSessionAsync_ReturnsRestoredSession()
    {
        var expected = CreateSession();
        var sessionManager = new Mock<IAuthSessionManager>();
        sessionManager.Setup(manager => manager.RefreshSessionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var service = new AuthService(CreateClient((_, _) => throw new InvalidOperationException()), sessionManager.Object, JsonOptions);

        var result = await service.GetSessionAsync();

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetSessionAsync_ReturnsNullForExpiredSession()
    {
        var sessionManager = new Mock<IAuthSessionManager>();
        sessionManager.Setup(manager => manager.RefreshSessionAsync(It.IsAny<CancellationToken>())).ReturnsAsync((AuthResponse?)null);
        var service = new AuthService(CreateClient((_, _) => throw new InvalidOperationException()), sessionManager.Object, JsonOptions);

        Assert.Null(await service.GetSessionAsync());
    }

    [Fact]
    public async Task GetEpisodesAsync_ReturnsEpisodes()
    {
        var service = new EpisodeService(CreateClient((request, _) =>
        {
            Assert.Equal("/api/episodes?range=30", request.RequestUri!.PathAndQuery);
            return Json(HttpStatusCode.OK, "[{\"id\":\"episode-1\",\"date\":\"2026-08-12T08:00:00Z\",\"intensity\":42,\"symptoms\":[\"tension\"],\"notes\":null}]");
        }), JsonOptions);

        var episodes = await service.GetEpisodesAsync(30);

        Assert.Single(episodes);
        Assert.Equal(42, episodes[0].Intensity);
    }

    [Fact]
    public async Task CreateEpisodeAsync_ReturnsCreatedEpisode()
    {
        var service = new EpisodeService(CreateClient((request, _) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            return Json(HttpStatusCode.Created, "{\"id\":\"episode-2\",\"date\":\"2026-08-12T08:00:00Z\",\"intensity\":65,\"symptoms\":[],\"notes\":\"test\"}");
        }), JsonOptions);

        var episode = await service.CreateEpisodeAsync(new CreateEpisodeRequest { Intensity = 65, Notes = "test" });

        Assert.Equal("episode-2", episode.Id);
    }

    [Fact]
    public async Task UpdateProfileAsync_ReturnsUpdatedProfile()
    {
        var service = new ProfileService(CreateClient((request, _) =>
        {
            Assert.Equal(HttpMethod.Patch, request.Method);
            Assert.Equal("/api/profile", request.RequestUri!.PathAndQuery);
            return Json(HttpStatusCode.OK, "{\"fullName\":\"Updated User\",\"avatarUrl\":null}");
        }), Mock.Of<IAuthSessionManager>(), JsonOptions);

        var profile = await service.UpdateProfileAsync(new UpdateProfileRequest { FullName = "Updated User" });

        Assert.Equal("Updated User", profile.FullName);
    }

    [Fact]
    public async Task CreateTokenAsync_ReturnsCreatedToken()
    {
        var service = new TokenService(CreateClient((request, _) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            return Json(HttpStatusCode.Created, TokenJson);
        }), JsonOptions);

        var token = await service.CreateTokenAsync(new CreateTokenRequest { Role = "self" });

        Assert.Equal("CODE-1234", token.Code);
    }

    [Fact]
    public async Task DeleteTokenAsync_ReturnsSuccess()
    {
        var id = Guid.NewGuid();
        var service = new TokenService(CreateClient((request, _) =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal($"/api/tokens/{id:D}", request.RequestUri!.PathAndQuery);
            return Json(HttpStatusCode.OK, "{\"success\":true}");
        }), JsonOptions);

        var result = await service.DeleteTokenAsync(id);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ChangePasswordAsync_ClearsSessionBeforeCompleting()
    {
        var clearStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clearFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sessionManager = new Mock<IAuthSessionManager>();
        sessionManager
            .Setup(manager => manager.ClearAsync())
            .Callback(() => clearStarted.SetResult())
            .Returns(clearFinished.Task);
        var service = new ProfileService(CreateClient((request, _) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/auth/change-password", request.RequestUri!.PathAndQuery);
            return Json(HttpStatusCode.OK, "{\"message\":\"Password changed\"}");
        }), sessionManager.Object, JsonOptions);

        var changePasswordTask = service.ChangePasswordAsync(new ChangePasswordRequest
        {
            CurrentPassword = "old-password",
            NewPassword = "new-password"
        });
        await clearStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(changePasswordTask.IsCompleted);
        clearFinished.SetResult();
        await changePasswordTask;
        sessionManager.Verify(manager => manager.ClearAsync(), Times.Once);
    }

    [Theory]
    [InlineData("FREE", "free")]
    [InlineData("free", "FREE")]
    public async Task GetPlansAsync_MatchesCurrentPlanIgnoringCase(string currentPlanId, string apiPlanId)
    {
        var service = new PlanService(
            CreateClient((request, _) =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/api/plans", request.RequestUri!.PathAndQuery);
                return Json(HttpStatusCode.OK, $"[{{\"id\":\"{apiPlanId}\",\"name\":\"Free\"}}]");
            }),
            JsonOptions);

        var plans = await service.GetPlansAsync();

        Assert.True(service.IsCurrentPlan(Assert.Single(plans), currentPlanId));
    }

    [Fact]
    public async Task RetryAfter_WithPositiveDelta_ReturnsSeconds()
    {
        await AssertRetryAfterAsync(
            headers => headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30)),
            seconds => Assert.Equal(30, seconds));
    }

    [Fact]
    public async Task RetryAfter_WithNegativeDelta_IsClampedToZero()
    {
        await AssertRetryAfterAsync(
            headers => headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(-5)),
            seconds => Assert.Equal(0, seconds));
    }

    [Fact]
    public async Task RetryAfter_WithFutureDate_ReturnsRemainingSeconds()
    {
        var retryAt = DateTimeOffset.UtcNow.AddSeconds(30);

        await AssertRetryAfterAsync(
            headers => headers.RetryAfter = new RetryConditionHeaderValue(retryAt),
            seconds =>
            {
                Assert.NotNull(seconds);
                Assert.InRange(seconds.Value, 25, 30);
            });
    }

    [Fact]
    public async Task RetryAfter_WithPastDate_IsClampedToZero()
    {
        await AssertRetryAfterAsync(
            headers => headers.RetryAfter = new RetryConditionHeaderValue(DateTimeOffset.UtcNow.AddMinutes(-1)),
            seconds => Assert.Equal(0, seconds));
    }

    [Fact]
    public async Task RetryAfter_WithoutHeader_ReturnsNull()
    {
        await AssertRetryAfterAsync(_ => { }, seconds => Assert.Null(seconds));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, 400)]
    [InlineData(HttpStatusCode.Unauthorized, 401)]
    [InlineData(HttpStatusCode.Gone, 410)]
    [InlineData(HttpStatusCode.Conflict, 409)]
    [InlineData(HttpStatusCode.TooManyRequests, 429)]
    public async Task ReadApiAsync_ThrowsApiExceptionForKnownErrors(HttpStatusCode statusCode, int expectedStatus)
    {
        using var response = Json(statusCode, ProblemJson(expectedStatus));

        var exception = await Assert.ThrowsAsync<ApiException>(() => response.ReadApiAsync<object>(JsonOptions));

        Assert.Equal(expectedStatus, exception.StatusCode);
    }

    private static async Task AssertRetryAfterAsync(
        Action<HttpResponseHeaders> configureHeaders,
        Action<int?> assertRetryAfter)
    {
        using var resultResponse = ErrorResponse(configureHeaders);
        var result = await resultResponse.ReadApiResultAsync<object>(JsonOptions);
        assertRetryAfter(result.RetryAfterSeconds);

        using var exceptionResponse = ErrorResponse(configureHeaders);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            exceptionResponse.ReadApiAsync<object>(JsonOptions));
        assertRetryAfter(exception.RetryAfterSeconds);
    }

    private static HttpResponseMessage ErrorResponse(Action<HttpResponseHeaders> configureHeaders)
    {
        var response = Json(HttpStatusCode.TooManyRequests, ProblemJson(429));
        configureHeaders(response.Headers);
        return response;
    }

    private static AuthResponse CreateSession() => new()
    {
        Token = "token-1",
        ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        User = new UserDto { Id = "user-1", FullName = "Test User", Email = "user@example.com", PlanId = "free" }
    };

    private static HttpClient CreateClient(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder) =>
        new(new StubHttpMessageHandler(responder)) { BaseAddress = new Uri("https://api.mangoon.xyz/") };

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string content) => new(statusCode)
    {
        Content = new StringContent(content, Encoding.UTF8, "application/json")
    };

    private static string ProblemJson(int status) => $"{{\"title\":\"Request failed\",\"status\":{status}}}";

    private const string SessionJson = "{\"token\":\"token-1\",\"expiresAt\":\"2026-08-13T08:00:00Z\",\"user\":{\"id\":\"user-1\",\"fullName\":\"Test User\",\"email\":\"user@example.com\",\"planId\":\"free\",\"emailVerified\":false,\"avatarUrl\":null}}";
    private const string TokenJson = "{\"id\":\"11111111-1111-1111-1111-111111111111\",\"code\":\"CODE-1234\",\"role\":\"self\",\"expiresAt\":\"2026-08-13T08:00:00Z\",\"status\":\"pending\"}";

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request, cancellationToken));
    }
}
