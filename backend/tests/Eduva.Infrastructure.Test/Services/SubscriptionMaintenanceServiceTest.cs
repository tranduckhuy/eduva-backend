using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Eduva.Infrastructure.Test.Services;

[TestFixture]
public class SubscriptionMaintenanceServiceTest
{
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<IServiceScope> _serviceScopeMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private Mock<ISchoolSubscriptionRepository> _schoolSubscriptionRepoMock;
    private Mock<IUserRepository> _userRepoMock;
    private Mock<ILessonMaterialRepository> _lessonMaterialRepoMock;
    private Mock<IGenericRepository<School, int>> _schoolRepoMock;
    private Mock<IEmailSender> _emailSenderMock;
    private Mock<IStorageService> _storageServiceMock;
    private Mock<ILogger<SubscriptionMaintenanceService>> _loggerMock;
    private Mock<ISystemConfigHelper> _systemConfigHelperMock;
    private SubscriptionMaintenanceService _service;

    [SetUp]
    public void Setup()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _schoolSubscriptionRepoMock = new Mock<ISchoolSubscriptionRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _lessonMaterialRepoMock = new Mock<ILessonMaterialRepository>();
        _schoolRepoMock = new Mock<IGenericRepository<School, int>>();
        _emailSenderMock = new Mock<IEmailSender>();
        _storageServiceMock = new Mock<IStorageService>();
        _loggerMock = new Mock<ILogger<SubscriptionMaintenanceService>>();
        _systemConfigHelperMock = new Mock<ISystemConfigHelper>();

        // Setup system config helper to return default values
        _systemConfigHelperMock.Setup(x => x.GetPayosReturnUrlPlanAsync())
            .ReturnsAsync("http://example.com/plan");

