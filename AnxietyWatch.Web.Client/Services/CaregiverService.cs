using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AnxietyWatch.Web.Client.Models.Api;
using AnxietyWatch.Web.Client.Models.Auth;

namespace AnxietyWatch.Web.Client.Services;

public interface ICaregiverService
{
    Task<AuthResponse> RedeemCodeAsync(RedeemCaregiverCodeRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> ActivateAsync(CaregiverActivationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CaregiverPatientDto>> GetPatientsAsync(CancellationToken cancellationToken = default);
    Task<CaregiverPatientDetailDto> GetPatientAsync(string patientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CaregiverEpisodeDto>> GetEpisodesAsync(string patientId, int range = 7, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CaregiverEventDto>> GetEventsAsync(string patientId, int limit = 50, CancellationToken cancellationToken = default);
    Task<LatestHeartRateDto?> GetLatestHeartRateAsync(string patientId, CancellationToken cancellationToken = default);
    Task<SuccessResponse> RevokeAsync(Guid tokenId, CancellationToken cancellationToken = default);
}

public sealed class CaregiverService(HttpClient http, JsonSerializerOptions jsonOptions) : ICaregiverService
{
    public async Task<AuthResponse> RedeemCodeAsync(RedeemCaregiverCodeRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync("api/tokens/accept-by-code", request, jsonOptions, cancellationToken);
        return await response.ReadApiAsync<AuthResponse>(jsonOptions, cancellationToken);
    }

    public async Task<AuthResponse> ActivateAsync(CaregiverActivationRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync("api/auth/caregiver/activate", request, jsonOptions, cancellationToken);
        return await response.ReadApiAsync<AuthResponse>(jsonOptions, cancellationToken);
    }

    public async Task<IReadOnlyList<CaregiverPatientDto>> GetPatientsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync("api/caregiver/patients", cancellationToken);
        return await response.ReadApiAsync<IReadOnlyList<CaregiverPatientDto>>(jsonOptions, cancellationToken);
    }

    public async Task<CaregiverPatientDetailDto> GetPatientAsync(string patientId, CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync($"api/caregiver/patients/{Uri.EscapeDataString(patientId)}", cancellationToken);
        return await response.ReadApiAsync<CaregiverPatientDetailDto>(jsonOptions, cancellationToken);
    }

    public async Task<IReadOnlyList<CaregiverEpisodeDto>> GetEpisodesAsync(string patientId, int range = 7, CancellationToken cancellationToken = default)
    {
        if (range is not (7 or 30 or 90)) throw new ArgumentOutOfRangeException(nameof(range));
        using var response = await http.GetAsync($"api/caregiver/patients/{Uri.EscapeDataString(patientId)}/episodes?range={range}", cancellationToken);
        return await response.ReadApiAsync<IReadOnlyList<CaregiverEpisodeDto>>(jsonOptions, cancellationToken);
    }

    public async Task<IReadOnlyList<CaregiverEventDto>> GetEventsAsync(string patientId, int limit = 50, CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(limit));
        using var response = await http.GetAsync($"api/caregiver/patients/{Uri.EscapeDataString(patientId)}/events?limit={limit}", cancellationToken);
        return await response.ReadApiAsync<IReadOnlyList<CaregiverEventDto>>(jsonOptions, cancellationToken);
    }

    public async Task<LatestHeartRateDto?> GetLatestHeartRateAsync(string patientId, CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync($"api/caregiver/patients/{Uri.EscapeDataString(patientId)}/heart-rate/latest", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NoContent) return null;
        return await response.ReadApiAsync<LatestHeartRateDto>(jsonOptions, cancellationToken);
    }

    public async Task<SuccessResponse> RevokeAsync(Guid tokenId, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsync($"api/tokens/{tokenId:D}/revoke", null, cancellationToken);
        return await response.ReadApiAsync<SuccessResponse>(jsonOptions, cancellationToken);
    }
}
