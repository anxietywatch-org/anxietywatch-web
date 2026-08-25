using System.Text.Json;
using AnxietyWatch.Web.Client.Models.Auth;
using Microsoft.JSInterop;

namespace AnxietyWatch.Web.Client.Services;

/// <summary>
/// Guarda el token y el usuario de la sesión actual y los expone al
/// <see cref="AuthHandler"/> y al proveedor de estado de autenticación.
/// </summary>
public interface ITokenStore
{
    Task StoreAsync(AuthResponse session);
    Task RestoreFromStorageAsync();
    Task ClearAsync();
    string? GetAccessToken();
    UserDto? GetUser();
    DateTimeOffset? GetExpiresAt();
}

/// <summary>
/// Implementación en memoria persistida a <c>localStorage</c> cuando hay
/// interop JS disponible (para sobrevivir a recargas del navegador).
/// </summary>
public class TokenStore : ITokenStore
{
    private const string TokenKey = "anxietywatch_token";
    private const string ExpiresKey = "anxietywatch_expires";
    private const string UserKey = "anxietywatch_user";

    private readonly IJSRuntime _js;
    private string? _token;
    private DateTimeOffset? _expiresAt;
    private UserDto? _user;

    public TokenStore(IJSRuntime js) => _js = js;

    public async Task StoreAsync(AuthResponse auth)
    {
        _token = auth.Token;
        _expiresAt = auth.ExpiresAt;
        _user = auth.User;

        try
        {
            await PersistAsync($"localStorage.setItem", TokenKey, auth.Token);
            await PersistAsync("localStorage.setItem", ExpiresKey, auth.ExpiresAt.ToString("o"));
            await PersistAsync("localStorage.setItem", UserKey, JsonSerializer.Serialize(auth.User));
        }
        catch
        {
            // La sesión sigue activa en memoria aunque la persistencia falle.
        }
    }

    public async Task ClearAsync()
    {
        _token = null;
        _expiresAt = null;
        _user = null;

        await RemovePersistedValueAsync(TokenKey);
        await RemovePersistedValueAsync(ExpiresKey);
        await RemovePersistedValueAsync(UserKey);
    }

    public string? GetAccessToken() => _token;

    public UserDto? GetUser() => _user;

    public DateTimeOffset? GetExpiresAt() => _expiresAt;

    /// <summary>Lee y cachea la sesión guardada en <c>localStorage</c> (restauración tras recarga).</summary>
    public async Task RestoreFromStorageAsync()
    {
        if (_token is not null && _expiresAt > DateTimeOffset.UtcNow && _user is not null) return;

        try
        {
            var token = await _js.InvokeAsync<string?>("localStorage.getItem", TokenKey);
            var expiresRaw = await _js.InvokeAsync<string?>("localStorage.getItem", ExpiresKey);
            var userRaw = await _js.InvokeAsync<string?>("localStorage.getItem", UserKey);

            if (string.IsNullOrWhiteSpace(token) ||
                !DateTimeOffset.TryParse(expiresRaw, out var expiresAt) ||
                expiresAt <= DateTimeOffset.UtcNow ||
                string.IsNullOrWhiteSpace(userRaw))
            {
                await ClearAsync();
                return;
            }

            var user = JsonSerializer.Deserialize<UserDto>(userRaw);
            if (user is null)
            {
                await ClearAsync();
                return;
            }

            _token = token;
            _expiresAt = expiresAt;
            _user = user;
        }
        catch (Exception exception) when (exception is JSException or JsonException or InvalidOperationException)
        {
            // Sin interop (prerender) no se restaura nada.
        }
    }

    private Task PersistAsync(string method, string key, string value) =>
        _js.InvokeVoidAsync(method, key, value).AsTask();

    private async Task RemovePersistedValueAsync(string key)
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch
        {
            // Cada clave es best-effort; un fallo no impide limpiar las restantes.
        }
    }
}
