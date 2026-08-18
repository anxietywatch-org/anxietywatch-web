using System.Reflection;
using AnxietyWatch.Web.Client.Pages.Auth;
using Xunit;

namespace AnxietyWatch.Web.Client.Tests;

public sealed class AuthTokenFlowTests
{
    [Fact]
    public void ResolveToken_HashToken_UsesConsumedToken()
    {
        var resolved = ResolveToken("VALID_TOKEN", null);
        Assert.Equal("VALID_TOKEN", resolved);
    }

    [Fact]
    public void ResolveToken_QueryTokenFallback_UsesQueryToken()
    {
        var resolved = ResolveToken(string.Empty, "VALID_TOKEN");
        Assert.Equal("VALID_TOKEN", resolved);
    }

    [Fact]
    public void ResolveToken_NoToken_ReturnsNull()
    {
        var resolved = ResolveToken(string.Empty, null);
        Assert.Null(resolved);
    }

    [Fact]
    public void SharedScript_RemovesTokenFromUrlAfterCapture()
    {
        var script = File.ReadAllText(VerifyEmailScriptPath);

        Assert.Contains("window.history.replaceState", script, StringComparison.Ordinal);
        Assert.Contains("url.hash = \"\";", script, StringComparison.Ordinal);
        Assert.Contains("url.searchParams.delete(\"token\");", script, StringComparison.Ordinal);
    }

    [Fact]
    public void ResetPassword_UsesCapturedTokenForResetRequest()
    {
        var page = File.ReadAllText(ResetPasswordPagePath);
        Assert.Contains("Token = resolvedToken", page, StringComparison.Ordinal);
    }

    [Fact]
    public void VerifyEmail_ContinuesUsingCompatibilityWrapper()
    {
        var page = File.ReadAllText(VerifyEmailPagePath);
        var script = File.ReadAllText(VerifyEmailScriptPath);

        Assert.Contains("anxietyWatch.emailVerification.consumeToken", page, StringComparison.Ordinal);
        Assert.Contains("window.anxietyWatch.emailVerification", script, StringComparison.Ordinal);
        Assert.Contains("return window.anxietyWatch.authToken.consume();", script, StringComparison.Ordinal);
    }

    private static string? ResolveToken(string? consumedToken, string? queryToken)
    {
        var method = typeof(ResetPassword).GetMethod(
            "ResolveToken",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        return (string?)method!.Invoke(null, [consumedToken, queryToken]);
    }

    private static string RepositoryRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static string VerifyEmailScriptPath =>
        Path.Combine(RepositoryRoot, "AnxietyWatch.Web.Client", "wwwroot", "verify-email.js");

    private static string ResetPasswordPagePath =>
        Path.Combine(RepositoryRoot, "AnxietyWatch.Web.Client", "Pages", "Auth", "ResetPassword.razor");

    private static string VerifyEmailPagePath =>
        Path.Combine(RepositoryRoot, "AnxietyWatch.Web.Client", "Pages", "Auth", "VerifyEmail.razor");
}
