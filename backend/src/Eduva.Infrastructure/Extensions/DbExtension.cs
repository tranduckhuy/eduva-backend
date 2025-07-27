using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Persistence.DbContext;
using Eduva.Shared.Constants;
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
                var roles = new List<IdentityRole<Guid>>
                {
                    new()
                    {
                        Id = new Guid("11111111-1111-1111-1111-111111111111"),
                        Name = nameof(Role.SystemAdmin),
                        NormalizedName = nameof(Role.SystemAdmin).ToUpper()
                    },
                    new()
                    {
                        Id = new Guid("22222222-2222-2222-2222-222222222222"),
                        Name = nameof(Role.SchoolAdmin),
                        NormalizedName = nameof(Role.SchoolAdmin).ToUpper()
                    },
                    new()
                    {
                        Id = new Guid("33333333-3333-3333-3333-333333333333"),
                        Name = nameof(Role.ContentModerator),
                        NormalizedName = nameof(Role.ContentModerator).ToUpper()
                    },
                    new()
                    {
                        Id = new Guid("44444444-4444-4444-4444-444444444444"),
                        Name = nameof(Role.Teacher),
                        NormalizedName = nameof(Role.Teacher).ToUpper()
                    },
                    new()
                    {
                        Id = new Guid("55555555-5555-5555-5555-555555555555"),
                        Name = nameof(Role.Student),
                        NormalizedName = nameof(Role.Student).ToUpper()
                    }
                };

                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role.Name!))
                    {
                        await roleManager.CreateAsync(role);
                    }
                }
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
                    LockoutEnabled = false,
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
                    LockoutEnabled = false
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
                    LockoutEnabled = false
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
                    TotalCredits = 40,
                    LockoutEnabled = false
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
                    TotalCredits = 0,
                    LockoutEnabled = false
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
                    Name = "Gói Cơ Bản",
                    Description = "Trường nhỏ (≤ 300 người).",
                    MaxUsers = 300,
                    StorageLimitGB = 100,
                    PriceMonthly = 300000m, // 300,000 VND
                    PricePerYear = 3420000m, // 3,420,000 VND
                };
                context.SubscriptionPlans.Add(plan);
                await context.SaveChangesAsync();
            }

            if (!await context.AICreditPacks.AnyAsync())
            {
                var creditPack = new AICreditPack
                {
                    Name = "Gói AI Dùng Thử",
                    Credits = 40,
                    Price = 10000m, // 10,000 VND
                    BonusCredits = 0,
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
                        UserId = new Guid("2a2a2a2a-2a2a-2a2a-2a2a-2a2a2a2a2a2a"),
                        Amount = 300000m,
                        PaymentPurpose = PaymentPurpose.SchoolSubscription,
                        PaymentMethod = PaymentMethod.PayOS,
                        PaymentStatus = PaymentStatus.Paid,
                        PaymentItemId = 1, // Subscription Plan ID
                        RelatedId = "4a1a1a1a-4a1a-4a1a-4a1a-4a1a1a1a1a1a",
                        TransactionCode = "1752256235",
                        BillingCycle = BillingCycle.Monthly

                    },
                    new PaymentTransaction
                    {
                        Id = new Guid("2b2b2b2b-2b2b-2b2b-2b2b-2b2b2b2b2b2b"),
                        UserId = new Guid("4a4a4a4a-4a4a-4a4a-4a4a-4a4a4a4a4a4a"),
                        Amount = 10000m,
                        PaymentPurpose = PaymentPurpose.CreditPackage,
                        PaymentMethod = PaymentMethod.PayOS,
                        PaymentStatus = PaymentStatus.Paid,
                        PaymentItemId = 1, // AI Credit Pack ID
                        RelatedId = "3a1a1a1a-1a1a-1a1a-1a1a-1a1a1a1a1a1a",
                        TransactionCode = "1752291769"
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
                    UserId = new Guid("4a4a4a4a-4a4a-4a4a-4a4a-4a4a4a4a4a4a"),
                    PaymentTransactionId = new Guid("2b2b2b2b-2b2b-2b2b-2b2b-2b2b2b2b2b2b"),
                    AICreditPackId = 1,
                    Credits = 40
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
                var aiServicePricings = new List<AIServicePricing>
                {
                    new AIServicePricing
                    {
                        ServiceType = AIServiceType.GenAudio,
                        PricePerMinuteCredits = 2,
                    },
                    new AIServicePricing
                    {
                        ServiceType = AIServiceType.GenVideo,
                        PricePerMinuteCredits = 4,
                    },
                };

                context.AIServicePricings.AddRange(aiServicePricings);
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
                    ClassCode = "QBTY3H2E",
                    BackgroundImageUrl = "https://egverimjijiaqduqcfur.supabase.co/storage/v1/object/public/classroom-images/dai_so.jpg",
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
                         UserId = new Guid("4a4a4a4a-4a4a-4a4a-4a4a-4a4a4a4a4a4a"),
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
                    SourceUrl = "https://eduva.blob.core.windows.net/eduva-storage/school1/snaptik.vn_41a5f.mp4",
                    CreatedByUserId = new Guid("4a4a4a4a-4a4a-4a4a-4a4a-4a4a4a4a4a4a"), // Huy Dinh Trong
                    FileSize = 2023759, // 1.93 MiB
                    Duration = 4,
                    Visibility = LessonMaterialVisibility.Private,
                    LessonStatus = LessonMaterialStatus.Approved
                };
                context.LessonMaterials.Add(lessonMaterial);
                await context.SaveChangesAsync();
            }

            if (!await context.LessonMaterialApprovals.AnyAsync())
            {
                var lessonMaterialApproval = new LessonMaterialApproval
                {
                    Id = new Guid("3d3d3d3d-3d3d-3d3d-3d3d-3d3d3d3d3d3d"),
                    LessonMaterialId = new Guid("3c3c3c3c-3c3c-3c3c-3c3c-3c3c3c3c3c3c"), // Giới thiệu về Đại số
                    ApproverId = new Guid("2a2a2a2a-2a2a-2a2a-2a2a-2a2a2a2a2a2a"), // Quy Nguyen Xuan
                    StatusChangeTo = LessonMaterialStatus.Approved,
                    Feedback = "Bài giảng đã được phê duyệt và sẵn sàng sử dụng.",
                };
                context.LessonMaterialApprovals.Add(lessonMaterialApproval);
                await context.SaveChangesAsync();
            }

            if (!await context.FolderLessonMaterials.AnyAsync())
            {
                var folderLessonMaterials = new List<FolderLessonMaterial>
                {
                    new FolderLessonMaterial
                    {
                        Id = new Guid("4d4d4d4d-4d4d-4d4d-4d4d-4d4d4d4d4d4d"),
                        FolderId = new Guid("1a1a1a1a-1a1a-1a1a-1a1a-1a1a1a1a1a1a"), // Toán 10A1 - Bài giảng
                        LessonMaterialId = new Guid("3c3c3c3c-3c3c-3c3c-3c3c-3c3c3c3c3c3c"), // Giới thiệu về Đại số
                    },
                    new FolderLessonMaterial
                    {
                        Id = new Guid("2b2b2b2b-2b2b-2b2b-2b2b-2b2b2b2b2b2b"),
                        FolderId = new Guid("2b2b2b2b-2b2b-2b2b-2b2b-2b2b2b2b2b2b"), // Toán 10A1 - Bài giảng Cá nhân
                        LessonMaterialId = new Guid("3c3c3c3c-3c3c-3c3c-3c3c-3c3c3c3c3c3c"), // Giới thiệu về Đại số
                    }
                };

                context.FolderLessonMaterials.AddRange(folderLessonMaterials);
                await context.SaveChangesAsync();
            }

            if (!await context.LessonMaterialQuestions.AnyAsync())
            {
                var lessonMaterialQuestion = new LessonMaterialQuestion
                {
                    Id = new Guid("1a1a1a1a-1a1a-1a1a-1a1a-1a1a1a1a1a1a"),
                    LessonMaterialId = new Guid("3c3c3c3c-3c3c-3c3c-3c3c-3c3c3c3c3c3c"), // Giới thiệu về Đại số
                    Title = "Câu hỏi về phương trình Đại số lớp 10",
                    Content = "Giúp em giải thích phương trình bậc nhất và bậc hai trong Đại số lớp 10.",
                    CreatedByUserId = new Guid("5a5a5a5a-5a5a-5a5a-5a5a-5a5a5a5a5a5a"), // Dung Nguyen Ngoc
                };
                context.LessonMaterialQuestions.Add(lessonMaterialQuestion);
                await context.SaveChangesAsync();
            }

            if (!await context.QuestionComments.AnyAsync())
            {
                var questionComment = new QuestionComment
                {
                    Id = new Guid("1a1a1a1a-1a1a-1a1a-1a1a-1a1a1a1a1a1a"),
                    QuestionId = new Guid("1a1a1a1a-1a1a-1a1a-1a1a-1a1a1a1a1a1a"), // Câu hỏi về phương trình Đại số lớp 10
                    Content = "Phương trình bậc nhất có dạng ax + b = 0, trong đó a và b là các hằng số. " +
                    "Phương trình bậc hai có dạng ax^2 + bx + c = 0. Để giải phương trình bậc nhất, ta chỉ cần chuyển b sang bên phải và chia cho a. " +
                    "Đối với phương trình bậc hai, ta có thể sử dụng công thức nghiệm hoặc phương pháp phân tích đa thức.",
                    CreatedByUserId = new Guid("4a4a4a4a-4a4a-4a4a-4a4a-4a4a4a4a4a4a"), // Huy Dinh Trong
                };
                context.QuestionComments.Add(questionComment);
                await context.SaveChangesAsync();
            }

            // SystemConfig
            if (!await context.SystemConfigs.AnyAsync())
            {
                var systemConfigs = new List<SystemConfig>
                {
                    new SystemConfig
                    {
                        Key = SystemConfigKeys.DEFAULT_AVATAR_URL,
                        Value = "https://firebasestorage.googleapis.com/v0/b/gdupa-2fa82.appspot.com/o/avatar%2Fdefault_avatar.png?alt=media&token=8654c964-e226-4777-ac66-b60d4182d287",
                        Description = "Default avatar URL for users."
                    },
                     new SystemConfig
                    {
                        Key = SystemConfigKeys.IMPORT_USERS_TEMPLATE,
                        Value = "https://firebasestorage.googleapis.com/v0/b/gdupa-2fa82.appspot.com/o/excel-template%2Fuser-import-template.xlsx?alt=media&token=a1863610-2ab1-4d81-893b-bef6f3f6f4e0",
                        Description = "Template for importing users."
                    },
                    new SystemConfig
                    {
                        Key = SystemConfigKeys.PAYOS_RETURN_URL_PLAN,
                        Value = "https://school.eduva.tech/school-admin",
                        Description = "Default return URL payment plan for users."
                    },
                    new SystemConfig
                    {
                        Key = SystemConfigKeys.PAYOS_RETURN_URL_PACK,
                        Value = "https://school.eduva.tech/teacher/credit-pack",
                        Description = "Default return URL payment pack for users."
                    },

                };
                context.SystemConfigs.AddRange(systemConfigs);
                await context.SaveChangesAsync();
            }
        }
    }
}