using AnxietyWatch.Web.Client.Models.Auth;
using AnxietyWatch.Web.Client.Pages.Auth;
using AnxietyWatch.Web.Client.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AnxietyWatch.Web.Client.Tests;

public sealed class AuthTokenFlowTests : TestContext
{
    [Fact]
    public void ResetPassword_HashTokenFlow_ShowsResolvingThenForm()
    {
        var auth = new RecordingAuthService();
        Services.AddSingleton<IAuthService>(auth);
        var plannedInvocation = JSInterop.Setup<string>("anxietyWatch.authToken.consume");

        var cut = RenderComponent<ResetPassword>();

        Assert.Contains("Validando tu enlace de recuperación", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("no contiene un token válido", cut.Markup, StringComparison.OrdinalIgnoreCase);

        plannedInvocation.SetResult("HASH_TOKEN");

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("form")));
    }

    [Fact]
    public void ResetPassword_QueryTokenFallback_KeepsTokenUsable()
    {
        var auth = new RecordingAuthService();
        Services.AddSingleton<IAuthService>(auth);
        var plannedInvocation = JSInterop.Setup<string>("anxietyWatch.authToken.consume");

        var cut = RenderComponent<ResetPassword>(parameters =>
            parameters.Add(component => component.QueryToken, "QUERY_TOKEN"));

        plannedInvocation.SetResult(string.Empty);

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("form")));

        cut.Find("#new-password").Change("NewPassword123!");
        cut.Find("#confirm-password").Change("NewPassword123!");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(auth.ResetRequest);
            Assert.Equal("QUERY_TOKEN", auth.ResetRequest!.Token);
        });
    }

    [Fact]
    public void ResetPassword_NoToken_ShowsInvalidTokenOnlyAfterResolution()
    {
        var auth = new RecordingAuthService();
        Services.AddSingleton<IAuthService>(auth);
        var plannedInvocation = JSInterop.Setup<string>("anxietyWatch.authToken.consume");

        var cut = RenderComponent<ResetPassword>();

        Assert.Contains("Validando tu enlace de recuperación", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("no contiene un token válido", cut.Markup, StringComparison.OrdinalIgnoreCase);

        plannedInvocation.SetResult(string.Empty);

        cut.WaitForAssertion(() =>
            Assert.Contains("no contiene un token válido", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ResetPassword_SubmitsCapturedToken()
    {
        var auth = new RecordingAuthService();
        Services.AddSingleton<IAuthService>(auth);
        JSInterop.Setup<string>("anxietyWatch.authToken.consume").SetResult("CAPTURED_TOKEN");

        var cut = RenderComponent<ResetPassword>();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("form")));

        cut.Find("#new-password").Change("NewPassword123!");
        cut.Find("#confirm-password").Change("NewPassword123!");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(auth.ResetRequest);
            Assert.Equal("CAPTURED_TOKEN", auth.ResetRequest!.Token);
        });
    }

    [Fact]
    public void VerifyEmail_StillConsumesTokenAndCallsService()
    {
        var auth = new RecordingAuthService();
        Services.AddSingleton<IAuthService>(auth);
        Services.AddSingleton<IAuthSessionManager>(new StubSessionManager());
        JSInterop.Setup<string>("anxietyWatch.emailVerification.consumeToken").SetResult("VERIFY_TOKEN");

        var cut = RenderComponent<VerifyEmail>();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(auth.VerifyRequest);
            Assert.Equal("VERIFY_TOKEN", auth.VerifyRequest!.Token);
            Assert.Contains("Correo verificado correctamente", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void SharedTokenScript_KeepsWrapperAndUrlCleanupContract()
    {
        var scriptPath = Path.Combine(
            RepositoryRoot,
            "AnxietyWatch.Web.Client",
            "wwwroot",
            "verify-email.js");
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("window.anxietyWatch.emailVerification", script, StringComparison.Ordinal);
        Assert.Contains("return window.anxietyWatch.authToken.consume();", script, StringComparison.Ordinal);
        Assert.Contains("window.history.replaceState", script, StringComparison.Ordinal);
        Assert.Contains("url.hash = \"\";", script, StringComparison.Ordinal);
        Assert.Contains("url.searchParams.delete(\"token\");", script, StringComparison.Ordinal);
    }

    private static string RepositoryRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private sealed class RecordingAuthService : IAuthService
    {
        public ResetPasswordRequest? ResetRequest { get; private set; }
        public EmailVerificationConfirmRequest? VerifyRequest { get; private set; }

        public Task<AuthResponse> LoginAsync(LoginRequest request) => throw new NotImplementedException();
        public Task<AuthResponse> RegisterAsync(RegisterRequest request) => throw new NotImplementedException();
        public Task LogoutAsync() => throw new NotImplementedException();
        public Task<AuthResponse?> GetSessionAsync() => throw new NotImplementedException();
        public Task<AuthMessageResponse> ForgotPasswordAsync(ForgotPasswordRequest request) => throw new NotImplementedException();

        public Task<AuthMessageResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            ResetRequest = request;
            return Task.FromResult(new AuthMessageResponse { Message = "ok" });
        }

        public Task<AuthMessageResponse> ConfirmEmailVerificationAsync(
            EmailVerificationConfirmRequest request,
            CancellationToken cancellationToken = default)
        {
            VerifyRequest = request;
            return Task.FromResult(new AuthMessageResponse { Message = "ok" });
        }

        public Task<EmailVerificationStatusResponse> GetEmailVerificationStatusAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<AuthMessageResponse> ResendEmailVerificationAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class StubSessionManager : IAuthSessionManager
    {
        public UserDto? CurrentUser => null;
        public bool IsAuthenticated => false;
        public string? InitializationError => null;
        public event Action? SessionChanged { add { } remove { } }
        public Task<bool> InitializeAsync() => Task.FromResult(false);
        public Task<bool> RetryInitializationAsync() => Task.FromResult(false);
        public Task<AuthResponse?> RefreshSessionAsync(CancellationToken cancellationToken = default) => Task.FromResult<AuthResponse?>(null);
        public Task SetSessionAsync(AuthResponse session) => Task.CompletedTask;
        public Task UpdateUserAsync(UserDto user) => Task.CompletedTask;
        public Task ClearAsync() => Task.CompletedTask;
    }
}
