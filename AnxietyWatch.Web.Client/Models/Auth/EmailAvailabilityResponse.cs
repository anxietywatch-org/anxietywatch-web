using System.Text.Json.Serialization;

namespace AnxietyWatch.Web.Client.Models.Auth;

public sealed class EmailAvailabilityResponse
{
    [JsonPropertyName("available")]
    public bool Available { get; init; }
}
