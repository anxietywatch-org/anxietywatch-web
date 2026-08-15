using System.Net;
using System.Text.Json;
using AnxietyWatch.Web.Client.Models.Auth;

namespace AnxietyWatch.Web.Client.Services;

public interface IAuthSessionManager
{
    UserDto? CurrentUser { get; }
    bool IsAuthenticated { get; }
    string? InitializationError { get; }
    event Action? SessionChanged;

    Task<bool> InitializeAsync();
    Task<bool> RetryInitializationAsync();
    Task<AuthResponse?> RefreshSessionAsync(CancellationToken cancellationToken = default);
    Task SetSessionAsync(AuthResponse session);
    Task UpdateUserAsync(UserDto user);
    Task ClearAsync();
}

public sealed class AuthSessionManager(
    HttpClient http,
    ITokenStore tokenStore,
    JsonSerializerOptions jsonOptions) : IAuthSessionManager
{
    private static readonly TimeSpan SessionValidationTimeout = TimeSpan.FromSeconds(10);

    private readonly object stateGate = new();
    private Task<bool>? initializationTask;
    private Task<bool>? retryTask;
    private bool isValidated;

    public UserDto? CurrentUser => IsAuthenticated ? tokenStore.GetUser() : null;
    public bool IsAuthenticated =>
        isValidated &&
        !string.IsNullOrWhiteSpace(tokenStore.GetAccessToken()) &&
        tokenStore.GetExpiresAt() > DateTimeOffset.UtcNow &&
        tokenStore.GetUser() is not null;
    public string? InitializationError { get; private set; }
    public event Action? SessionChanged;

    public Task<bool> InitializeAsync()
    {
        lock (stateGate)
        {
            return initializationTask ??= InitializeCoreAsync();
        }
    }

    public Task<bool> RetryInitializationAsync()
    {
        if (retryTask is { IsCompleted: false })
        {
            return retryTask;
        }

        retryTask = RetryCoreAsync();
        return retryTask;
    }

    private async Task<bool> RetryCoreAsync()
    {
        lock (stateGate)
        {
            initializationTask = null;
            InitializationError = null;
        }
        try
        {
            return await InitializeAsync();
        }
        finally
        {
            retryTask = null;
        }
    }

    public async Task<AuthResponse?> RefreshSessionAsync(CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync("api/auth/session", cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await ClearAsync();
            return null;
        }

        var session = await response.ReadApiAsync<AuthResponse>(jsonOptions, cancellationToken);
        await SetSessionAsync(session);
        return session;
    }

    public async Task SetSessionAsync(AuthResponse session)
    {
        if (string.IsNullOrWhiteSpace(session.Token) ||
            session.ExpiresAt <= DateTimeOffset.UtcNow ||
            session.User is null)
        {
            throw new InvalidOperationException("La respuesta de autenticación no contiene una sesión válida.");
        }

        await tokenStore.StoreAsync(session);
        lock (stateGate)
        {
            isValidated = true;
            InitializationError = null;
            initializationTask = Task.FromResult(true);
        }
        SessionChanged?.Invoke();
    }

    public async Task ClearAsync()
    {
        await tokenStore.ClearAsync();
        lock (stateGate)
        {
            isValidated = false;
            InitializationError = null;
            initializationTask = Task.FromResult(false);
        }
        SessionChanged?.Invoke();
    }

    public async Task UpdateUserAsync(UserDto user)
    {
        var token = tokenStore.GetAccessToken();
        var expiresAt = tokenStore.GetExpiresAt();
        if (string.IsNullOrWhiteSpace(token) || expiresAt is null)
        {
            return;
        }

        await SetSessionAsync(new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt.Value,
            User = user
        });
    }

    private async Task<bool> InitializeCoreAsync()
    {
        await tokenStore.RestoreFromStorageAsync();
        if (string.IsNullOrWhiteSpace(tokenStore.GetAccessToken()) ||
            tokenStore.GetExpiresAt() <= DateTimeOffset.UtcNow ||
            tokenStore.GetUser() is null)
        {
            await ClearAsync();
            return false;
        }

        try
        {
            using var timeout = new CancellationTokenSource(SessionValidationTimeout);
            return await RefreshSessionAsync(timeout.Token) is not null;
        }
        catch (ApiException exception)
        {
            InitializationError = ApiErrorMessages.For(exception, "No pudimos validar tu sesión.");
            isValidated = false;
            return false;
        }
        catch (OperationCanceledException)
        {
            InitializationError = "La validación de tu sesión tardó demasiado. Revisa tu conexión e inténtalo nuevamente.";
            isValidated = false;
            return false;
        }
        catch (HttpRequestException)
        {
            InitializationError = "No pudimos conectar con el servicio para validar tu sesión.";
            isValidated = false;
            return false;
        }
        catch (InvalidOperationException)
        {
            InitializationError = "El servicio devolvió una sesión inválida. Intenta iniciar sesión nuevamente.";
            isValidated = false;
            return false;
        }
    }
}
