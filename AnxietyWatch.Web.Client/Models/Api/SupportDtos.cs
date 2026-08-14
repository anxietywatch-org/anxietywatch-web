using System.Text.Json.Serialization;

namespace AnxietyWatch.Web.Client.Models.Api;

public sealed record CreateSupportTicketRequest(
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("priority")] string Priority,
    [property: JsonPropertyName("message")] string Message);

public sealed record SupportTicketDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("priority")] string Priority,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt);
