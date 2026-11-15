using MFCMP.Api.Domain;
using Microsoft.AspNetCore.Identity;

namespace MFCMP.Api.Services;

public sealed class BootstrapAdminSeeder(
    IServiceProvider services,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<BootstrapAdminSeeder> logger)
{
    public async Task SeedAsync()
    {
        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<PortalUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        const string role = "Administrator";
        if (!await roles.RoleExistsAsync(role))
        {
            var roleResult = await roles.CreateAsync(new IdentityRole(role));
            if (!roleResult.Succeeded) throw new InvalidOperationException("Cannot create Administrator role.");
        }

        const string username = "admin";
        var existing = await users.FindByNameAsync(username);
        if (existing is not null)
        {
            if (!await users.IsInRoleAsync(existing, role)) await users.AddToRoleAsync(existing, role);
            return; // Never reset an existing password.
        }

        var password = configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(password))
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException("Set BootstrapAdmin:Password using a secure secret provider before production bootstrap.");
            password = "admin";
            logger.LogWarning("Created development bootstrap admin/admin. Change it on first sign-in.");
        }

        var admin = new PortalUser { UserName = username, MustChangePassword = true };
        var created = await users.CreateAsync(admin, password);
        if (!created.Succeeded)
            throw new InvalidOperationException("Bootstrap admin creation failed: " + string.Join(", ", created.Errors.Select(e => e.Code)));
        await users.AddToRoleAsync(admin, role);
    }
}
