using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AnxietyWatch.Web.Client.Models.Api;

public sealed class LinkTokenDto
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset ExpiresAt { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;
}

public sealed class CreateTokenRequest
{
    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;
}

public sealed class ShareTokenRequest
{
    [Required]
    [EmailAddress]
    [StringLength(254)]
    [JsonPropertyName("recipientEmail")]
    public string RecipientEmail { get; init; } = string.Empty;
}

public sealed record TokenExport(byte[] Content, string FileName, string ContentType);
