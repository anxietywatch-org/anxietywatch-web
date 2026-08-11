using System.Text.Json;
using AnxietyWatch.Web.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Url base de la API backend (override con appsettings.json o Api:BaseUrl).
var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException(
        "Configura 'Api:BaseUrl' en wwwroot/appsettings.json para consumir la API.");

// camelCase al serializar/deserializar, acorde al contrato JSON de la API.
JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
builder.Services.AddSingleton(jsonOptions);

// Almacén de sesión (token + usuario) y handler que inyecta Authorization: Bearer.
builder.Services.AddScoped<ITokenStore, TokenStore>();

builder.Services.AddScoped(sp => new HttpClient(
        new AuthHandler(sp.GetRequiredService<ITokenStore>())
        {
            InnerHandler = new HttpClientHandler()
        })
    {
        BaseAddress = new Uri(apiBaseUrl)
    });

// Estado de autenticación para que la UI reaccione a login/logout.
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<IAuthSessionManager, AuthSessionManager>();
builder.Services.AddScoped<ApiAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ApiAuthenticationStateProvider>());

// Servicios de aplicación.
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IEpisodeService, EpisodeService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IProfileService, ProfileService>();

var host = builder.Build();
await host.RunAsync();
