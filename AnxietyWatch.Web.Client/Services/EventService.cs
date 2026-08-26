using System.Text.Json;
using AnxietyWatch.Web.Client.Models.Api;

namespace AnxietyWatch.Web.Client.Services;

public interface IEventService
{
    Task<IReadOnlyList<AlertPointDto>> GetEventsAsync(int limit = 50, CancellationToken cancellationToken = default);
}

public sealed class EventService(HttpClient http, JsonSerializerOptions jsonOptions) : IEventService
{
    public async Task<IReadOnlyList<AlertPointDto>> GetEventsAsync(
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync($"api/events?limit={limit}", cancellationToken);
        return await response.ReadApiAsync<IReadOnlyList<AlertPointDto>>(jsonOptions, cancellationToken);
    }
}
