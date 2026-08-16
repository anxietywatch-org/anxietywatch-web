using System.Net.Http.Headers;

namespace AnxietyWatch.Web.Client.Services;

/// <summary>
/// Inyecta <c>Authorization: Bearer &lt;token&gt;</c> exclusivamente en rutas
/// protegidas del origen HTTPS configurado.
/// </summary>
public class AuthHandler : DelegatingHandler
{
    private static readonly string[] ProtectedPaths =
    [
        "api/dashboard",
        "api/episodes",
        "api/tokens",
        "api/profile",
        "api/settings",
        "api/support",
        "api/billing",
        "api/auth/session",
        "api/auth/logout",
        "api/auth/change-password",
        "api/auth/verify-email/status",
        "api/auth/verify-email/resend"
    ];

    private readonly ITokenStore _tokenStore;
    private readonly Uri _apiBaseAddress;

    public AuthHandler(ITokenStore tokenStore, Uri apiBaseAddress)
    {
        _tokenStore = tokenStore;
        _apiBaseAddress = apiBaseAddress;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var requestUri = request.RequestUri;
        var absoluteRequestUri = requestUri switch
        {
            null => null,
            { IsAbsoluteUri: true } => requestUri,
            _ => new Uri(_apiBaseAddress, requestUri)
        };
        var token = _tokenStore.GetAccessToken();
        var expiresAt = _tokenStore.GetExpiresAt();
        if (absoluteRequestUri is not null &&
            _apiBaseAddress.IsBaseOf(absoluteRequestUri) &&
            IsProtectedPath(Uri.UnescapeDataString(absoluteRequestUri.AbsolutePath.TrimStart('/'))) &&
            !string.IsNullOrWhiteSpace(token) &&
            expiresAt > DateTimeOffset.UtcNow)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }

    private static bool IsProtectedPath(string path) => ProtectedPaths.Any(protectedPath =>
        path.Equals(protectedPath, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith($"{protectedPath}/", StringComparison.OrdinalIgnoreCase));
}
