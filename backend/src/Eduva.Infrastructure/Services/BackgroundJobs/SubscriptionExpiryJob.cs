using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Eduva.Infrastructure.Services.BackgroundJobs
{
    public class SubscriptionExpiryJob : BackgroundService
    {
        private readonly ILogger<SubscriptionExpiryJob> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public SubscriptionExpiryJob(ILogger<SubscriptionExpiryJob> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var subscriptionRepo = unitOfWork.GetCustomRepository<ISchoolSubscriptionRepository>();
                var schoolRepo = unitOfWork.GetRepository<School, int>();

                var now = DateTimeOffset.UtcNow;
                var expiringSubs = await subscriptionRepo.GetExpiringSubscriptionsAsync(now);

                foreach (var sub in expiringSubs)
                {
                    sub.SubscriptionStatus = SubscriptionStatus.Expired;
                    sub.EndDate = now;

                    _logger.LogInformation($"Subscription expired: {sub.Id}");

                    var hasOtherActive = await subscriptionRepo.HasAnyActiveSubscriptionAsync(sub.SchoolId);
                    if (!hasOtherActive)
                    {
                        var school = await schoolRepo.GetByIdAsync(sub.SchoolId);
                        if (school != null && school.Status == EntityStatus.Active)
                        {
                            school.Status = EntityStatus.Inactive;
                            _logger.LogInformation($"School marked as Inactive: {school.Id}");
                        }
                    }
                }

                if (expiringSubs.Any())
                    await unitOfWork.CommitAsync();

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}