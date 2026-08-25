using AnxietyWatch.Web.Client.Models.Api;
using AnxietyWatch.Web.Client.Models.Auth;
using AnxietyWatch.Web.Client.Services;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using AuthPage = AnxietyWatch.Web.Client.Pages.Auth.Auth;
using PlanPage = AnxietyWatch.Web.Client.Pages.Dashboard.Plan;
using TokensPage = AnxietyWatch.Web.Client.Pages.Dashboard.Tokens;

namespace AnxietyWatch.Web.Client.Tests;

public sealed class ComponentFlowTests
{
    [Fact]
    public void Auth_CompletePaidRegistration_ShowsCheckoutAndNavigatesToVerification()
    {
        using var ctx = new BunitContext();
        var auth = new Mock<IAuthService>();
        var billing = new Mock<IBillingService>();
        var plans = new Mock<IPlanService>();
        var session = new Mock<IAuthSessionManager>();
        var catalog = CreatePlans();

        plans.Setup(service => service.GetPlansAsync(It.IsAny<CancellationToken>())).ReturnsAsync(catalog);
        auth.Setup(service => service.CheckEmailAvailabilityAsync("new@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailAvailabilityResponse { Available = true });
        auth.Setup(service => service.RegisterAsync(It.IsAny<RegisterRequest>())).ReturnsAsync(CreateSession("free"));
        billing.Setup(service => service.SimulatePaymentAsync(
                It.Is<SimulatePaymentRequest>(request => request.PlanId == "professional" && request.BillingCycle == "monthly"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SimulatedPaymentDto { PlanId = "professional", BillingCycle = "monthly", Status = "succeeded" });
        session.Setup(service => service.RefreshSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSession("professional"));

        ctx.Services.AddSingleton(auth.Object);
        ctx.Services.AddSingleton(billing.Object);
        ctx.Services.AddSingleton(plans.Object);
        ctx.Services.AddSingleton(session.Object);
        var navigation = ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/register");

        var cut = ctx.Render<AuthPage>();
        cut.Find("#register-name").Change("Test User");
        cut.Find("#register-email").Change("new@example.test");
        cut.Find("#register-password").Change("Password123!");
        cut.Find("form.auth-form").Submit();

        cut.WaitForAssertion(() => Assert.Contains("Elige tu plan", cut.Markup, StringComparison.Ordinal));
        cut.Find("button.auth-plan-card.plan-tone--professional").Click();
        Assert.NotNull(cut.Find(".simulated-checkout"));
        Assert.Contains("Pago seguro simulado", cut.Markup, StringComparison.Ordinal);
        cut.Find("#demo-card-number").Input("4242 4242 4242 4242");
        cut.Find("#demo-expiry").Input("12/99");
        cut.Find("#demo-cvv").Input("123");
        cut.Find("form.auth-form").Submit();

        cut.WaitForAssertion(() =>
            Assert.Equal("/verify-email/pending", new Uri(navigation.Uri).AbsolutePath));
        auth.Verify(service => service.RegisterAsync(It.Is<RegisterRequest>(request =>
            request.FullName == "Test User" && request.Email == "new@example.test" && request.PlanId == "free")), Times.Once);
        billing.VerifyAll();
        session.Verify(service => service.RefreshSessionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Tokens_CreateThenDeletePendingToken_UpdatesRenderedListAndQuota()
    {
        using var ctx = new BunitContext();
        var tokenService = new Mock<ITokenService>();
        var session = CreateAuthenticatedSessionMock("free");
        var tokenId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var createdToken = new LinkTokenDto
        {
            Id = tokenId.ToString(),
            Code = "TEST-CODE-1234",
            Role = "self",
            Status = "pending",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        };

        tokenService.Setup(service => service.GetTokensAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<LinkTokenDto>());
        tokenService.SetupSequence(service => service.GetQuotaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenQuotaDto { Limit = 3, Used = 0, Remaining = 3 })
            .ReturnsAsync(new TokenQuotaDto { Limit = 3, Used = 1, Remaining = 2 })
            .ReturnsAsync(new TokenQuotaDto { Limit = 3, Used = 0, Remaining = 3 });
        tokenService.Setup(service => service.CreateTokenAsync(
                It.Is<CreateTokenRequest>(request => request.Role == "self"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdToken);
        tokenService.Setup(service => service.DeleteTokenAsync(tokenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessResponse { Success = true });

        ctx.Services.AddSingleton(tokenService.Object);
        ctx.Services.AddSingleton(session.Object);
        var cut = ctx.Render<TokensPage>();
        cut.WaitForAssertion(() => Assert.Contains("Aún no hay tokens", cut.Markup, StringComparison.Ordinal));

        cut.Find("button.new-token-action").Click();
        Assert.Contains("Nuevo token", cut.Find(".token-modal").TextContent, StringComparison.Ordinal);
        cut.Find("button.generate-token-button").Click();
        cut.WaitForAssertion(() => Assert.Equal("TEST-CODE-1234", cut.Find(".generated-token code").TextContent));
        cut.Find(".token-modal-button--cancel").Click();
        Assert.Single(cut.FindAll("tbody tr"));
        Assert.Contains("••••-1234", cut.Find("tbody").TextContent, StringComparison.Ordinal);

        cut.Find("button[aria-label='Eliminar token']").Click();
        Assert.Contains("¿Eliminar este token?", cut.Find(".token-confirm-modal").TextContent, StringComparison.Ordinal);
        cut.Find(".token-confirm-modal .token-modal-button--danger").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll("tbody tr"));
            Assert.Contains("Token eliminado correctamente", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("0 de 3", cut.Markup, StringComparison.Ordinal);
        });
        tokenService.Verify(service => service.CreateTokenAsync(It.IsAny<CreateTokenRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        tokenService.Verify(service => service.DeleteTokenAsync(tokenId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Plan_DowngradeToFree_RefreshesSessionAndShowsSuccess()
    {
        using var ctx = new BunitContext();
        var plans = new Mock<IPlanService>();
        var billing = new Mock<IBillingService>();
        var session = new Mock<IAuthSessionManager>();
        var currentUser = CreateUser("professional");
        var catalog = CreatePlans();

        session.SetupGet(manager => manager.CurrentUser).Returns(() => currentUser);
        session.Setup(manager => manager.RefreshSessionAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken _) =>
            {
                currentUser = CreateUser("free");
                return Task.FromResult<AuthResponse?>(CreateSession("free"));
            });
        plans.Setup(service => service.GetPlansAsync(It.IsAny<CancellationToken>())).ReturnsAsync(catalog);
        billing.Setup(service => service.DowngradeToFreeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DowngradeToFreeResponse
            {
                PlanId = "free",
                PreviousPlanId = "professional",
                Changed = true,
                DowngradedAt = DateTimeOffset.UtcNow
            });

        ctx.Services.AddSingleton(plans.Object);
        ctx.Services.AddSingleton(billing.Object);
        ctx.Services.AddSingleton(session.Object);
        var cut = ctx.Render<PlanPage>();
        cut.WaitForAssertion(() => Assert.Contains("Plan activo · Profesional", cut.Markup, StringComparison.Ordinal));

        cut.Find(".option-card--free button.plan-action").Click();
        Assert.Contains("Vas a cambiar a Gratuito", cut.Find(".upgrade-modal").TextContent, StringComparison.Ordinal);
        cut.Find(".upgrade-button--confirm").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Tu plan se cambió a Gratuito", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Plan activo · Gratuito", cut.Markup, StringComparison.Ordinal);
        });
        billing.Verify(service => service.DowngradeToFreeAsync(It.IsAny<CancellationToken>()), Times.Once);
        session.Verify(service => service.RefreshSessionAsync(It.IsAny<CancellationToken>()), Times.Once);
        plans.Verify(service => service.GetPlansAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private static Mock<IAuthSessionManager> CreateAuthenticatedSessionMock(string planId)
    {
        var session = new Mock<IAuthSessionManager>();
        session.SetupGet(manager => manager.CurrentUser).Returns(CreateUser(planId));
        return session;
    }

    private static UserDto CreateUser(string planId) => new()
    {
        Id = "user-1",
        FullName = "Test User",
        Email = "user@example.test",
        PlanId = planId,
        EmailVerified = true
    };

    private static AuthResponse CreateSession(string planId) => new()
    {
        Token = "session-token",
        ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        User = CreateUser(planId)
    };

    private static IReadOnlyList<PlanDto> CreatePlans() =>
    [
        new PlanDto
        {
            Id = "free",
            Name = "Gratuito",
            PriceMonthly = 0,
            PriceYearly = 0,
            Features = ["Funciones básicas"],
            IdealFor = "Uso personal"
        },
        new PlanDto
        {
            Id = "professional",
            Name = "Profesional",
            PriceMonthly = 299,
            PriceYearly = 2990,
            Features = ["Funciones profesionales"],
            IdealFor = "Profesionales"
        }
    ];
}
