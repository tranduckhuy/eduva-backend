using Eduva.Application.Features.SchoolSubscriptions.Configurations;
using Eduva.Application.Features.SchoolSubscriptions.Configurations.PayOSService;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Infrastructure.Configurations;
using Eduva.Infrastructure.Configurations.Email;
using Eduva.Infrastructure.Configurations.ExcelTemplate;
using Eduva.Infrastructure.Email;
using Eduva.Infrastructure.Persistence.DbContext;
using Eduva.Infrastructure.Persistence.Repositories;
using Eduva.Infrastructure.Persistence.UnitOfWork;
using Eduva.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Net.payOS;

namespace Eduva.Infrastructure.Extensions
{
    public static class InfrastructureExtension
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));
            services.AddHealthChecks().Services.AddDbContext<AppDbContext>();

            // Add Redis distributed cache
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration["Redis:ConnectionString"];
                options.InstanceName = configuration["Redis:InstanceName"];
            });

            // Add Email Configuration
            var emailConfig = configuration.GetSection("EmailConfiguration").Get<EmailConfiguration>();
            services.AddSingleton(emailConfig ?? throw new InvalidDataException("EmailConfiguration is missing in appsettings.json"));
            services.AddScoped<IEmailSender, EmailSender>();

            // Azure Blob Storage Options
            var azureBlobStorageOptions = configuration.GetSection("AzureBlobStorage").Get<AzureBlobStorageOptions>();
            services.AddSingleton(azureBlobStorageOptions ?? throw new InvalidDataException("AzureBlobStorageOptions is missing in appsettings.json"));
            services.AddScoped<IStorageService, AzureBlobStorageService>();

            // Unit of Work 
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Excel Service
            services.Configure<ImportTemplateConfig>(configuration.GetSection("ImportTemplate"));

            // Register repositories
            services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
            services.AddScoped<IRepositoryFactory, RepositoryFactory>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ILessonMaterialRepository, LessonMaterialRepository>();
            services.AddScoped<ISchoolSubscriptionRepository, SchoolSubscriptionRepository>();
            services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
            services.AddScoped<IClassroomRepository, ClassroomRepository>();
            services.AddScoped<ISchoolRepository, SchoolRepository>();
            services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
            services.AddScoped<IAICreditPackRepository, AICreditPackRepository>();

            services.AddScoped<PayOS>(provider =>
            {
                var config = provider.GetRequiredService<IOptions<PayOSConfig>>().Value;
                return new PayOS(config.PAYOS_CLIENT_ID, config.PAYOS_API_KEY, config.PAYOS_CHECKSUM_KEY);
            });

            // Payment Configuration
            services.Configure<PayOSConfig>(configuration.GetSection(PayOSConfig.ConfigName));
            services.AddScoped<IPayOSService, PayOSService>();

            services.AddHttpClient();
            services.AddHttpClient("EduvaHttpClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            });


            services.AddScoped<ISchoolSubscriptionService, SchoolSubscriptionService>();
            services.AddScoped<ISchoolValidationService, SchoolValidationService>();

            return services;
        }
    }
}