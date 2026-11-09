using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDayAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// ── 1. Database ───────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ── 2. Session ────────────────────────────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name        = "RaceDay.Session";
    options.IdleTimeout        = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly    = true;
    options.Cookie.IsEssential = true;
});

// ── 2a. Make Forbid() return a real 403 ──────────────────────
// Controllers call Forbid() for wrong-role requests. With no authentication
// scheme registered that call throws (HTTP 500), so register a minimal scheme
// whose only job is to answer "access denied" with 403 instead of redirecting.
builder.Services.AddAuthentication("RaceDay")
    .AddCookie("RaceDay", options =>
    {
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    });

// ── 2b. Azure Blob Storage (Part 3) ──────────────────────────
// The app still starts without a connection string; only the upload
// endpoints respond with 503 until AzureBlob:ConnectionString is set.
builder.Services.AddSingleton<RaceDayAPI.Services.IBlobStorageService>(sp =>
    new RaceDayAPI.Services.BlobStorageService(sp.GetRequiredService<IConfiguration>()));

// ── 3. Controllers ────────────────────────────────────────────
// AddNewtonsoftJson makes sure Swagger renders request bodies
// as an editable JSON text box, not individual query fields.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Keep property names as written in the DTOs (PascalCase)
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// ── 4. Swagger / OpenAPI ──────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "RaceDay API",
        Version     = "v1",
        Description =
            "RESTful API for the RaceDay event management platform.\n\n" +
            "**How to test:**\n" +
            "1. Register via POST /api/auth/register\n" +
            "2. Log in via POST /api/auth/login (session cookie is set automatically)\n" +
            "3. Use the other endpoints — the session cookie is sent with every request"
    });

    // This tells Swagger to read the <example> tags from DTOs
    // and render each endpoint's request as an editable JSON body
    c.UseInlineDefinitionsForEnums();
    c.DescribeAllParametersInCamelCase();
});

var app = builder.Build();

// ── 5. Swagger UI ─────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
    c.RoutePrefix = string.Empty;   // Swagger opens at the root URL
    c.DefaultModelsExpandDepth(1);  // Show request body schemas expanded
});

// ── 6. Middleware pipeline ────────────────────────────────────
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseSession();
app.MapControllers();

// ── 7. Auto-apply EF migrations on startup ───────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();

public partial class Program { }
