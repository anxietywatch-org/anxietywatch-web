using System.Net;
using System.Net.Http.Headers;
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

    [Fact]
    public async Task ForgotPasswordAsync_ReturnsMessage()
    {
        var service = CreateAuthService((request, _) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/auth/password/forgot", request.RequestUri!.PathAndQuery);
            return Json(HttpStatusCode.OK, "{\"message\":\"Recovery email sent\"}");
        });

        var result = await service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "user@example.test" });

        Assert.Equal("Recovery email sent", result.Message);
    }

    [Fact]
    public async Task ForgotPasswordAsync_RateLimitedThrows429()
    {
        var service = CreateAuthService((_, _) => Json(HttpStatusCode.TooManyRequests, ProblemJson(429)));

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "user@example.test" }));

        Assert.Equal(429, exception.StatusCode);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsMessage()
    {
        var service = CreateAuthService((request, _) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/auth/password/reset", request.RequestUri!.PathAndQuery);
            return Json(HttpStatusCode.OK, "{\"message\":\"Password reset\"}");
        });

        var result = await service.ResetPasswordAsync(new ResetPasswordRequest
        {
            Token = "reset-token",
            NewPassword = "NewPassword123!"
        });

        Assert.Equal("Password reset", result.Message);
    }

    [Fact]
    public async Task ResetPasswordAsync_ExpiredTokenThrows410()
    {
        var service = CreateAuthService((_, _) => Json(HttpStatusCode.Gone, ProblemJson(410)));

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.ResetPasswordAsync(new ResetPasswordRequest { Token = "expired", NewPassword = "NewPassword123!" }));

        Assert.Equal(410, exception.StatusCode);
    }

    [Fact]
    public async Task GetSettingsAsync_ReturnsSettings()
    {
        var service = new ProfileService(CreateClient((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/settings", request.RequestUri!.PathAndQuery);
            return Json(HttpStatusCode.OK, "{\"anxietyThreshold\":7,\"pushNotifications\":true,\"privateMode\":false}");
        }), JsonOptions);

        var result = await service.GetSettingsAsync();

        Assert.Equal(7, result.AnxietyThreshold);
        Assert.True(result.PushNotifications);
        Assert.False(result.PrivateMode);
    }

    [Fact]
    public async Task GetSettingsAsync_UnauthorizedThrows401()
    {
        var service = new ProfileService(CreateClient((_, _) => Json(HttpStatusCode.Unauthorized, ProblemJson(401))), JsonOptions);

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.GetSettingsAsync());

        Assert.Equal(401, exception.StatusCode);
    }

    [Fact]
    public async Task UpdateSettingsAsync_ReturnsUpdatedSettings()
    {
        var service = new ProfileService(CreateClient((request, _) =>
        {
            Assert.Equal(HttpMethod.Patch, request.Method);
            Assert.Equal("/api/settings", request.RequestUri!.PathAndQuery);
            return Json(HttpStatusCode.OK, "{\"anxietyThreshold\":9,\"pushNotifications\":false,\"privateMode\":true}");
        }), JsonOptions);

        var result = await service.UpdateSettingsAsync(new UpdateSettingsRequest
        {
            AnxietyThreshold = 9,
            PushNotifications = false,
            PrivateMode = true
        });

        Assert.Equal(9, result.AnxietyThreshold);
        Assert.False(result.PushNotifications);
        Assert.True(result.PrivateMode);
    }

    [Fact]
    public async Task UpdateSettingsAsync_InvalidRequestThrows400()
    {
        var service = new ProfileService(CreateClient((_, _) => Json(HttpStatusCode.BadRequest, ProblemJson(400))), JsonOptions);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.UpdateSettingsAsync(new UpdateSettingsRequest { AnxietyThreshold = -1 }));

        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsDashboardSummary()
    {
        var service = new DashboardService(CreateClient((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/dashboard/summary", request.RequestUri!.PathAndQuery);
            return Json(HttpStatusCode.OK, "{\"anxietyLevel\":{\"current\":42,\"trend\":\"down\"},\"weeklyRecords\":{\"used\":3,\"limit\":7},\"streakDays\":5,\"exercisesCompleted\":12}");
        }), JsonOptions);

        var result = await service.GetSummaryAsync();

        Assert.Equal(42, result.AnxietyLevel.Current);
        Assert.Equal("down", result.AnxietyLevel.Trend);
        Assert.Equal(3, result.WeeklyRecords.Used);
        Assert.Equal(7, result.WeeklyRecords.Limit);
        Assert.Equal(5, result.StreakDays);
    }

    [Fact]
    public async Task GetSummaryAsync_UnauthorizedThrows401()
    {
        var service = new DashboardService(CreateClient((_, _) => Json(HttpStatusCode.Unauthorized, ProblemJson(401))), JsonOptions);

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.GetSummaryAsync());

        Assert.Equal(401, exception.StatusCode);
    }

    [Fact]
    public async Task GetQuotaAsync_ReturnsQuota()
    {
        var service = new TokenService(CreateClient((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/tokens/quota", request.RequestUri!.PathAndQuery);
            return Json(HttpStatusCode.OK, "{\"limit\":5,\"used\":2,\"remaining\":3}");
        }), JsonOptions);

        var result = await service.GetQuotaAsync();

        Assert.Equal(5, result.Limit);
        Assert.Equal(2, result.Used);
        Assert.Equal(3, result.Remaining);
    }

    [Fact]
    public async Task GetQuotaAsync_UnauthorizedThrows401()
    {
        var service = new TokenService(CreateClient((_, _) => Json(HttpStatusCode.Unauthorized, ProblemJson(401))), JsonOptions);

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.GetQuotaAsync());

        Assert.Equal(401, exception.StatusCode);
    }

    [Fact]
    public async Task ExportTokensAsync_ReturnsFileMetadataAndContent()
    {
        var expected = Encoding.UTF8.GetBytes("code,status\nABC,pending");
        var service = new TokenService(CreateClient((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/tokens/export", request.RequestUri!.PathAndQuery);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(expected)
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = "tokens.csv" };
            return response;
        }), JsonOptions);

        var result = await service.ExportTokensAsync();

        Assert.Equal(expected, result.Content);
        Assert.Equal("tokens.csv", result.FileName);
        Assert.Equal("text/csv", result.ContentType);
    }

    [Fact]
    public async Task ExportTokensAsync_UnauthorizedThrows401()
    {
        var service = new TokenService(CreateClient((_, _) => Json(HttpStatusCode.Unauthorized, ProblemJson(401))), JsonOptions);

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.ExportTokensAsync());

        Assert.Equal(401, exception.StatusCode);
    }

    [Fact]
    public async Task ShareTokenAsync_ReturnsSentResponse()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var service = new TokenService(CreateClient((request, _) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal($"/api/tokens/{id:D}/share", request.RequestUri!.PathAndQuery);
            return Json(HttpStatusCode.OK, "{\"sent\":true}");
        }), JsonOptions);

        var result = await service.ShareTokenAsync(id, new ShareTokenRequest { RecipientEmail = "recipient@example.test" });

        Assert.True(result.Sent);
    }

    [Fact]
    public async Task ShareTokenAsync_NotFoundThrows404()
    {
        var service = new TokenService(CreateClient((_, _) => Json(HttpStatusCode.NotFound, ProblemJson(404))), JsonOptions);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            service.ShareTokenAsync(Guid.NewGuid(), new ShareTokenRequest { RecipientEmail = "recipient@example.test" }));

        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task GetBillingSummaryAsync_ReturnsSummary()
    {
        var service = new BillingService(CreateClient((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/billing/summary", request.RequestUri!.PathAndQuery);
            return Json(HttpStatusCode.OK, "{\"planId\":\"individual\",\"billingCycle\":\"monthly\",\"status\":\"active\",\"lastPayment\":null,\"transactions\":[],\"simulated\":true}");
        }), JsonOptions);

        var result = await service.GetSummaryAsync();

        Assert.Equal("individual", result.PlanId);
        Assert.Equal("monthly", result.BillingCycle);
        Assert.Equal("active", result.Status);
        Assert.Empty(result.Transactions);
    }

    [Fact]
    public async Task GetBillingSummaryAsync_UnauthorizedThrows401()
    {
        var service = new BillingService(CreateClient((_, _) => Json(HttpStatusCode.Unauthorized, ProblemJson(401))), JsonOptions);

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.GetSummaryAsync());

        Assert.Equal(401, exception.StatusCode);
    }

    [Fact]
    public async Task GetEventsAsync_ReturnsAlertEvents()
    {
        var service = new EventService(CreateClient((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/events?limit=50", request.RequestUri!.PathAndQuery);
            return Json(HttpStatusCode.OK, "[{\"eventId\":\"event-1\",\"type\":\"SOS\",\"occurredAt\":\"2026-08-25T08:30:00Z\",\"status\":\"TRIGGERED\"}]");
        }), JsonOptions);

        var result = await service.GetEventsAsync();

        var alertEvent = Assert.Single(result);
        Assert.Equal("event-1", alertEvent.EventId);
        Assert.Equal("SOS", alertEvent.Type);
        Assert.Equal("TRIGGERED", alertEvent.Status);
        Assert.Equal(DateTimeOffset.Parse("2026-08-25T08:30:00Z"), alertEvent.OccurredAt);
    }

    [Fact]
    public async Task GetEventsAsync_UnauthorizedThrows401()
    {
        var service = new EventService(CreateClient((_, _) => Json(HttpStatusCode.Unauthorized, ProblemJson(401))), JsonOptions);

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.GetEventsAsync());

        Assert.Equal(401, exception.StatusCode);
    }

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string content) => new(statusCode)
    {
        Content = new StringContent(content, Encoding.UTF8, "application/json")
    };

    private static AuthService CreateAuthService(
        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder) =>
        new(CreateClient(responder), new StubSessionManager(), JsonOptions);

    private static HttpClient CreateClient(
        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder) =>
        new(new StubHttpMessageHandler((request, cancellationToken) =>
            Task.FromResult(responder(request, cancellationToken))))
        {
            BaseAddress = new Uri("https://api.mangoon.xyz/")
        };

    private static string ProblemJson(int statusCode) =>
        $"{{\"title\":\"Request failed\",\"status\":{statusCode}}}";

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
