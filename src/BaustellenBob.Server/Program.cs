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
    options.UseNpgsql(connectionString),
    contextLifetime: ServiceLifetime.Transient);

// Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.LogoutPath = "/api/auth/logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

// Tenant + User providers (resolved from claims)
builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();
builder.Services.AddScoped<UserUiStateService>();

// Application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IPhotoService>(sp =>
    new PhotoService(
        sp.GetRequiredService<AppDbContext>(),
        sp.GetRequiredService<ITenantProvider>(),
        sp.GetRequiredService<ICurrentUserProvider>(),
        sp.GetRequiredService<ITierLimitService>()));
builder.Services.AddScoped<IWorkReportService, WorkReportService>();
builder.Services.AddScoped<IUserService>(sp =>
    new UserService(
        sp.GetRequiredService<AppDbContext>(),
        sp.GetRequiredService<ITenantProvider>(),
        sp.GetRequiredService<ITierLimitService>()));
builder.Services.AddScoped<ITierLimitService, TierLimitService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IMaterialService, MaterialService>();
builder.Services.AddScoped<IProjectAssignmentService, ProjectAssignmentService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IProjectReportService>(sp =>
    new ProjectReportService(sp.GetRequiredService<AppDbContext>()));
builder.Services.AddScoped<ITenantService>(sp =>
    new TenantService(
        sp.GetRequiredService<AppDbContext>(),
        sp.GetRequiredService<ITenantProvider>()));

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

// Tenant-secured image download from database storage
app.MapGet("/uploads/{tenantId}/{**filePath}", async (HttpContext ctx, AppDbContext db, IWebHostEnvironment env, string tenantId, string filePath) =>
{
    var userTenant = ctx.User.FindFirstValue("TenantId");
    if (userTenant is null || !string.Equals(userTenant, tenantId, StringComparison.OrdinalIgnoreCase))
        return Results.Forbid();

    var normalizedFilePath = filePath.Replace('\\', '/');
    var relativePath = $"{tenantId}/{normalizedFilePath}";

    if (!Guid.TryParse(tenantId, out var tenantGuid))
        return Results.Forbid();

    var photo = await db.Photos
        .AsNoTracking()
        .Where(p => p.FilePath == relativePath)
        .Select(p => new { p.FileData, p.FileContentType })
        .FirstOrDefaultAsync();
    if (photo?.FileData is { Length: > 0 })
        return Results.File(photo.FileData, photo.FileContentType ?? "image/jpeg");

    var userAvatar = await db.Users
        .AsNoTracking()
        .Where(u => u.TenantId == tenantGuid && u.ProfilePicturePath == relativePath)
        .Select(u => new { u.ProfilePictureData, u.ProfilePictureContentType })
        .FirstOrDefaultAsync();
    if (userAvatar?.ProfilePictureData is { Length: > 0 })
        return Results.File(userAvatar.ProfilePictureData, userAvatar.ProfilePictureContentType ?? "image/jpeg");

    var tenantLogo = await db.Tenants
        .AsNoTracking()
        .Where(t => t.Id == tenantGuid && t.LogoPath == relativePath)
        .Select(t => new { t.LogoData, t.LogoContentType })
        .FirstOrDefaultAsync();
    if (tenantLogo?.LogoData is { Length: > 0 })
        return Results.File(tenantLogo.LogoData, tenantLogo.LogoContentType ?? "image/png");

    // Legacy fallback for already existing file-based uploads.
    var uploadsRoot = Path.Combine(env.ContentRootPath, "uploads");
    var fullPath = Path.GetFullPath(Path.Combine(uploadsRoot, tenantId, normalizedFilePath.Replace('/', Path.DirectorySeparatorChar)));
    if (fullPath.StartsWith(Path.GetFullPath(uploadsRoot), StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
    {
        var contentType = Path.GetExtension(fullPath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
        return Results.File(fullPath, contentType);
    }

    return Results.NotFound();
}).RequireAuthorization();

// PDF project report download
app.MapGet("/api/projects/{projectId:guid}/report", async (Guid projectId, IProjectReportService reportService, ITierLimitService tierLimitService) =>
{
    if (!await tierLimitService.CanExportPdfAsync())
        return Results.Problem(
            title: "PDF-Export nicht verfugbar",
            detail: "Ihr aktueller Tarif unterstutzt keinen PDF-Export.",
            statusCode: StatusCodes.Status403Forbidden);

    var pdf = await reportService.GenerateReportAsync(projectId);
    return Results.File(pdf, "application/pdf", $"ProjectReport-{projectId:N}.pdf");
}).RequireAuthorization();

// Invoice PDF download
app.MapGet("/api/invoices/{invoiceId:guid}/pdf", async (Guid invoiceId, IInvoiceService invoiceService, ITierLimitService tierLimitService) =>
{
    if (!await tierLimitService.CanExportPdfAsync())
        return Results.Problem(
            title: "PDF-Export nicht verfugbar",
            detail: "Ihr aktueller Tarif unterstutzt keinen PDF-Export.",
            statusCode: StatusCodes.Status403Forbidden);

    var pdf = await invoiceService.GenerateInvoicePdfAsync(invoiceId);
    return Results.File(pdf, "application/pdf", $"Rechnung-{invoiceId:N}.pdf");
}).RequireAuthorization();

// ---- REST API v1 (API key authentication) ----
var api = app.MapGroup("/api/v1").AddEndpointFilter(async (ctx, next) =>
{
    var httpCtx = ctx.HttpContext;
    var authHeader = httpCtx.Request.Headers["X-Api-Key"].FirstOrDefault();
    if (string.IsNullOrEmpty(authHeader))
        return Results.Unauthorized();

    var apiKeyService = httpCtx.RequestServices.GetRequiredService<IApiKeyService>();
    var tenantId = await apiKeyService.ValidateKeyAsync(authHeader);
    if (tenantId is null)
        return Results.Unauthorized();

    // Set tenant context for this request via claims
    var claims = new List<Claim>
    {
        new("TenantId", tenantId.Value.ToString()),
        new(ClaimTypes.Role, "Api")
    };
    httpCtx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "ApiKey"));
    return await next(ctx);
});

api.MapGet("/projects", async (IProjectService projectService) =>
    Results.Ok(await projectService.GetAllAsync()));

api.MapGet("/projects/{id:guid}", async (Guid id, IProjectService projectService) =>
{
    var project = await projectService.GetByIdAsync(id);
    return project is null ? Results.NotFound() : Results.Ok(project);
});

api.MapGet("/projects/{id:guid}/reports", async (Guid id, IWorkReportService reportService) =>
    Results.Ok(await reportService.GetByProjectAsync(id)));

api.MapGet("/projects/{id:guid}/materials", async (Guid id, IMaterialService materialService) =>
    Results.Ok(await materialService.GetByProjectAsync(id)));

api.MapPost("/projects/{id:guid}/reports", async (Guid id, BaustellenBob.Application.DTOs.WorkReportDto dto, IWorkReportService reportService) =>
{
    try
    {
        var created = await reportService.CreateAsync(id, dto);
        return Results.Created($"/api/v1/projects/{id}/reports", created);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
