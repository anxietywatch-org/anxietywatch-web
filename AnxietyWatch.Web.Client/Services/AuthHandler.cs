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
        var token = _tokenStore.GetAccessToken();
        var expiresAt = _tokenStore.GetExpiresAt();
        if (requestUri is not null &&
            requestUri.Scheme == Uri.UriSchemeHttps &&
            _apiBaseAddress.IsBaseOf(requestUri) &&
            IsProtectedPath(Uri.UnescapeDataString(_apiBaseAddress.MakeRelativeUri(requestUri).GetComponents(UriComponents.Path, UriFormat.Unescaped))) &&
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
