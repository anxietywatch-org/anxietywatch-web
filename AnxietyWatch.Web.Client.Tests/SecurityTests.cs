using AnxietyWatch.Web.Client.Models.Api;
using AnxietyWatch.Web.Client.Models.Auth;
using AnxietyWatch.Web.Client.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using Xunit;
using LandingPage = AnxietyWatch.Web.Client.Pages.Landing;
using ResetPasswordPage = AnxietyWatch.Web.Client.Pages.Auth.ResetPassword;

namespace AnxietyWatch.Web.Client.Tests;

public sealed class SecurityTests
{
    private const string SecretToken = "RESET_TOKEN_DO_NOT_DISPLAY";
    private const string SecretPassword = "Password-Do-Not-Display!";

    [Theory]
    [InlineData(400, "Revisa los datos ingresados e inténtalo nuevamente.")]
    [InlineData(404, "No encontramos el recurso solicitado.")]
    [InlineData(409, "La operación entra en conflicto con el estado actual.")]
    [InlineData(410, "El recurso solicitado ya no está disponible.")]
    [InlineData(418, "No pudimos completar la operación de prueba.")]
    public void ApiErrorMessages_DoesNotExposeUntrustedProblemTitle(int statusCode, string expected)
    {
        var exception = new ApiException(
            new ApiProblemDetails
            {
                Status = statusCode,
                Title = $"token={SecretToken}; password={SecretPassword}; InvalidOperationException"
            },
            statusCode);

        var result = ApiErrorMessages.For(exception, "No pudimos completar la operación de prueba.");

        Assert.Equal(expected, result);
        Assert.DoesNotContain(SecretToken, result, StringComparison.Ordinal);
        Assert.DoesNotContain(SecretPassword, result, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", result, StringComparison.Ordinal);
    }

    [Fact]
    public void ResetPassword_ApiFailureDoesNotExposeTokenOrPasswordInError()
    {
        using var ctx = new BunitContext();
        var auth = new Mock<IAuthService>();
        auth.Setup(service => service.ResetPasswordAsync(It.IsAny<ResetPasswordRequest>()))
            .ThrowsAsync(new ApiException(
                new ApiProblemDetails
                {
                    Status = 400,
                    Title = $"token={SecretToken}; password={SecretPassword}"
                },
                400));
        ctx.Services.AddSingleton(auth.Object);
        ctx.JSInterop.Setup<string>("anxietyWatch.authToken.consume").SetResult(SecretToken);

        var cut = ctx.Render<ResetPasswordPage>();
        cut.WaitForElement("form.reset-form");
        cut.Find("#new-password").Change(SecretPassword);
        cut.Find("#confirm-password").Change(SecretPassword);
        cut.Find("form.reset-form").Submit();

        cut.WaitForAssertion(() =>
        {
            var error = cut.Find(".reset-error[role='alert']").TextContent;
            Assert.Equal("Revisa los datos ingresados e inténtalo nuevamente.", error.Trim());
            Assert.DoesNotContain(SecretToken, error, StringComparison.Ordinal);
            Assert.DoesNotContain(SecretPassword, error, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Landing_EscapesApiSuppliedHtml()
    {
        const string question = "<img id=\"xss-question\" src=x onerror=\"alert(1)\">";
        const string answer = "<script id=\"xss-answer\">alert(document.domain)</script>";
        using var ctx = new BunitContext();
        var plans = new Mock<IPlanService>();
        var content = new Mock<IContentService>();
        plans.Setup(service => service.GetPlansAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PlanDto>());
        content.Setup(service => service.GetFaqAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new FaqDto { Question = question, Answer = answer }]);
        content.Setup(service => service.GetTestimonialsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TestimonialDto>());
        ctx.Services.AddSingleton(plans.Object);
        ctx.Services.AddSingleton(content.Object);

        var cut = ctx.Render<LandingPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(question, cut.Find(".faq-list details summary").TextContent);
            Assert.Equal(answer, cut.Find(".faq-list details p").TextContent);
            Assert.Empty(cut.FindAll(".faq-list img"));
            Assert.Empty(cut.FindAll(".faq-list script"));
            Assert.Contains("&lt;img", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("&lt;script", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task TokenStore_ClearAsyncClearsMemoryAndAllLocalStorageKeys()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.SetupVoid("localStorage.setItem", _ => true);
        ctx.JSInterop.SetupVoid("localStorage.removeItem", "anxietywatch_token");
        ctx.JSInterop.SetupVoid("localStorage.removeItem", "anxietywatch_expires");
        ctx.JSInterop.SetupVoid("localStorage.removeItem", "anxietywatch_user");
        var store = new TokenStore(ctx.Services.GetRequiredService<IJSRuntime>());
        await store.StoreAsync(new AuthResponse
        {
            Token = "token-before-clear",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            User = new UserDto { Id = "user-1", FullName = "Test User", Email = "user@example.test", PlanId = "free" }
        });

        await store.ClearAsync();

        Assert.Null(store.GetAccessToken());
        Assert.Null(store.GetExpiresAt());
        Assert.Null(store.GetUser());
        var removals = ctx.JSInterop.Invocations.Where(call => call.Identifier == "localStorage.removeItem").ToArray();
        Assert.Collection(
            removals,
            call => Assert.Equal("anxietywatch_token", call.Arguments[0]),
            call => Assert.Equal("anxietywatch_expires", call.Arguments[0]),
            call => Assert.Equal("anxietywatch_user", call.Arguments[0]));
    }

    [Fact]
    public async Task TokenStore_ClearAsyncStillAttemptsRemainingKeysWhenOneRemovalFails()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.SetupVoid("localStorage.removeItem", "anxietywatch_token")
            .SetException(new JSException("storage failure"));
        ctx.JSInterop.SetupVoid("localStorage.removeItem", "anxietywatch_expires");
        ctx.JSInterop.SetupVoid("localStorage.removeItem", "anxietywatch_user");
        var store = new TokenStore(ctx.Services.GetRequiredService<IJSRuntime>());

        await store.ClearAsync();

        var removals = ctx.JSInterop.Invocations.Where(call => call.Identifier == "localStorage.removeItem").ToArray();
        Assert.Equal(3, removals.Length);
        Assert.Contains(removals, call => Equals(call.Arguments[0], "anxietywatch_expires"));
        Assert.Contains(removals, call => Equals(call.Arguments[0], "anxietywatch_user"));
    }
}
