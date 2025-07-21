using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Features.Dashboard.DTOs;
using Eduva.Application.Features.Dashboard.Queries;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Enums;
using Moq;

namespace Eduva.Application.Test.Features.Dashboard.Queries
{
    [TestFixture]
    public class GetSchoolAdminDashboardQueryHandlerTests
    {
        private Mock<IDashboardRepository> _dashboardRepositoryMock = null!;
        private GetSchoolAdminDashboardQueryHandler _handler = null!;

        #region Setup

        [SetUp]
        public void Setup()
        {
            _dashboardRepositoryMock = new Mock<IDashboardRepository>();
            _handler = new GetSchoolAdminDashboardQueryHandler(_dashboardRepositoryMock.Object);
        }

        #endregion

        #region Tests

        [Test]
        public async Task Handle_ShouldReturnDashboardResponse_WhenSchoolAdminIdIsValid()
        {
            // Arrange
            var schoolAdminId = Guid.NewGuid();
            int schoolId = 123;
            var request = new GetSchoolAdminDashboardQuery
            {
                SchoolAdminId = schoolAdminId,
                StartDate = DateTimeOffset.UtcNow.AddDays(-10),
                EndDate = DateTimeOffset.UtcNow,
                LessonActivityPeriod = PeriodType.Week,
                LessonStatusPeriod = PeriodType.Month,
                ContentTypePeriod = PeriodType.Week,
                ReviewLessonsLimit = 5,
                TopTeachersLimit = 3
            };

            _dashboardRepositoryMock.Setup(x => x.GetSchoolIdByAdminIdAsync(schoolAdminId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(schoolId);

            var overview = new SchoolAdminSystemOverviewDto()
            {
                TotalUsers = 100,
                TotalLessons = 50,
                Classes = 20,
                Teachers = 10,
                Students = 70,
                AIGeneratedLessons = 30,
                UploadedLessons = 20,
                ContentModerators = 5,
                SchoolAdmin = 2,
                UsedStorageBytes = 500000000,
                UsedStorageGB = 500,
                StorageUsagePercentage = 50,
                CurrentSubscription = new CurrentSubscriptionDto
                {
                    Name = "Premium",
                    StartDate = DateTimeOffset.UtcNow,
                    EndDate = DateTimeOffset.UtcNow.AddMonths(1),
                    Price = 200000,
                    AmountPaid = 200000,
                    MaxStorageBytes = 1000000000,
                    MaxStorageGB = 1000,
                    BillingCycle = BillingCycle.Monthly,
                    Id = Guid.NewGuid(),
                }
            };
            var lessonActivity = new List<LessonActivityDataPoint> { new() { Period = "2024-01", TotalCount = 10, AIGeneratedCount = 5, UploadedCount = 5 } };
            var lessonStatus = new List<LessonStatusStatsDto> { new() { Period = "2024-01", Total = 10, Approved = 2, Pending = 2, Rejected = 6, ApprovedPercentage = 20, PendingPercentage = 20, RejectedPercentage = 60 } };
            var contentType = new List<ContentTypeStatsDto> { new() { Period = "2024-01", Total = 10, Audio = 2, Doc = 2, Pdf = 2, Video = 4, AudioPercentage = 20, DocPercentage = 20, PdfPercentage = 20, VideoPercentage = 40 } };
            var topTeachers = new List<TopTeachersDto> { new() { Id = Guid.NewGuid(), FullName = "Teacher", LessonCount = 5, ClassesCount = 2 } };
            var reviewLessons = new List<ReviewLessonDto> { new() { Id = Guid.NewGuid(), Title = "Lesson", ContentType = ContentType.PDF, CreatedAt = DateTimeOffset.UtcNow, LessonStatus = LessonMaterialStatus.Approved, OwnerName = "S" } };

            _dashboardRepositoryMock.Setup(x => x.GetSchoolAdminSystemOverviewAsync(schoolId, It.IsAny<CancellationToken>())).ReturnsAsync(overview);
            _dashboardRepositoryMock.Setup(x => x.GetSchoolAdminLessonActivityAsync(schoolId, request.LessonActivityPeriod, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(lessonActivity);
            _dashboardRepositoryMock.Setup(x => x.GetSchoolAdminLessonStatusStatsAsync(schoolId, request.LessonStatusPeriod, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(lessonStatus);
            _dashboardRepositoryMock.Setup(x => x.GetSchoolAdminContentTypeStatsAsync(schoolId, request.ContentTypePeriod, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(contentType);
            _dashboardRepositoryMock.Setup(x => x.GetSchoolAdminTopTeachersAsync(schoolId, request.TopTeachersLimit, It.IsAny<CancellationToken>())).ReturnsAsync(topTeachers);
            _dashboardRepositoryMock.Setup(x => x.GetSchoolAdminReviewLessonsAsync(schoolId, request.ReviewLessonsLimit, It.IsAny<CancellationToken>())).ReturnsAsync(reviewLessons);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.SystemOverview, Is.EqualTo(overview));
                Assert.That(result.LessonActivity, Is.EqualTo(lessonActivity));
                Assert.That(result.LessonStatusStats, Is.EqualTo(lessonStatus));
                Assert.That(result.ContentTypeStats, Is.EqualTo(contentType));
                Assert.That(result.TopTeachers, Is.EqualTo(topTeachers));
                Assert.That(result.ReviewLessons, Is.EqualTo(reviewLessons));
            });
        }

        [Test]
        public void Handle_ShouldThrowUserNotPartOfSchoolException_WhenSchoolIdIsNull()
        {
            // Arrange
            var schoolAdminId = Guid.NewGuid();
            var request = new GetSchoolAdminDashboardQuery { SchoolAdminId = schoolAdminId };

            _dashboardRepositoryMock.Setup(x => x.GetSchoolIdByAdminIdAsync(schoolAdminId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((int?)null);

            // Act & Assert
            Assert.ThrowsAsync<UserNotPartOfSchoolException>(() => _handler.Handle(request, CancellationToken.None));
        }

        [Test]
        public async Task Handle_ShouldUseDefaultDates_WhenDatesAreNull()
        {
            // Arrange
            var schoolAdminId = Guid.NewGuid();
            int schoolId = 123;
            var request = new GetSchoolAdminDashboardQuery { SchoolAdminId = schoolAdminId, StartDate = null, EndDate = null };

            _dashboardRepositoryMock.Setup(x => x.GetSchoolIdByAdminIdAsync(schoolAdminId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(schoolId);

            _dashboardRepositoryMock.Setup(x => x.GetSchoolAdminSystemOverviewAsync(schoolId, It.IsAny<CancellationToken>())).ReturnsAsync(new SchoolAdminSystemOverviewDto());
            _dashboardRepositoryMock.Setup(x => x.GetSchoolAdminLessonActivityAsync(schoolId, It.IsAny<PeriodType>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<LessonActivityDataPoint>());
            _dashboardRepositoryMock.Setup(x => x.GetSchoolAdminLessonStatusStatsAsync(schoolId, It.IsAny<PeriodType>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<LessonStatusStatsDto>());
            _dashboardRepositoryMock.Setup(x => x.GetSchoolAdminContentTypeStatsAsync(schoolId, It.IsAny<PeriodType?>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<ContentTypeStatsDto>());
            _dashboardRepositoryMock.Setup(x => x.GetSchoolAdminTopTeachersAsync(schoolId, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<TopTeachersDto>());
            _dashboardRepositoryMock.Setup(x => x.GetSchoolAdminReviewLessonsAsync(schoolId, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<ReviewLessonDto>());

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            _dashboardRepositoryMock.Verify(x => x.GetSchoolAdminLessonActivityAsync(
                schoolId,
                It.IsAny<PeriodType>(),
                It.Is<DateTimeOffset>(d => d <= DateTimeOffset.UtcNow),
                It.Is<DateTimeOffset>(d => d <= DateTimeOffset.UtcNow),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldPassCorrectLimitsAndPeriods_ToRepository()
        {
            // Arrange
            var schoolAdminId = Guid.NewGuid();
            int schoolId = 123;
            var request = new GetSchoolAdminDashboardQuery
            {
                SchoolAdminId = schoolAdminId,
                LessonActivityPeriod = PeriodType.Month,
                LessonStatusPeriod = PeriodType.Week,
                ContentTypePeriod = PeriodType.Month,
                ReviewLessonsLimit = 7,
                TopTeachersLimit = 8
            };

            _dashboardRepositoryMock.Setup(x => x.GetSchoolIdByAdminIdAsync(schoolAdminId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(schoolId);

            _dashboardRepositoryMock.Setup(x => x.GetSchoolAdminSystemOverviewAsync(schoolId, It.IsAny<CancellationToken>())).ReturnsAsync(new SchoolAdminSystemOverviewDto());
            _dashboardRepositoryMock.Setup(x => x.GetSchoolAdminLessonActivityAsync(schoolId, request.LessonActivityPeriod, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<LessonActivityDataPoint>());
            _dashboardRepositoryMock.Setup(x => x.GetSchoolAdminLessonStatusStatsAsync(schoolId, request.LessonStatusPeriod, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<LessonStatusStatsDto>());
            _dashboardRepositoryMock.Setup(x => x.GetSchoolAdminContentTypeStatsAsync(schoolId, request.ContentTypePeriod, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<ContentTypeStatsDto>());
            _dashboardRepositoryMock.Setup(x => x.GetSchoolAdminTopTeachersAsync(schoolId, request.TopTeachersLimit, It.IsAny<CancellationToken>())).ReturnsAsync(new List<TopTeachersDto>());
            _dashboardRepositoryMock.Setup(x => x.GetSchoolAdminReviewLessonsAsync(schoolId, request.ReviewLessonsLimit, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ReviewLessonDto>());

            // Act
            await _handler.Handle(request, CancellationToken.None);

            // Assert
            _dashboardRepositoryMock.Verify(x => x.GetSchoolAdminLessonActivityAsync(schoolId, PeriodType.Month, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
            _dashboardRepositoryMock.Verify(x => x.GetSchoolAdminLessonStatusStatsAsync(schoolId, PeriodType.Week, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
            _dashboardRepositoryMock.Verify(x => x.GetSchoolAdminContentTypeStatsAsync(schoolId, PeriodType.Month, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
            _dashboardRepositoryMock.Verify(x => x.GetSchoolAdminTopTeachersAsync(schoolId, 8, It.IsAny<CancellationToken>()), Times.Once);
            _dashboardRepositoryMock.Verify(x => x.GetSchoolAdminReviewLessonsAsync(schoolId, 7, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void Handle_ShouldPropagateRepositoryException()
        {
            // Arrange
            var schoolAdminId = Guid.NewGuid();
            int schoolId = 123;
            var request = new GetSchoolAdminDashboardQuery { SchoolAdminId = schoolAdminId };

            _dashboardRepositoryMock.Setup(x => x.GetSchoolIdByAdminIdAsync(schoolAdminId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(schoolId);

            _dashboardRepositoryMock.Setup(x => x.GetSchoolAdminSystemOverviewAsync(schoolId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("DB error"));

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(request, CancellationToken.None));
        }

        #endregion

    }
}