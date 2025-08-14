using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Eduva.Infrastructure.Services;

public class SubscriptionMaintenanceService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriptionMaintenanceService> _logger;
    private readonly int _warningDaysBefore = 3;
    private readonly int _dataRetentionDays = 14;

    public SubscriptionMaintenanceService(IServiceProvider serviceProvider, ILogger<SubscriptionMaintenanceService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Subscription Maintenance Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                TimeZoneInfo vietnamTimeZone = Common.Helper.GetVietnamTimeZone();

                var utcNow = DateTime.UtcNow;
                var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, vietnamTimeZone);

                // Calculate next 2 AM Vietnam time
                var next2AM = vietnamNow.Date.AddHours(2);
                if (vietnamNow.Hour >= 2)
                {
                    next2AM = next2AM.AddDays(1);
                }

                // Convert to UTC for accurate delay calculation
                var next2AMUTC = TimeZoneInfo.ConvertTimeToUtc(next2AM, vietnamTimeZone);
                var delay = next2AMUTC - utcNow;

                if (delay.TotalMilliseconds > 0)
                {
                    _logger.LogInformation("Next maintenance run scheduled at {NextRun} Vietnam time (UTC+7) - {DelayHours:F1} hours from now",
                        next2AM.ToString("yyyy-MM-dd HH:mm:ss"), delay.TotalHours);
                    await Task.Delay(delay, stoppingToken);
                }

                if (stoppingToken.IsCancellationRequested)
                    break;

                var currentVietnamTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
                _logger.LogInformation("Running maintenance at {CurrentTime} Vietnam time",
                    currentVietnamTime.ToString("yyyy-MM-dd HH:mm:ss"));

                await PerformMaintenanceAsync(stoppingToken);

                // Wait until next day's 2 AM
                var nextDay2AM = currentVietnamTime.Date.AddDays(1).AddHours(2);
                var nextDay2AMUTC = TimeZoneInfo.ConvertTimeToUtc(nextDay2AM, vietnamTimeZone);
                var nextDayDelay = nextDay2AMUTC - DateTime.UtcNow;

                if (nextDayDelay.TotalMilliseconds > 0)
                {
                    _logger.LogInformation("Maintenance completed. Next run scheduled at {NextRun} Vietnam time - {DelayHours:F1} hours from now",
                        nextDay2AM.ToString("yyyy-MM-dd HH:mm:ss"), nextDayDelay.TotalHours);
                    await Task.Delay(nextDayDelay, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during subscription maintenance");

                // Wait 1 hour before retrying if there's an error
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        _logger.LogInformation("Subscription Maintenance Service stopped");
    }

    private async Task PerformMaintenanceAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var systemConfigHelper = scope.ServiceProvider.GetRequiredService<ISystemConfigHelper>();

        var packageInfoUrl = await systemConfigHelper.GetPayosReturnUrlPlanAsync();

        try
        {
            _logger.LogInformation("Starting subscription maintenance cycle");

            var schoolSubscriptionRepo = unitOfWork.GetCustomRepository<ISchoolSubscriptionRepository>();
            var userRepo = unitOfWork.GetCustomRepository<IUserRepository>();
            var schoolRepo = unitOfWork.GetRepository<School, int>();
            var lessonMaterialRepo = unitOfWork.GetCustomRepository<ILessonMaterialRepository>();

            TimeZoneInfo vietnamTimeZone = Common.Helper.GetVietnamTimeZone();

            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone).Date;
            var warningDate = now.AddDays(_warningDaysBefore + 1).AddTicks(-1);
            var dataCleanupDate = now.AddDays(-_dataRetentionDays);

            // 1. Send warning emails for subscriptions expiring soon
            await SendExpirationWarningEmailsAsync(schoolSubscriptionRepo, userRepo, schoolRepo, emailSender, now, warningDate, packageInfoUrl, cancellationToken);

            // 2. Send expired emails and update status
            await HandleExpiredSubscriptionsAsync(schoolSubscriptionRepo, userRepo, schoolRepo, emailSender, unitOfWork, now, packageInfoUrl, cancellationToken);

            // 3. Handle storage limit compliance for downgraded subscriptions
            await HandleStorageLimitComplianceAsync(schoolSubscriptionRepo, lessonMaterialRepo, dataCleanupDate, storageService, unitOfWork, cancellationToken);

            // 4. Clean up lesson materials for subscriptions expired more than 14 days
            await CleanupExpiredSubscriptionDataAsync(schoolSubscriptionRepo, lessonMaterialRepo, storageService, unitOfWork, dataCleanupDate, cancellationToken);

            _logger.LogInformation("Subscription maintenance cycle completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform subscription maintenance");
        }
    }

    private async Task SendExpirationWarningEmailsAsync(
        ISchoolSubscriptionRepository schoolSubscriptionRepo,
        IUserRepository userRepo,
        IGenericRepository<School, int> schoolRepo,
        IEmailSender emailSender,
        DateTimeOffset now,
        DateTimeOffset warningDate,
        string packageInfoUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get subscriptions that will expire in the warning period
            var startDate = now.AddDays(1);
            var subscriptionsNearExpiry = await schoolSubscriptionRepo.GetSubscriptionsExpiringBetweenAsync(startDate, warningDate, cancellationToken);

            _logger.LogInformation("Found {Count} subscriptions expiring within {Days} days", subscriptionsNearExpiry.Count, _warningDaysBefore);

            foreach (var subscription in subscriptionsNearExpiry)
            {
                try
                {
                    // Get school admin
                    var schoolAdmin = await userRepo.GetSchoolAdminBySchoolIdAsync(subscription.SchoolId, cancellationToken);

                    if (schoolAdmin == null)
                    {
                        _logger.LogWarning("No school admin found for school {SchoolId}", subscription.SchoolId);
                        continue;
                    }

                    var school = await schoolRepo.GetByIdAsync(subscription.SchoolId);
                    if (school == null)
                    {
                        _logger.LogWarning("School not found for subscription {SubscriptionId}", subscription.Id);
                        continue;
                    }

                    // Send warning email
                    _ = SendSubscriptionWarningEmailAsync(emailSender, schoolAdmin, school, subscription, packageInfoUrl);
                    _logger.LogInformation("Warning email sent to school admin {AdminEmail} for school {SchoolName}", schoolAdmin.Email, school.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send warning email for subscription {SubscriptionId}", subscription.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send expiration warning emails");
        }
    }

    private async Task HandleExpiredSubscriptionsAsync(
        ISchoolSubscriptionRepository schoolSubscriptionRepo,
        IUserRepository userRepo,
        IGenericRepository<School, int> schoolRepo,
        IEmailSender emailSender,
        IUnitOfWork unitOfWork,
        DateTimeOffset now,
        string packageInfoUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            var endDate = now.AddDays(1);
            var expiredToday = await schoolSubscriptionRepo.GetSubscriptionsExpiredOnDateAsync(now, endDate, cancellationToken);

            _logger.LogInformation("Found {Count} subscriptions that expired today", expiredToday.Count);

            foreach (var subscription in expiredToday)
            {
                try
                {
                    // Get school admin
                    var schoolAdmin = await userRepo.GetSchoolAdminBySchoolIdAsync(subscription.SchoolId, cancellationToken);

                    if (schoolAdmin == null)
                    {
                        _logger.LogWarning("No school admin found for school {SchoolId}", subscription.SchoolId);
                        continue;
                    }

                    var school = await schoolRepo.GetByIdAsync(subscription.SchoolId);
                    if (school == null)
                    {
                        _logger.LogWarning("School not found for subscription {SubscriptionId}", subscription.Id);
                        continue;
                    }

                    // Send expiration email
                    _ = SendSubscriptionExpiredEmailAsync(emailSender, schoolAdmin, school, subscription, packageInfoUrl);
                    _logger.LogInformation("Expiration email sent to school admin {AdminEmail} for school {SchoolName}", schoolAdmin.Email, school.Name);

                    // Update subscription status to Expired
                    subscription.SubscriptionStatus = Domain.Enums.SubscriptionStatus.Expired;
                    schoolSubscriptionRepo.Update(subscription);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to handle expired subscription {SubscriptionId}", subscription.Id);
                }
            }

            // Save all status updates
            if (expiredToday.Count > 0)
            {
                await unitOfWork.CommitAsync();
                _logger.LogInformation("Updated status for {Count} expired subscriptions", expiredToday.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle expired subscriptions");
        }
    }

    private async Task CleanupExpiredSubscriptionDataAsync(
        ISchoolSubscriptionRepository schoolSubscriptionRepo,
        ILessonMaterialRepository lessonMaterialRepo,
        IStorageService storageService,
        IUnitOfWork unitOfWork,
        DateTimeOffset dataCleanupDate,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get subscriptions that expired more than retention days ago (14 days)
            var subscriptionsForCleanup = await schoolSubscriptionRepo.GetSubscriptionsExpiredBeforeAsync(dataCleanupDate, cancellationToken);

            _logger.LogInformation("Found {Count} subscriptions eligible for data cleanup", subscriptionsForCleanup.Count);

            foreach (var subscription in subscriptionsForCleanup)
            {
                try
                {
                    var lessonMaterials = await lessonMaterialRepo.GetLessonMaterialsBySchoolOrderedByFileSizeAsync(
                        subscription.SchoolId, cancellationToken);

                    if (lessonMaterials.Count == 0)
                    {
                        _logger.LogInformation("No lesson materials found for school {SchoolId}", subscription.SchoolId);
                        continue;
                    }

                    _logger.LogInformation("Found {Count} lesson materials for cleanup in school {SchoolId}",
                        lessonMaterials.Count, subscription.SchoolId);

                    var deletedCount = 0;
                    var blobsToDelete = new List<string>();

                    foreach (var lessonMaterial in lessonMaterials)
                    {
                        deletedCount++;

                        blobsToDelete.Add(lessonMaterial.SourceUrl);
                        lessonMaterial.FileSize = 0;
                        lessonMaterialRepo.Update(lessonMaterial);
                    }

                    if (blobsToDelete.Count > 0)
                    {
                        await storageService.DeleteRangeFileAsync(blobsToDelete, true);
                    }

                    _logger.LogInformation("Cleaned up {DeletedCount} lesson material blobs for school {SchoolId}",
                        deletedCount, subscription.SchoolId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to cleanup data for subscription {SubscriptionId}", subscription.Id);
                }
            }

            if (subscriptionsForCleanup.Count > 0)
            {
                await unitOfWork.CommitAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired subscription data");
        }
    }

    private async Task SendSubscriptionWarningEmailAsync(
        IEmailSender emailSender,
        ApplicationUser schoolAdmin,
        School school,
        SchoolSubscription subscription,
        string packageInfoUrl)
    {
        var templatePath = Path.Combine(AppContext.BaseDirectory, "email-templates", "template-about-to-expire.html");

        if (!File.Exists(templatePath))
        {
            _logger.LogError("Email template not found: {TemplatePath}", templatePath);
            return;
        }

        // Get Vietnam timezone for date formatting
        TimeZoneInfo vietnamTimeZone = Common.Helper.GetVietnamTimeZone();

        // Convert EndDate to Vietnam time for display
        var expiryDateVietnam = TimeZoneInfo.ConvertTime(subscription.EndDate, vietnamTimeZone);

        var template = await File.ReadAllTextAsync(templatePath);
        var htmlContent = template
            .Replace("{{school_admin_name}}", schoolAdmin.FullName ?? schoolAdmin.Email)
            .Replace("{{school_name}}", school.Name)
            .Replace("{{expiry_date}}", expiryDateVietnam.ToString("dd/MM/yyyy"))
            .Replace("{{package_info_link}}", packageInfoUrl)
            .Replace("{{current_year}}", DateTimeOffset.UtcNow.Year.ToString());

        await emailSender.SendEmailBrevoHtmlAsync(
            schoolAdmin.Email!,
            schoolAdmin.FullName ?? schoolAdmin.Email!,
            "Thông báo: Gói đăng ký sắp hết hạn",
            htmlContent);
    }

    private async Task SendSubscriptionExpiredEmailAsync(
        IEmailSender emailSender,
        ApplicationUser schoolAdmin,
        School school,
        SchoolSubscription subscription,
        string packageInfoUrl)
    {
        var templatePath = Path.Combine(AppContext.BaseDirectory, "email-templates", "template-expired.html");

        if (!File.Exists(templatePath))
        {
            _logger.LogError("Email template not found: {TemplatePath}", templatePath);
            return;
        }

        // Get Vietnam timezone for date formatting
        TimeZoneInfo vietnamTimeZone = Common.Helper.GetVietnamTimeZone();

        // Convert dates to Vietnam time for display
        var expiryDateVietnam = TimeZoneInfo.ConvertTime(subscription.EndDate, vietnamTimeZone);
        var deleteDate = subscription.EndDate.AddDays(_dataRetentionDays);
        var deleteDateVietnam = TimeZoneInfo.ConvertTime(deleteDate, vietnamTimeZone);

        var template = await File.ReadAllTextAsync(templatePath);
        var htmlContent = template
            .Replace("{{school_admin_name}}", schoolAdmin.FullName ?? schoolAdmin.Email)
            .Replace("{{school_name}}", school.Name)
            .Replace("{{expiry_date}}", expiryDateVietnam.ToString("dd/MM/yyyy"))
            .Replace("{{delete_date}}", deleteDateVietnam.ToString("dd/MM/yyyy"))
            .Replace("{{renew_link}}", packageInfoUrl)
            .Replace("{{current_year}}", DateTimeOffset.UtcNow.Year.ToString());

        await emailSender.SendEmailBrevoHtmlAsync(
            schoolAdmin.Email!,
            schoolAdmin.FullName ?? schoolAdmin.Email!,
            "Thông báo: Gói đăng ký đã hết hạn",
            htmlContent);
    }

    private async Task HandleStorageLimitComplianceAsync(
        ISchoolSubscriptionRepository schoolSubscriptionRepo,
        ILessonMaterialRepository lessonMaterialRepo,
        DateTimeOffset dataCleanupDate,
        IStorageService storageService,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get active subscriptions that might have storage limit issues
            var activeSubscriptions = await schoolSubscriptionRepo.GetAllActiveSchoolSubscriptionsExceedingStorageLimitAsync(dataCleanupDate, cancellationToken);

            _logger.LogInformation("Checking storage limit compliance for {Count} active subscriptions", activeSubscriptions.Count);

            foreach (var subscription in activeSubscriptions)
            {
                try
                {
                    var lessonMaterials = await lessonMaterialRepo.GetLessonMaterialsBySchoolOrderedByFileSizeAsync(
                        subscription.SchoolId, cancellationToken);

                    if (lessonMaterials.Count == 0)
                        continue;

                    long totalStorageUsed = lessonMaterials.Sum(lm => (long)lm.FileSize);
                    decimal totalStorageUsedGB = totalStorageUsed / (1024m * 1024m * 1024m);

                    // Get subscription plan storage limit
                    decimal storageLimit = subscription.Plan.StorageLimitGB;

                    _logger.LogInformation("School {SchoolId}: Using {UsedGB:F2}GB / {LimitGB:F2}GB",
                        subscription.SchoolId, totalStorageUsedGB, storageLimit);

                    if (totalStorageUsedGB > storageLimit)
                    {
                        _logger.LogWarning("School {SchoolId} exceeds storage limit. Used: {UsedGB:F2}GB, Limit: {LimitGB:F2}GB",
                            subscription.SchoolId, totalStorageUsedGB, storageLimit);

                        await ReduceStorageToLimit(lessonMaterials, storageLimit, totalStorageUsed, lessonMaterialRepo, storageService, subscription.SchoolId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to check storage compliance for subscription {SubscriptionId}", subscription.Id);
                }
            }

            await unitOfWork.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle storage limit compliance");
        }
    }

    private async Task ReduceStorageToLimit(
        List<LessonMaterial> lessonMaterials,
        decimal storageLimitGB,
        long totalStorageUsed,
        ILessonMaterialRepository lessonMaterialRepo,
        IStorageService storageService,
        int schoolId)
    {
        long storageLimitBytes = (long)(storageLimitGB * 1024 * 1024 * 1024);
        long currentStorageBytes = totalStorageUsed;

        var blobsToDelete = new List<string>();
        var deletedCount = 0;

        foreach (var lessonMaterial in lessonMaterials)
        {
            if (currentStorageBytes <= storageLimitBytes)
                break;

            var originalFileSize = lessonMaterial.FileSize;
            blobsToDelete.Add(lessonMaterial.SourceUrl);
            currentStorageBytes -= lessonMaterial.FileSize;
            deletedCount++;

            lessonMaterial.FileSize = 0;
            lessonMaterialRepo.Update(lessonMaterial);

            _logger.LogInformation("Marked lesson material {LessonMaterialId} for deletion (Size: {SizeMB:F2}MB)",
                lessonMaterial.Id, originalFileSize / (1024.0 * 1024.0));
        }

        if (blobsToDelete.Count > 0)
        {
            await storageService.DeleteRangeFileAsync(blobsToDelete, true);
        }

        decimal newStorageGB = currentStorageBytes / (1024m * 1024m * 1024m);
        _logger.LogInformation("Reduced storage for school {SchoolId}: Deleted {DeletedCount} files, New usage: {NewUsageGB:F2}GB",
            schoolId, deletedCount, newStorageGB);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Subscription Maintenance Service is stopping");
        await base.StopAsync(cancellationToken);
    }

    // This method is public for testing purposes
    public async Task PerformMaintenanceForTestingAsync(CancellationToken cancellationToken = default)
    {
        await PerformMaintenanceAsync(cancellationToken);
    }
}
