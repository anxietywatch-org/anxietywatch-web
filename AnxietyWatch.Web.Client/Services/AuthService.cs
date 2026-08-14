using System.Net.Http.Json;
using System.Text.Json;
using AnxietyWatch.Web.Client.Models.Auth;

namespace AnxietyWatch.Web.Client.Services;

/// <summary>
/// Implementación real de <see cref="IAuthService"/> contra el backend REST.
/// Tras login/register guarda la sesión y avisa al proveedor de estado.
/// </summary>
public class AuthService(
    HttpClient http,
    IAuthSessionManager sessionManager,
    JsonSerializerOptions jsonOptions) : IAuthService
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        using var response = await http.PostAsJsonAsync("api/auth/login", request, jsonOptions);
        var session = await response.ReadApiAsync<AuthResponse>(jsonOptions);
        await sessionManager.SetSessionAsync(session);
        return session;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        using var response = await http.PostAsJsonAsync("api/auth/register", request, jsonOptions);
        var session = await response.ReadApiAsync<AuthResponse>(jsonOptions);
        await sessionManager.SetSessionAsync(session);
        return session;
    }

    public async Task LogoutAsync()
    {
        try
        {
            using var response = await http.PostAsync("api/auth/logout", null);
            response.EnsureSuccessStatusCode();
        }
        catch
        {
            // La revocación es best-effort; se cierra la sesión local igualmente.
        }

        await sessionManager.ClearAsync();
    }

    public async Task<AuthResponse?> GetSessionAsync()
    {
        return await sessionManager.RefreshSessionAsync();
    }

    public async Task<AuthMessageResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        using var response = await http.PostAsJsonAsync("api/auth/password/forgot", request, jsonOptions);
        return await response.ReadApiAsync<AuthMessageResponse>(jsonOptions);
    }

    public async Task<AuthMessageResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        using var response = await http.PostAsJsonAsync("api/auth/password/reset", request, jsonOptions);
        return await response.ReadApiAsync<AuthMessageResponse>(jsonOptions);
    }

    public async Task<AuthMessageResponse> ConfirmEmailVerificationAsync(EmailVerificationConfirmRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync("api/auth/verify-email/confirm", request, jsonOptions, cancellationToken);
        return await response.ReadApiAsync<AuthMessageResponse>(jsonOptions, cancellationToken);
    }

    public async Task<EmailVerificationStatusResponse> GetEmailVerificationStatusAsync(CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync("api/auth/verify-email/status", cancellationToken);
        return await response.ReadApiAsync<EmailVerificationStatusResponse>(jsonOptions, cancellationToken);
    }

    public async Task<AuthMessageResponse> ResendEmailVerificationAsync(CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsync("api/auth/verify-email/resend", null, cancellationToken);
        return await response.ReadApiAsync<AuthMessageResponse>(jsonOptions, cancellationToken);
    }
}