        // Setup service scope factory
        var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        serviceScopeFactoryMock.Setup(factory => factory.CreateScope()).Returns(_serviceScopeMock.Object);
        _serviceScopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);

        // Setup service provider to return mocked services
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory))).Returns(serviceScopeFactoryMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IUnitOfWork))).Returns(_unitOfWorkMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IEmailSender))).Returns(_emailSenderMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IStorageService))).Returns(_storageServiceMock.Object);

        // Setup unit of work to return mocked repositories
        _unitOfWorkMock.Setup(uow => uow.GetCustomRepository<ISchoolSubscriptionRepository>())
            .Returns(_schoolSubscriptionRepoMock.Object);
        _unitOfWorkMock.Setup(uow => uow.GetCustomRepository<IUserRepository>())
            .Returns(_userRepoMock.Object);
        _unitOfWorkMock.Setup(uow => uow.GetCustomRepository<ILessonMaterialRepository>())
            .Returns(_lessonMaterialRepoMock.Object);
        _unitOfWorkMock.Setup(uow => uow.GetRepository<School, int>())
            .Returns(_schoolRepoMock.Object);

        _service = new SubscriptionMaintenanceService(_serviceProviderMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task HandleStorageLimitCompliance_ShouldDeleteLargestFiles_WhenStorageExceedsLimit()
    {
        // Arrange
        var schoolId = 1;
        var subscription = new SchoolSubscription
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            SubscriptionStatus = SubscriptionStatus.Active,
            Plan = new SubscriptionPlan
            {
                Id = 1,
                Name = "Basic Plan",
                StorageLimitGB = 1.0m // 1GB limit
            }
        };

        var lessonMaterials = new List<LessonMaterial>
        {
            new() { Id = Guid.NewGuid(), SchoolId = schoolId, FileSize = 600 * 1024 * 1024, SourceUrl = "http://example.com/file1.pdf" }, // 600MB
            new() { Id = Guid.NewGuid(), SchoolId = schoolId, FileSize = 500 * 1024 * 1024, SourceUrl = "http://example.com/file2.pdf" }, // 500MB
            new() { Id = Guid.NewGuid(), SchoolId = schoolId, FileSize = 100 * 1024 * 1024, SourceUrl = "http://example.com/file3.pdf" }, // 100MB
        };
        // Total: 1.2GB, exceeds 1GB limit, should delete 600MB file to get under limit

        _schoolSubscriptionRepoMock.Setup(repo => repo.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<SchoolSubscription, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SchoolSubscription> { subscription });

        _lessonMaterialRepoMock.Setup(repo => repo.GetLessonMaterialsBySchoolOrderedByFileSizeAsync(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lessonMaterials);

        // Act
        using var cts = new CancellationTokenSource();
        await _service.PerformMaintenanceForTestingAsync(cts.Token);

        // Assert
        _lessonMaterialRepoMock.Verify(repo => repo.Update(It.IsAny<LessonMaterial>()), Times.AtLeastOnce);
        _storageServiceMock.Verify(service => service.DeleteRangeFileAsync(It.IsAny<List<string>>(), It.IsAny<bool>()), Times.AtLeastOnce);
        _unitOfWorkMock.Verify(uow => uow.CommitAsync(), Times.AtLeastOnce);
    }

    [Test]
    public async Task CleanupExpiredSubscriptionData_ShouldDeleteAllFiles_WhenSubscriptionExpiredMoreThan14Days()
    {
        // Arrange
        var schoolId = 1;
        var subscription = new SchoolSubscription
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            EndDate = DateTimeOffset.UtcNow.AddDays(-20), // Expired 20 days ago
            SubscriptionStatus = SubscriptionStatus.Expired
        };

        var lessonMaterials = new List<LessonMaterial>
        {
            new() { Id = Guid.NewGuid(), SchoolId = schoolId, FileSize = 100 * 1024 * 1024, SourceUrl = "http://example.com/file1.pdf" },
            new() { Id = Guid.NewGuid(), SchoolId = schoolId, FileSize = 200 * 1024 * 1024, SourceUrl = "http://example.com/file2.pdf" },
            new() { Id = Guid.NewGuid(), SchoolId = schoolId, FileSize = 150 * 1024 * 1024, SourceUrl = "http://example.com/file3.pdf" },
        };

        _schoolSubscriptionRepoMock.Setup(repo => repo.GetSubscriptionsExpiredBeforeAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SchoolSubscription> { subscription });

        _lessonMaterialRepoMock.Setup(repo => repo.GetLessonMaterialsBySchoolOrderedByFileSizeAsync(schoolId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lessonMaterials);

        // Act
        using var cts = new CancellationTokenSource();
        await _service.PerformMaintenanceForTestingAsync(cts.Token);

        // Assert - Should delete ALL files for expired subscriptions
        _lessonMaterialRepoMock.Verify(repo => repo.Update(It.IsAny<LessonMaterial>()), Times.Exactly(3));
        _storageServiceMock.Verify(service => service.DeleteRangeFileAsync(It.IsAny<List<string>>(), It.IsAny<bool>()), Times.AtLeastOnce);
    }

    [Test]
    public void SchedulingLogic_ShouldCalculateCorrectDelayFor2AMVietnamTime()
    {
        // Arrange - Test the same logic as in the service with fallback timezone handling
        TimeZoneInfo vietnamTimeZone;
        try
        {
            vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }

        // Test case 1: Current time is 1 AM Vietnam time - should run at 2 AM same day
        var testTime1AM = new DateTime(2024, 1, 15, 1, 30, 0, DateTimeKind.Unspecified);
        var testTime1AMUTC = TimeZoneInfo.ConvertTimeToUtc(testTime1AM, vietnamTimeZone);

        var next2AM = testTime1AM.Date.AddHours(2);
        if (testTime1AM.Hour >= 2)
        {
            next2AM = next2AM.AddDays(1);
        }
        var next2AMUTC = TimeZoneInfo.ConvertTimeToUtc(next2AM, vietnamTimeZone);
        var delay = next2AMUTC - testTime1AMUTC;

        // Should be 30 minutes delay (from 1:30 AM to 2:00 AM)
        Assert.That(delay.TotalMinutes, Is.EqualTo(30).Within(1));

        // Test case 2: Current time is 3 AM Vietnam time - should run at 2 AM next day
        var testTime3AM = new DateTime(2024, 1, 15, 3, 0, 0, DateTimeKind.Unspecified);
        var testTime3AMUTC = TimeZoneInfo.ConvertTimeToUtc(testTime3AM, vietnamTimeZone);

        var next2AMNextDay = testTime3AM.Date.AddHours(2);
        if (testTime3AM.Hour >= 2)
        {
            next2AMNextDay = next2AMNextDay.AddDays(1);
        }
        var next2AMNextDayUTC = TimeZoneInfo.ConvertTimeToUtc(next2AMNextDay, vietnamTimeZone);
        var delayNextDay = next2AMNextDayUTC - testTime3AMUTC;

        // Should be 23 hours delay (from 3 AM today to 2 AM tomorrow)
        Assert.That(delayNextDay.TotalHours, Is.EqualTo(23).Within(0.1));
    }

    [Test]
    public void TimeZoneFallback_ShouldWorkOnAllPlatforms()
    {
        // Arrange & Act - Test that timezone fallback logic works
        TimeZoneInfo vietnamTimeZone;
        bool fallbackUsed = false;

        try
        {
            vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            fallbackUsed = true;
        }

        // Assert
        Assert.That(vietnamTimeZone, Is.Not.Null);
        Assert.That(vietnamTimeZone.BaseUtcOffset, Is.EqualTo(TimeSpan.FromHours(7)));

        // Verify timezone is working correctly
        var utcTime = new DateTime(2024, 1, 15, 19, 0, 0, DateTimeKind.Utc); // 7 PM UTC
        var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, vietnamTimeZone);
        Assert.That(vietnamTime.Hour, Is.EqualTo(2)); // Should be 2 AM next day in Vietnam

        // Log which timezone was used for debugging
        TestContext.WriteLine($"Using timezone: {vietnamTimeZone.Id}, Fallback used: {fallbackUsed}");
    }

    [TearDown]
    public void TearDown()
    {
        _service?.Dispose();
    }
}
