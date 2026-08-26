using System.Text.Json;
using AnxietyWatch.Web.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// La API se consume siempre a través del mismo origen desde el que se cargó
// el frontend. El servidor web reenvía /api al backend HTTPS. Derivar la URL
// del host evita que una configuración de producción rompa localhost o un
// dominio de preview y mantiene las solicitudes dentro de la CSP same-origin.
var apiBaseAddress = new Uri(builder.HostEnvironment.BaseAddress, UriKind.Absolute);

// camelCase al serializar/deserializar, acorde al contrato JSON de la API.
JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
builder.Services.AddSingleton(jsonOptions);

// Almacén de sesión (token + usuario) y handler que inyecta Authorization: Bearer.
builder.Services.AddScoped<ITokenStore, TokenStore>();

builder.Services.AddScoped(sp => new HttpClient(
        new AuthHandler(sp.GetRequiredService<ITokenStore>(), apiBaseAddress)
        {
            InnerHandler = new HttpClientHandler()
        })
{
    BaseAddress = apiBaseAddress
});

// Estado de autenticación para que la UI reaccione a login/logout.
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<IAuthSessionManager, AuthSessionManager>();
builder.Services.AddScoped<ApiAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ApiAuthenticationStateProvider>());

// Servicios de aplicación.
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IEpisodeService, EpisodeService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ISupportService, SupportService>();

var host = builder.Build();
await host.RunAsync();
