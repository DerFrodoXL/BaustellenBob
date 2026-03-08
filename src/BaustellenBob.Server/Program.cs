using BaustellenBob.Application.Interfaces;
using BaustellenBob.Infrastructure.Data;
using BaustellenBob.Infrastructure.Services;
using BaustellenBob.Server.Components;
using BaustellenBob.Server.Services;
using BaustellenBob.Shared.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using System.Security.Claims;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Npgsql: treat all DateTime as UTC (MudDatePicker returns Kind=Local)
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=baustellenbob;Username=postgres;Password=postgres";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/api/auth/logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Tenant + User providers (resolved from claims)
builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();

// Application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IPhotoService>(sp =>
    new PhotoService(
        sp.GetRequiredService<AppDbContext>(),
        sp.GetRequiredService<ITenantProvider>(),
        sp.GetRequiredService<ICurrentUserProvider>(),
        sp.GetRequiredService<ITierLimitService>(),
        Path.Combine(builder.Environment.ContentRootPath, "uploads")));
builder.Services.AddScoped<IWorkReportService, WorkReportService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITierLimitService, TierLimitService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IProjectReportService>(sp =>
    new ProjectReportService(
        sp.GetRequiredService<AppDbContext>(),
        Path.Combine(builder.Environment.ContentRootPath, "uploads")));

// QuestPDF Community license
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// MudBlazor
builder.Services.AddMudServices();

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Auto-migrate on startup in all environments (including production).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Ensure uploads directory exists
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Login endpoint (minimal API — Blazor SSR posts here)
app.MapPost("/api/auth/login", async (HttpContext ctx, IAuthService authService) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();

    var result = await authService.LoginAsync(email, password);
    if (result is null)
    {
        ctx.Response.Redirect("/login?error=1");
        return;
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, result.UserId.ToString()),
        new(ClaimTypes.Name, result.UserName),
        new(ClaimTypes.Email, result.Email),
        new(ClaimTypes.Role, result.Role),
        new("TenantId", result.TenantId.ToString()),
        new("TenantName", result.TenantName)
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    ctx.Response.Redirect("/");
});

// Registration endpoint
app.MapPost("/api/auth/register", async (HttpContext ctx, IRegistrationService registrationService, IAuthService authService) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var companyName = form["companyName"].ToString().Trim();
    var adminName = form["adminName"].ToString().Trim();
    var email = form["email"].ToString().Trim();
    var password = form["password"].ToString();

    if (string.IsNullOrEmpty(companyName) || string.IsNullOrEmpty(adminName)
        || string.IsNullOrEmpty(email) || password.Length < 6)
    {
        ctx.Response.Redirect("/register?error=" + Uri.EscapeDataString("Bitte alle Felder korrekt ausfüllen."));
        return;
    }

    var result = await registrationService.RegisterAsync(companyName, adminName, email, password);
    if (!result.Success)
    {
        ctx.Response.Redirect("/register?error=" + Uri.EscapeDataString(result.Error!));
        return;
    }

    // Auto-login after registration
    var loginResult = await authService.LoginAsync(email, password);
    if (loginResult is null)
    {
        ctx.Response.Redirect("/login");
        return;
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, loginResult.UserId.ToString()),
        new(ClaimTypes.Name, loginResult.UserName),
        new(ClaimTypes.Email, loginResult.Email),
        new(ClaimTypes.Role, loginResult.Role),
        new("TenantId", loginResult.TenantId.ToString()),
        new("TenantName", loginResult.TenantName)
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    ctx.Response.Redirect("/");
});

// Logout endpoint
app.MapPost("/api/auth/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    ctx.Response.Redirect("/login");
});

// Tenant-secured file download
app.MapGet("/uploads/{tenantId}/{**filePath}", (HttpContext ctx, string tenantId, string filePath) =>
{
    var userTenant = ctx.User.FindFirstValue("TenantId");
    if (userTenant is null || !string.Equals(userTenant, tenantId, StringComparison.OrdinalIgnoreCase))
        return Results.Forbid();

    var fullPath = Path.Combine(uploadsPath, tenantId, filePath);
    var normalizedFull = Path.GetFullPath(fullPath);
    if (!normalizedFull.StartsWith(Path.GetFullPath(uploadsPath), StringComparison.OrdinalIgnoreCase))
        return Results.Forbid();

    if (!File.Exists(normalizedFull))
        return Results.NotFound();

    var contentType = Path.GetExtension(normalizedFull).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };
    return Results.File(normalizedFull, contentType);
}).RequireAuthorization();

// PDF project report download
app.MapGet("/api/projects/{projectId:guid}/report", async (Guid projectId, IProjectReportService reportService, ITierLimitService tierLimitService) =>
{
    if (!await tierLimitService.CanExportPdfAsync())
        return Results.Forbid();
    var pdf = await reportService.GenerateReportAsync(projectId);
    return Results.File(pdf, "application/pdf", $"ProjectReport-{projectId:N}.pdf");
}).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
