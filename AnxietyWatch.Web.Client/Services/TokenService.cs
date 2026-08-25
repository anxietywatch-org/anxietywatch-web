using System.Net.Http.Json;
using System.Text.Json;
using AnxietyWatch.Web.Client.Models.Api;

namespace AnxietyWatch.Web.Client.Services;

public interface ITokenService
{
    Task<IReadOnlyList<LinkTokenDto>> GetTokensAsync(CancellationToken cancellationToken = default);
    Task<TokenQuotaDto> GetQuotaAsync(CancellationToken cancellationToken = default);
    Task<LinkTokenDto> CreateTokenAsync(CreateTokenRequest request, CancellationToken cancellationToken = default);
    Task<LinkTokenDto> RotateTokenAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SuccessResponse> DeleteTokenAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SuccessResponse> RevokeTokenAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SentResponse> ShareTokenAsync(Guid id, ShareTokenRequest request, CancellationToken cancellationToken = default);
    Task<TokenExport> ExportTokensAsync(CancellationToken cancellationToken = default);
}

public sealed class TokenService(HttpClient http, JsonSerializerOptions jsonOptions) : ITokenService
{
    public async Task<IReadOnlyList<LinkTokenDto>> GetTokensAsync(CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync("api/tokens", cancellationToken);
        return await response.ReadApiAsync<IReadOnlyList<LinkTokenDto>>(jsonOptions, cancellationToken);
    }

    public async Task<TokenQuotaDto> GetQuotaAsync(CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync("api/tokens/quota", cancellationToken);
        return await response.ReadApiAsync<TokenQuotaDto>(jsonOptions, cancellationToken);
    }

    public async Task<LinkTokenDto> CreateTokenAsync(
        CreateTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync("api/tokens", request, jsonOptions, cancellationToken);
        return await response.ReadApiAsync<LinkTokenDto>(jsonOptions, cancellationToken);
    }

    public async Task<LinkTokenDto> RotateTokenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsync($"api/tokens/{id:D}/rotate", content: null, cancellationToken);
        return await response.ReadApiAsync<LinkTokenDto>(jsonOptions, cancellationToken);
    }

    public async Task<SuccessResponse> DeleteTokenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await http.DeleteAsync($"api/tokens/{id:D}", cancellationToken);
        return await response.ReadApiAsync<SuccessResponse>(jsonOptions, cancellationToken);
    }

    public async Task<SuccessResponse> RevokeTokenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsync($"api/tokens/{id:D}/revoke", content: null, cancellationToken);
        return await response.ReadApiAsync<SuccessResponse>(jsonOptions, cancellationToken);
    }

    public async Task<SentResponse> ShareTokenAsync(
        Guid id,
        ShareTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync(
            $"api/tokens/{id:D}/share",
            request,
            jsonOptions,
            cancellationToken);
        return await response.ReadApiAsync<SentResponse>(jsonOptions, cancellationToken);
    }

    public async Task<TokenExport> ExportTokensAsync(CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync("api/tokens/export", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await response.ReadApiAsync<object>(jsonOptions, cancellationToken);
        }

        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var disposition = response.Content.Headers.ContentDisposition;
        var fileName = disposition?.FileNameStar ?? disposition?.FileName?.Trim('"') ?? "tokens.csv";
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        return new TokenExport(content, fileName, contentType);
    }
}
