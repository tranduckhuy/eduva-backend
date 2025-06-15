using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Persistence.DbContext;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Eduva.Infrastructure.Extensions
{
    public static class DbExtension
    {
        public static async Task MigrationsDatabaseAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<AppDbContext>();

            await context.Database.MigrateAsync();

            await SeedAuthDataAsync(services, context);
        }

        private static async Task SeedAuthDataAsync(IServiceProvider services, AppDbContext context)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            if (!await context.Roles.AnyAsync())
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(nameof(Role.SystemAdmin)));
                await roleManager.CreateAsync(new IdentityRole<Guid>(nameof(Role.SchoolAdmin)));
                await roleManager.CreateAsync(new IdentityRole<Guid>(nameof(Role.ContentModerator)));
                await roleManager.CreateAsync(new IdentityRole<Guid>(nameof(Role.Teacher)));
                await roleManager.CreateAsync(new IdentityRole<Guid>(nameof(Role.Student)));
            }

            if (!await context.Users.AnyAsync())
            {
                var huyAdmin = new ApplicationUser
                {
                    Id = new Guid("1a1a1a1a-1a1a-1a1a-1a1a-1a1a1a1a1a1a"),
                    Email = "huytdqe170235@fpt.edu.vn",
                    UserName = "qe170235",
                    EmailConfirmed = true,
                    FullName = "Huy Tran Duc",
                    PhoneNumber = "0838683869"
                };

                await userManager.CreateAsync(huyAdmin, "Admin11@");
                await userManager.AddToRoleAsync(huyAdmin, nameof(Role.SystemAdmin));

                var quyAdmin = new ApplicationUser
                {
                    Id = new Guid("2a2a2a2a-2a2a-2a2a-2a2a-2a2a2a2a2a2a"),
                    Email = "quynxqe170239@fpt.edu.vn",
                    UserName = "qe170239",
                    EmailConfirmed = true,
                    FullName = "Quy Nguyen Xuan",
                    PhoneNumber = "0838683868"
                };

                await userManager.CreateAsync(quyAdmin, "Admin11@");
                await userManager.AddToRoleAsync(quyAdmin, nameof(Role.SystemAdmin));

                var sangAdmin = new ApplicationUser
                {
                    Id = new Guid("3a3a3a3a-3a3a-3a3a-3a3a-3a3a3a3a3a3a"),
                    Email = "sangtnqe170193@fpt.edu.vn",
                    UserName = "qe170193",
                    EmailConfirmed = true,
                    FullName = "Sang Tran Ngoc",
                    PhoneNumber = "0838683866"
                };

                await userManager.CreateAsync(sangAdmin, "Admin11@");
                await userManager.AddToRoleAsync(sangAdmin, nameof(Role.SystemAdmin));

                var huyAdmin2 = new ApplicationUser
                {
                    Id = new Guid("4a4a4a4a-4a4a-4a4a-4a4a-4a4a4a4a4a4a"),
                    Email = "huydtqe170135@fpt.edu.vn",
                    UserName = "qe170135",
                    EmailConfirmed = true,
                    FullName = "Huy Dinh Trong",
                    PhoneNumber = "0838683865"
                };

                await userManager.CreateAsync(huyAdmin2, "Admin11@");
                await userManager.AddToRoleAsync(huyAdmin2, nameof(Role.SystemAdmin));

                var dungAdmin = new ApplicationUser
                {
                    Id = new Guid("5a5a5a5a-5a5a-5a5a-5a5a-5a5a5a5a5a5a"),
                    Email = "dungnnqe170175@fpt.edu.vn",
                    UserName = "qe170175",
                    EmailConfirmed = true,
                    FullName = "Dung Nguyen Ngoc",
                    PhoneNumber = "0838683864"
                };

                await userManager.CreateAsync(dungAdmin, "Admin11@");
                await userManager.AddToRoleAsync(dungAdmin, nameof(Role.SystemAdmin));
            }
        }
    }
}