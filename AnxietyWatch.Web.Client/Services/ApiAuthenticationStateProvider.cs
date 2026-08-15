using System.Security.Claims;
using AnxietyWatch.Web.Client.Models.Auth;
using Microsoft.AspNetCore.Components.Authorization;

namespace AnxietyWatch.Web.Client.Services;

/// <summary>
/// Proveedor de estado que actualiza la identidad de la UI según la sesión
/// guardada en <see cref="ITokenStore"/> (activa/cerrada).
/// </summary>
public class ApiAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly IAuthSessionManager _sessionManager;

    public ApiAuthenticationStateProvider(IAuthSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
        _sessionManager.SessionChanged += NotifyAuthenticationStateChanged;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await _sessionManager.InitializeAsync();
        var user = _sessionManager.CurrentUser;
        if (user is null)
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var identity = new ClaimsIdentity(
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("planId", user.PlanId)
            ],
            authenticationType: "Bearer");

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    /// <summary>Notifica a la UI que el estado de sesión cambió (login/logout).</summary>
    public void NotifyAuthenticationStateChanged() =>
        base.NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    public void Dispose() => _sessionManager.SessionChanged -= NotifyAuthenticationStateChanged;
}
