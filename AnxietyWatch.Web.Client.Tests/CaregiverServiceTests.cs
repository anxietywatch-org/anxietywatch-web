using System.Net;
using System.Text.Json;
using AnxietyWatch.Web.Client.Models.Api;
using AnxietyWatch.Web.Client.Services;
using Xunit;

namespace AnxietyWatch.Web.Client.Tests;

public sealed class CaregiverServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GetLatestHeartRateAsync_204ReturnsNoData()
    {
        var service = new CaregiverService(Client((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/caregiver/patients/patient-1/heart-rate/latest", request.RequestUri!.PathAndQuery);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        }), JsonOptions);

        var result = await service.GetLatestHeartRateAsync("patient-1");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetEpisodesAsync_UsesSupportedRangeAndPrivateModeContract()
    {
        var service = new CaregiverService(Client((request, _) =>
        {
            Assert.Equal("/api/caregiver/patients/patient-1/episodes?range=30", request.RequestUri!.PathAndQuery);
            return Task.FromResult(Json(HttpStatusCode.OK,
                "[{\"date\":\"2026-08-25T12:00:00Z\",\"intensity\":4,\"symptoms\":null,\"notes\":null,\"detailsHidden\":true}]"));
        }), JsonOptions);

        var result = await service.GetEpisodesAsync("patient-1", 30);

        Assert.Single(result);
        Assert.True(result[0].DetailsHidden);
        Assert.Null(result[0].Symptoms);
        Assert.Null(result[0].Notes);
    }

    [Fact]
    public async Task GetEventsAsync_PreservesSupportRequestedAsNonSosEvent()
    {
        var service = new CaregiverService(Client((request, _) =>
        {
            Assert.Equal("/api/caregiver/patients/patient-1/events?limit=50", request.RequestUri!.PathAndQuery);
            return Task.FromResult(Json(HttpStatusCode.OK,
                "[{\"eventId\":\"event-1\",\"type\":\"SUPPORT_REQUESTED\",\"occurredAt\":\"2026-08-25T12:00:00Z\",\"status\":\"DETECTED\"}]"));
        }), JsonOptions);

        var result = await service.GetEventsAsync("patient-1");

        Assert.Equal("SUPPORT_REQUESTED", result[0].Type);
        Assert.NotEqual("SOS", result[0].Type);
    }

    [Fact]
    public async Task GetPatientAsync_ForbiddenThrowsApiException()
    {
        var service = new CaregiverService(Client((_, _) =>
            Task.FromResult(Json(HttpStatusCode.Forbidden, "{\"status\":403,\"title\":\"Forbidden\"}"))), JsonOptions);

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.GetPatientAsync("patient-1"));

        Assert.Equal(403, exception.StatusCode);
    }

    private static HttpClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) =>
        new(new StubHttpMessageHandler(handler)) { BaseAddress = new Uri("https://api.mangoon.xyz/") };

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            responder(request, cancellationToken);
    }
}
