using MFCMP.Api.Data;
using MFCMP.Api.Domain;
using MFCMP.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection.");

builder.Services.AddDbContext<MonitoringDbContext>(o => o.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));
builder.Services.AddIdentity<PortalUser, IdentityRole>(o =>
{
    o.User.RequireUniqueEmail = false;
    // The temporary bootstrap password is weak by design. Access to monitoring endpoints
    // is blocked until the first-login password-change flow enforces a strong password.
    o.Password.RequiredLength = 5;
    o.Password.RequireDigit = false;
    o.Password.RequireLowercase = false;
    o.Password.RequireUppercase = false;
    o.Password.RequireNonAlphanumeric = false;
    o.Lockout.AllowedForNewUsers = true;
    o.Lockout.MaxFailedAccessAttempts = 5;
    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
}).AddEntityFrameworkStores<MonitoringDbContext>().AddDefaultTokenProviders();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<PortalUser>, PortalClaimsPrincipalFactory>();
builder.Services.ConfigureApplicationCookie(o =>
{
    o.Cookie.Name = "MFCMP.Auth";
    o.Cookie.Path = "/";
    o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.SlidingExpiration = true;
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    o.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});
builder.Services.AddAntiforgery(o =>
{
    o.HeaderName = "X-CSRF-TOKEN";
    o.Cookie.Name = "MFCMP.AntiForgery";
    o.Cookie.Path = "/";
    o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.AddControllers();
builder.Services.AddAuthorization(options =>
    options.AddPolicy("PortalPasswordChanged", policy =>
        policy.RequireAuthenticatedUser().RequireClaim("portal_password_changed", "true")));
builder.Services.AddHttpClient<IVaultStatusProbe, RestVaultStatusProbe>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(12);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("MFCMP-Monitor/1.0");
});
builder.Services.AddScoped<BootstrapAdminSeeder>();

var app = builder.Build();
if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<MonitoringDbContext>().Database.MigrateAsync();
}
if (app.Configuration.GetValue<bool>("BootstrapAdmin:CreateIfMissing"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<BootstrapAdminSeeder>().SeedAsync();
}
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/healthz", () => Results.Ok(new { status = "healthy", application = "MFCMP.Api", timestampUtc = DateTimeOffset.UtcNow })).AllowAnonymous();
app.Run();

public partial class Program { }
