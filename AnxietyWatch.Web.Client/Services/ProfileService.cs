using System.Net.Http.Json;
using System.Text.Json;
using AnxietyWatch.Web.Client.Models.Api;

namespace AnxietyWatch.Web.Client.Services;

public interface IProfileService
{
    Task<ProfileResponse> GetProfileAsync(CancellationToken cancellationToken = default);
    Task<SettingsResponse> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<ProfileResponse> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<SettingsResponse> UpdateSettingsAsync(UpdateSettingsRequest request, CancellationToken cancellationToken = default);
    Task<MessageResponse> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
}

public sealed class ProfileService(
    HttpClient http,
    IAuthSessionManager sessionManager,
    JsonSerializerOptions jsonOptions) : IProfileService
{
    public async Task<ProfileResponse> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync("api/profile", cancellationToken);
        return await response.ReadApiAsync<ProfileResponse>(jsonOptions, cancellationToken);
    }

    public async Task<SettingsResponse> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync("api/settings", cancellationToken);
        return await response.ReadApiAsync<SettingsResponse>(jsonOptions, cancellationToken);
    }

    public async Task<ProfileResponse> UpdateProfileAsync(
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await http.PatchAsJsonAsync("api/profile", request, jsonOptions, cancellationToken);
        return await response.ReadApiAsync<ProfileResponse>(jsonOptions, cancellationToken);
    }

    public async Task<SettingsResponse> UpdateSettingsAsync(
        UpdateSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await http.PatchAsJsonAsync("api/settings", request, jsonOptions, cancellationToken);
        return await response.ReadApiAsync<SettingsResponse>(jsonOptions, cancellationToken);
    }

    public async Task<MessageResponse> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync(
            "api/auth/change-password",
            request,
            jsonOptions,
            cancellationToken);
        var result = await response.ReadApiAsync<MessageResponse>(jsonOptions, cancellationToken);
        await sessionManager.ClearAsync();
        return result;
    }
}
