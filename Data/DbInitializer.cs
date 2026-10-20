using Microsoft.AspNetCore.Identity;
using MalapadCogonEConcern.Models.Entities;

namespace MalapadCogonEConcern.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.EnsureCreatedAsync();

        // 1. Seed Roles
        string[] roles = ["Admin", "Staff"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Seed Default Admin
        var adminEmail = "admin@malapadcogon.gov.ph";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Barangay Administrator",
                Position = "Barangay Secretary",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@Malapad2026!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // 3. Seed Default Staff
        var staffEmail = "staff@malapadcogon.gov.ph";
        var staffUser = await userManager.FindByEmailAsync(staffEmail);
        if (staffUser == null)
        {
            staffUser = new ApplicationUser
            {
                UserName = staffEmail,
                Email = staffEmail,
                FullName = "Barangay Desk Officer",
                Position = "Barangay Tanod Desk",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(staffUser, "Staff@Malapad2026!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(staffUser, "Staff");
            }
        }

        // 4. Seed Default Categories
        if (!context.Categories.Any())
        {
            var categories = new List<Category>
            {
                new() { Name = "Road and Infrastructure", Description = "Damaged roads, bridges, pathways, broken streetlights, and farm-to-market road issues.", IconClass = "bi-cone-striped", DisplayOrder = 1 },
                new() { Name = "Drainage and Flooding", Description = "Clogged drainage, flood-prone areas, culvert issues, and waterway obstructions.", IconClass = "bi-water", DisplayOrder = 2 },
                new() { Name = "Waste Management and Cleanliness", Description = "Uncollected garbage, illegal dumping, and dirty public spaces.", IconClass = "bi-trash3", DisplayOrder = 3 },
                new() { Name = "Agriculture and Livelihood", Description = "Communal irrigation canals, farm access ways, and agricultural facility concerns.", IconClass = "bi-flower1", DisplayOrder = 4 },
                new() { Name = "Public Facilities and Services", Description = "Barangay Hall, multi-purpose covered gym, and public health station maintenance.", IconClass = "bi-building", DisplayOrder = 5 },
                new() { Name = "Disaster and Hazard Reports", Description = "Fallen trees, dangerous electrical lines, flood hazards, and landslide threats.", IconClass = "bi-exclamation-triangle", DisplayOrder = 6 },
                new() { Name = "Other Community Concerns", Description = "General community concerns not classified above.", IconClass = "bi-chat-left-dots", DisplayOrder = 7 }
            };

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        // 5. Seed Default Sitios / Puroks
        if (!context.Sitios.Any())
        {
            var sitios = new List<Sitio>
            {
                new() { Name = "Purok 1", Description = "Barangay Malapad Cogon - Purok 1", DisplayOrder = 1 },
                new() { Name = "Purok 2", Description = "Barangay Malapad Cogon - Purok 2", DisplayOrder = 2 },
                new() { Name = "Purok 3", Description = "Barangay Malapad Cogon - Purok 3", DisplayOrder = 3 },
                new() { Name = "Purok 4", Description = "Barangay Malapad Cogon - Purok 4", DisplayOrder = 4 },
                new() { Name = "Purok 5", Description = "Barangay Malapad Cogon - Purok 5", DisplayOrder = 5 },
                new() { Name = "Purok 6", Description = "Barangay Malapad Cogon - Purok 6", DisplayOrder = 6 },
                new() { Name = "Sitio Ilaya", Description = "Barangay Malapad Cogon - Upper Zone", DisplayOrder = 7 },
                new() { Name = "Sitio Ilawod", Description = "Barangay Malapad Cogon - Lower Zone", DisplayOrder = 8 }
            };

            await context.Sitios.AddRangeAsync(sitios);
            await context.SaveChangesAsync();
        }
    }
}
