using System.Text.Json.Serialization;

namespace AnxietyWatch.Web.Client.Models.Api;

public sealed class AlertPointDto
{
    [JsonPropertyName("eventId")]
    public string EventId { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("occurredAt")]
    public DateTimeOffset OccurredAt { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;
}
