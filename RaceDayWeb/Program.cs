using RaceDayWeb.Services;

var builder = WebApplication.CreateBuilder(args);

// ── 1. MVC ────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── 2. Session ────────────────────────────────────────────────
// Holds the logged-in user's id, role and name, plus the API's session cookie.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "RaceDayWeb.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

// ── 3. Typed HttpClient for the Part 2 API ────────────────────
// UseCookies = false: we forward the API's session cookie ourselves, per user,
// so one shared HttpClient never mixes up two different users' sessions.
string apiBase = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:59175/";
if (!apiBase.EndsWith('/')) apiBase += "/";

builder.Services.AddHttpClient<IRaceDayApiClient, RaceDayApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBase);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    UseCookies = false,

    // Accept the API's self-signed dev certificate, but only for local addresses.
    ServerCertificateCustomValidationCallback = (request, cert, chain, sslErrors) =>
    {
        if (sslErrors == System.Net.Security.SslPolicyErrors.None)
            return true;

        var host = request.RequestUri?.Host;
        return host is "localhost" or "127.0.0.1" or "::1";
    }
});

var app = builder.Build();

// ── 4. Pipeline ───────────────────────────────────────────────
// Friendly error pages: users never see a stack trace or raw error text.
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/StatusPage", "?code={0}");

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();