using AnxietyWatch.Web.Components;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient("AnxietyWatchApi", client =>
{
    client.BaseAddress = new Uri("https://api.mangoon.xyz/");
});
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    var allowedHosts = (builder.Configuration["Security:AllowedHosts"] ?? string.Empty)
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    app.Use(async (context, next) =>
    {
        if (context.Request.Path != "/healthz" &&
            !allowedHosts.Contains(context.Request.Host.Host, StringComparer.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        await next();
    });
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    context.Items["CspNonce"] = nonce;
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;
        headers["Content-Security-Policy"] = $"default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'none'; form-action 'self'; script-src 'self' 'nonce-{nonce}' 'wasm-unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' data:; connect-src 'self'; manifest-src 'self'; worker-src 'self' blob:";
        headers["X-Content-Type-Options"] = "nosniff";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
        headers["X-Frame-Options"] = "DENY";
        if (!app.Environment.IsDevelopment())
        {
            headers["Strict-Transport-Security"] = "max-age=31536000";
        }
        return Task.CompletedTask;
    });

    await next();
});

app.UseAntiforgery();

app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }));
app.MapMethods("/api/{**path}", ["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"], async (
    HttpContext context,
    IHttpClientFactory httpClientFactory,
    CancellationToken cancellationToken) =>
{
    var request = context.Request;
    var destination = new Uri($"https://api.mangoon.xyz{request.PathBase}{request.Path}{request.QueryString}");
    using var upstreamRequest = new HttpRequestMessage(new HttpMethod(request.Method), destination);

    foreach (var header in request.Headers)
    {
        if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) &&
            !header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
        {
            upstreamRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }

    if (request.ContentLength is > 0 || request.Headers.ContainsKey("Transfer-Encoding"))
    {
        upstreamRequest.Content = new StreamContent(request.Body);
        foreach (var header in request.Headers.Where(header => header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase)))
        {
            upstreamRequest.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }

    using var upstreamResponse = await httpClientFactory.CreateClient("AnxietyWatchApi")
        .SendAsync(upstreamRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    context.Response.StatusCode = (int)upstreamResponse.StatusCode;

    foreach (var header in upstreamResponse.Headers.Concat(upstreamResponse.Content.Headers))
    {
        if (!header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) &&
            !header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }
    }

    await upstreamResponse.Content.CopyToAsync(context.Response.Body, cancellationToken);
});
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(AnxietyWatch.Web.Client._Imports).Assembly);

app.Run();
