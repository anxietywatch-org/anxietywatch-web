using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AnxietyWatch.Web.Client.Models.Api;

public sealed class RedeemCaregiverCodeRequest
{
    [Required, JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [Required, JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = string.Empty;
}

public sealed class CaregiverActivationRequest
{
    [Required, EmailAddress, JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(64, MinimumLength = 8), JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public sealed class CaregiverPatientDto
{
    public string PatientId { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public string Role { get; init; } = string.Empty;
    public DateTimeOffset LinkedAt { get; init; }
}

public sealed class CaregiverPatientDetailDto
{
    public string PatientId { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
}

public sealed class CaregiverEpisodeDto
{
    public DateTimeOffset Date { get; init; }
    public int Intensity { get; init; }
    public string? Symptoms { get; init; }
    public string? Notes { get; init; }
    public bool DetailsHidden { get; init; }
}

public sealed class CaregiverEventDto
{
    public string EventId { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public DateTimeOffset OccurredAt { get; init; }
    public string Status { get; init; } = string.Empty;
}

public sealed class LatestHeartRateDto
{
    public int HeartRateBpm { get; init; }
    public DateTimeOffset MeasuredAt { get; init; }
    public long AgeSeconds { get; init; }
    public string Quality { get; init; } = string.Empty;
}

public sealed class NoLatestHeartRate
{
    private NoLatestHeartRate() { }
    public static NoLatestHeartRate Value { get; } = new();
}
