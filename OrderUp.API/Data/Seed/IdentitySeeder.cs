using Microsoft.AspNetCore.Identity;

namespace OrderUp.API.Data.Seed;

/// <summary>
/// Seeds Identity roles and admin user for development.
/// Reads admin credentials from configuration keys Admin:Email and Admin:Password.
/// </summary>
public static class IdentitySeeder
{
    public const string AdminRole = "Admin";

    public static async Task SeedAsync(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration)
    {
        // Create Admin role if it doesn't exist
        if (!await roleManager.RoleExistsAsync(AdminRole))
        {
            await roleManager.CreateAsync(new IdentityRole(AdminRole));
        }

        // Get admin credentials from configuration
        var adminEmail = configuration["Admin:Email"];
        var adminPassword = configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            // Skip admin user creation if credentials not configured
            return;
        }

        // Create admin user if it doesn't exist
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin is null)
        {
            var adminUser = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true // Skip email confirmation for seeded admin
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, AdminRole);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create admin user: {errors}");
            }
        }
        else
        {
            // Ensure existing admin has the Admin role
            if (!await userManager.IsInRoleAsync(existingAdmin, AdminRole))
            {
                await userManager.AddToRoleAsync(existingAdmin, AdminRole);
            }
        }
    }
}
