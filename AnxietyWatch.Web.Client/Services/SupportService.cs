using System.Net.Http.Json;
using System.Text.Json;
using AnxietyWatch.Web.Client.Models.Api;

namespace AnxietyWatch.Web.Client.Services;

public interface ISupportService
{
    Task<SupportTicketDto> CreateTicketAsync(
        CreateSupportTicketRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class SupportService(HttpClient http, JsonSerializerOptions jsonOptions) : ISupportService
{
    public async Task<SupportTicketDto> CreateTicketAsync(
        CreateSupportTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync(
            "api/support/tickets",
            request,
            jsonOptions,
            cancellationToken);
        return await response.ReadApiAsync<SupportTicketDto>(jsonOptions, cancellationToken);
    }
}
