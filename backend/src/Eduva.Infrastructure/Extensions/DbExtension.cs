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
            await SeedDataAsync(services, context);
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
                    PhoneNumber = "0838683869",
                    TotalCredits = 1000,
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
                    PhoneNumber = "0838683868",
                    TotalCredits = 1000,
                };

                await userManager.CreateAsync(quyAdmin, "Admin11@");
                await userManager.AddToRoleAsync(quyAdmin, nameof(Role.SchoolAdmin));

                var sangAdmin = new ApplicationUser
                {
                    Id = new Guid("3a3a3a3a-3a3a-3a3a-3a3a-3a3a3a3a3a3a"),
                    Email = "sangtnqe170193@fpt.edu.vn",
                    UserName = "qe170193",
                    EmailConfirmed = true,
                    FullName = "Sang Tran Ngoc",
                    PhoneNumber = "0838683866",
                    TotalCredits = 1000,
                };

                await userManager.CreateAsync(sangAdmin, "Admin11@");
                await userManager.AddToRoleAsync(sangAdmin, nameof(Role.ContentModerator));

                var huyAdmin2 = new ApplicationUser
                {
                    Id = new Guid("4a4a4a4a-4a4a-4a4a-4a4a-4a4a4a4a4a4a"),
                    Email = "huydtqe170135@fpt.edu.vn",
                    UserName = "qe170135",
                    EmailConfirmed = true,
                    FullName = "Huy Dinh Trong",
                    PhoneNumber = "0838683865",
                    TotalCredits = 1000,
                };

                await userManager.CreateAsync(huyAdmin2, "Admin11@");
                await userManager.AddToRoleAsync(huyAdmin2, nameof(Role.Teacher));

                var dungAdmin = new ApplicationUser
                {
                    Id = new Guid("5a5a5a5a-5a5a-5a5a-5a5a-5a5a5a5a5a5a"),
                    Email = "dungnnqe170175@fpt.edu.vn",
                    UserName = "qe170175",
                    EmailConfirmed = true,
                    FullName = "Dung Nguyen Ngoc",
                    PhoneNumber = "0838683864",
                    TotalCredits = 1000,
                };

                await userManager.CreateAsync(dungAdmin, "Admin11@");
                await userManager.AddToRoleAsync(dungAdmin, nameof(Role.Student));
            }
        }

        private static async Task SeedDataAsync(IServiceProvider services, AppDbContext context)
        {
            if (!await context.Schools.AnyAsync())
            {
                var school = new School
                {
                    Id = 1,
                    Name = "FPT University",
                    Address = "FPT University Quy Nhon AI Campus",
                    ContactEmail = "fptuniversity@fpt.edu.vn",
                    ContactPhone = "0838683867",
                    WebsiteUrl = "https://fpt.edu.vn",
                };
                context.Schools.Add(school);
                await context.SaveChangesAsync();
            }
            if (!await context.SubscriptionPlans.AnyAsync())
            {
                var plan = new SubscriptionPlan
                {
                    Id = 1,
                    Name = "Basic Plan",
                    Description = "Basic plan for small schools with limited features.",
                    MaxUsers = 100,
                    StorageLimitGB = 10,
                    PriceMonthly = 100000m, // 100,000 VND
                    PricePerYear = 1100000m, // 1,000,000 VND
                };
                context.SubscriptionPlans.Add(plan);
                await context.SaveChangesAsync();
            }

            if (!await context.AICreditPacks.AnyAsync())
            {
                var creditPack = new AICreditPack
                {
                    Id = 1,
                    Name = "Basic AI Credit Pack",
                    Credits = 1000,
                    Price = 50000m, // 50,000 VND
                    BonusCredits = 100,
                };
                context.AICreditPacks.Add(creditPack);
                await context.SaveChangesAsync();
            }

            if (!await context.PaymentTransactions.AnyAsync())
            {
                var transactions = new List<PaymentTransaction>
                {
                    new PaymentTransaction
                    {
                        Id = new Guid("1a1a1a1a-1a1a-1a1a-1a1a-1a1a1a1a1a1a"),
                        UserId = new Guid("1a1a1a1a-1a1a-1a1a-1a1a-1a1a1a1a1a1a"),
                        Amount = 100000m,
                        PaymentMethod = PaymentMethod.PayOS,
                        PaymentStatus = PaymentStatus.Pending,
                        CreatedAt = DateTimeOffset.UtcNow,
                        PaymentItemId = 1, // Subscription Plan ID
                        RelatedId = null,
                    },
                    new PaymentTransaction
                    {
                        Id = new Guid("2b2b2b2b-2b2b-2b2b-2b2b-2b2b2b2b2b2bb"),
                        UserId = new Guid("1a1a1a1a-1a1a-1a1a-1a1a-1a1a1a1a1a1a"),
                        Amount = 50000m,
                        PaymentMethod = PaymentMethod.PayOS,
                        PaymentStatus = PaymentStatus.Pending,
                        CreatedAt = DateTimeOffset.UtcNow,
                        PaymentItemId = 1, // AI Credit Pack ID
                        RelatedId = null,
                    }
                };

                context.PaymentTransactions.AddRange(transactions);
                await context.SaveChangesAsync();
            }

            if (!await context.UserCreditTransactions.AnyAsync())
            {
                var transaction = new UserCreditTransaction
                {
                    Id = new Guid("3a1a1a1a-1a1a-1a1a-1a1a-1a1a1a1a1a1a"),
                    UserId = new Guid("1a1a1a1a-1a1a-1a1a-1a1a-1a1a1a1a1a1a"),
                    PaymentTransactionId = new Guid("2b2b2b2b-2b2b-2b2b-2b2b-2b2b2b2b2b2bb"),
                    AICreditPackId = 1,
                    Credits = 1000,
                };
                context.UserCreditTransactions.Add(transaction);
                await context.SaveChangesAsync();
            }

            if (!await context.SchoolSubscriptions.AnyAsync())
            {
                var subscription = new SchoolSubscription
                {
                    Id = new Guid("4a1a1a1a-4a1a-4a1a-4a1a-4a1a1a1a1a1a"),
                    SchoolId = 1,
                    PlanId = 1,
                    StartDate = DateTimeOffset.UtcNow,
                    EndDate = DateTimeOffset.UtcNow.AddDays(30),
                    SubscriptionStatus = SubscriptionStatus.Active,
                    BillingCycle = BillingCycle.Monthly,
                    PaymentTransactionId = new Guid("1a1a1a1a-1a1a-1a1a-1a1a-1a1a1a1a1a1a"),
                };
                context.SchoolSubscriptions.Add(subscription);
                await context.SaveChangesAsync();
            }

            if (!await context.AIServicePricings.AnyAsync())
            {
                var aiServicePricing = new AIServicePricing
                {
                    Id = 1,
                    ServiceType = AIServiceType.GenAudio,
                    PricePerMinuteCredits = 10,
                };
                context.AIServicePricings.Add(aiServicePricing);
                await context.SaveChangesAsync();
            }

            if (!await context.Classes.AnyAsync())
            {
                var classroom = new Classroom
                {
                    Id = new Guid("1a1a1a1a-1a1a-1a1a-1a1a-1a1a1a1a1a1a"),
                    Name = "Toán 10A1",
                    SchoolId = 1,
                    TeacherId = new Guid("4a4a4a4a-4a4a-4a4a-4a4a-4a4a4a4a4a4a"), // Huy Dinh Trong
                    ClassCode = "TOAN10A1",
                };
                context.Classes.Add(classroom);
                await context.SaveChangesAsync();
            }

            if (!await context.StudentClasses.AnyAsync())
            {
                var studentClass = new StudentClass
                {
                    Id = new Guid("5a5a5a5a-5a5a-5a5a-5a5a-5a5a5a5a5a5a"),
                    StudentId = new Guid("5a5a5a5a-5a5a-5a5a-5a5a-5a5a5a5a5a5a"), // Dung Nguyen Ngoc
                    ClassId = new Guid("1a1a1a1a-1a1a-1a1a-1a1a-1a1a1a1a1a1a"), // Toán 10A1
                };
                context.StudentClasses.Add(studentClass);
                await context.SaveChangesAsync();
            }

            if (!await context.Folders.AnyAsync())
            {
                var folders = new List<Folder>
                {
                     new Folder
                     {
                         Id = new Guid("1a1a1a1a-1a1a-1a1a-1a1a-1a1a1a1a1a1a"),
                         Name = "Toán 10A1 - Bài giảng",
                         UserId = null,
                         ClassId = new Guid("1a1a1a1a-1a1a-1a1a-1a1a-1a1a1a1a1a1a"), // Toán 10A1
                         OwnerType = OwnerType.Class,
                     },
                     new Folder
                     {
                         Id = new Guid("2b2b2b2b-2b2b-2b2b-2b2b-2b2b2b2b2b2b"),
                         Name = "Toán 10A1 - Bài giảng Cá nhân",
                         UserId = new Guid("4a4a4a4a-4a4a-4a4a-4a4a-4a4a4a4a4a4a"),
                         ClassId = null,
                         OwnerType = OwnerType.Personal
                     },
                };
                context.Folders.AddRange(folders);
                await context.SaveChangesAsync();
            }

            if (!await context.LessonMaterials.AnyAsync())
            {
                var lessonMaterial = new LessonMaterial
                {
                    Id = new Guid("3c3c3c3c-3c3c-3c3c-3c3c-3c3c3c3c3c3c"),
                    Title = "Giới thiệu về Đại số",
                    Description = "Bài giảng về các khái niệm cơ bản trong Đại số.",
                    SchoolId = 1,
                    ContentType = ContentType.Video,
                    SourceUrl = "https://example.com/video.mp4",
                    CreatedBy = new Guid("4a4a4a4a-4a4a-4a4a-4a4a-4a4a4a4a4a4a"), // Huy Dinh Trong
                    FileSize = 10485760, // 10 MB
                    Duration = 0,
                    Visibility = LessonMaterialVisibility.Private,
                    LessonStatus = LessonMaterialStatus.Approved
                };
                context.LessonMaterials.Add(lessonMaterial);
                await context.SaveChangesAsync();
            }

            if (!await context.FolderLessonMaterials.AnyAsync())
            {
                var folderLessonMaterial = new FolderLessonMaterial
                {
                    Id = new Guid("4d4d4d4d-4d4d-4d4d-4d4d-4d4d4d4d4d4d"),
                    FolderId = new Guid("1a1a1a1a-1a1a-1a1a-1a1a-1a1a1a1a1a1a"), // Toán 10A1 - Bài giảng
                    LessonMaterialId = new Guid("3c3c3c3c-3c3c-3c3c-3c3c-3c3c3c3c3c3c"), // Giới thiệu về Đại số
                };
                context.FolderLessonMaterials.Add(folderLessonMaterial);
                await context.SaveChangesAsync();
            }
        }
    }
}