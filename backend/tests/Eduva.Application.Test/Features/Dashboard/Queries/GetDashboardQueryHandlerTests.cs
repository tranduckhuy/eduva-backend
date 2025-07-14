using Eduva.Application.Features.Dashboard.DTOs;
using Eduva.Application.Features.Dashboard.Queries;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Enums;
using Moq;

namespace Eduva.Application.Test.Features.Dashboard.Queries
{
    [TestFixture]
    public class GetDashboardQueryHandlerTests
    {
        private Mock<IDashboardRepository> _dashboardRepositoryMock = null!;
        private GetDashboardQueryHandler _handler = null!;

        #region Setup

        [SetUp]
        public void Setup()
        {
            _dashboardRepositoryMock = new Mock<IDashboardRepository>();
            _handler = new GetDashboardQueryHandler(_dashboardRepositoryMock.Object);
        }

        #endregion

        #region Tests

        [Test]
        public async Task Handle_ShouldCallAllRepositoryMethods_WhenRequestIsValid()
        {
            // Arrange
            var request = new GetDashboardQuery
            {
                StartDate = DateTimeOffset.UtcNow.AddDays(-30),
                EndDate = DateTimeOffset.UtcNow,
                LessonActivityPeriod = PeriodType.Week,
                UserRegistrationPeriod = PeriodType.Day,
                RevenuePeriod = PeriodType.Month,
                TopSchoolsCount = 10
            };

            SetupRepositoryMocks();

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            VerifyAllRepositoryCalls(request);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.SystemOverview, Is.Not.Null);
            Assert.That(result.LessonActivity, Is.Not.Null);
            Assert.That(result.TopSchools, Is.Not.Null);
            Assert.That(result.UserRegistrations, Is.Not.Null);
            Assert.That(result.RevenueStats, Is.Not.Null);
        }

        [Test]
        public async Task Handle_ShouldUseDefaultDates_WhenDatesAreNull()
        {
            // Arrange
            var request = new GetDashboardQuery
            {
                StartDate = null,
                EndDate = null
            };

            SetupRepositoryMocks();

            // Act
            await _handler.Handle(request, CancellationToken.None);

            // Assert
            _dashboardRepositoryMock.Verify(x => x.GetLessonCreationActivityAsync(
                It.IsAny<PeriodType>(),
                It.Is<DateTimeOffset>(d => d <= DateTimeOffset.UtcNow),
                It.Is<DateTimeOffset>(d => d <= DateTimeOffset.UtcNow),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldUseProvidedDates_WhenDatesAreProvided()
        {
            // Arrange
            var startDate = DateTimeOffset.UtcNow.AddDays(-60);
            var endDate = DateTimeOffset.UtcNow.AddDays(-30);
            var request = new GetDashboardQuery
            {
                StartDate = startDate,
                EndDate = endDate
            };

            SetupRepositoryMocks();

            // Act
            await _handler.Handle(request, CancellationToken.None);

            // Assert
            _dashboardRepositoryMock.Verify(x => x.GetLessonCreationActivityAsync(
                It.IsAny<PeriodType>(),
                startDate,
                endDate,
                It.IsAny<CancellationToken>()), Times.Once);

            _dashboardRepositoryMock.Verify(x => x.GetUserRegistrationStatsAsync(
                It.IsAny<PeriodType>(),
                startDate,
                endDate,
                It.IsAny<CancellationToken>()), Times.Once);

            _dashboardRepositoryMock.Verify(x => x.GetRevenueStatsAsync(
                It.IsAny<PeriodType>(),
                startDate,
                endDate,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldPassCorrectPeriodTypes_ToRepositoryMethods()
        {
            // Arrange
            var request = new GetDashboardQuery
            {
                LessonActivityPeriod = PeriodType.Month,
                UserRegistrationPeriod = PeriodType.Year,
                RevenuePeriod = PeriodType.Year
            };

            SetupRepositoryMocks();

            // Act
            await _handler.Handle(request, CancellationToken.None);

            // Assert
            _dashboardRepositoryMock.Verify(x => x.GetLessonCreationActivityAsync(
                PeriodType.Month,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()), Times.Once);

            _dashboardRepositoryMock.Verify(x => x.GetUserRegistrationStatsAsync(
                PeriodType.Year,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()), Times.Once);

            _dashboardRepositoryMock.Verify(x => x.GetRevenueStatsAsync(
                PeriodType.Year,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldPassCorrectTopSchoolsCount_ToRepository()
        {
            // Arrange
            var request = new GetDashboardQuery { TopSchoolsCount = 15 };
            SetupRepositoryMocks();

            // Act
            await _handler.Handle(request, CancellationToken.None);

            // Assert
            _dashboardRepositoryMock.Verify(x => x.GetTopSchoolsAsync(
                15, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldPassCancellationToken_ToAllRepositoryMethods()
        {
            // Arrange
            var request = new GetDashboardQuery();
            var cancellationToken = new CancellationToken();
            SetupRepositoryMocks();

            // Act
            await _handler.Handle(request, cancellationToken);

            // Assert
            _dashboardRepositoryMock.Verify(x => x.GetSystemOverviewAsync(cancellationToken), Times.Once);
            _dashboardRepositoryMock.Verify(x => x.GetLessonCreationActivityAsync(
                It.IsAny<PeriodType>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), cancellationToken), Times.Once);
            _dashboardRepositoryMock.Verify(x => x.GetTopSchoolsAsync(It.IsAny<int>(), cancellationToken), Times.Once);
            _dashboardRepositoryMock.Verify(x => x.GetUserRegistrationStatsAsync(
                It.IsAny<PeriodType>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), cancellationToken), Times.Once);
            _dashboardRepositoryMock.Verify(x => x.GetRevenueStatsAsync(
                It.IsAny<PeriodType>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldReturnCorrectResponse_WithAllData()
        {
            // Arrange
            var request = new GetDashboardQuery();
            var systemOverview = new SystemOverviewDto { TotalUsers = 100 };
            var lessonActivity = new List<LessonActivityDataPoint> { new() { Period = "2025-01", TotalCount = 50 } };
            var topSchools = new List<TopSchoolItem> { new() { SchoolId = 1, SchoolName = "Test School" } };
            var userRegistrations = new List<UserRegistrationDataPoint> { new() { Period = "2025-01", TotalRegistrations = 20 } };
            var revenueStats = new List<RevenueDataPoint> { new() { Period = "2025-01", TotalRevenue = 1000000 } };

            _dashboardRepositoryMock.Setup(x => x.GetSystemOverviewAsync(It.IsAny<CancellationToken>()))
                                   .ReturnsAsync(systemOverview);
            _dashboardRepositoryMock.Setup(x => x.GetLessonCreationActivityAsync(It.IsAny<PeriodType>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                                   .ReturnsAsync(lessonActivity);
            _dashboardRepositoryMock.Setup(x => x.GetTopSchoolsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                                   .ReturnsAsync(topSchools);
            _dashboardRepositoryMock.Setup(x => x.GetUserRegistrationStatsAsync(It.IsAny<PeriodType>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                                   .ReturnsAsync(userRegistrations);
            _dashboardRepositoryMock.Setup(x => x.GetRevenueStatsAsync(It.IsAny<PeriodType>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                                   .ReturnsAsync(revenueStats);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.SystemOverview, Is.EqualTo(systemOverview));
                Assert.That(result.LessonActivity, Is.EqualTo(lessonActivity));
                Assert.That(result.TopSchools, Is.EqualTo(topSchools));
                Assert.That(result.UserRegistrations, Is.EqualTo(userRegistrations));
                Assert.That(result.RevenueStats, Is.EqualTo(revenueStats));
            });
        }

        [Test]
        public void Handle_ShouldThrowException_WhenRepositoryThrowsException()
        {
            // Arrange
            var request = new GetDashboardQuery();
            _dashboardRepositoryMock.Setup(x => x.GetSystemOverviewAsync(It.IsAny<CancellationToken>()))
                                   .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(request, CancellationToken.None));
        }

        #endregion

        #region Helper Methods

        private void SetupRepositoryMocks()
        {
            _dashboardRepositoryMock.Setup(x => x.GetSystemOverviewAsync(It.IsAny<CancellationToken>()))
                                   .ReturnsAsync(new SystemOverviewDto());
            _dashboardRepositoryMock.Setup(x => x.GetLessonCreationActivityAsync(It.IsAny<PeriodType>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                                   .ReturnsAsync(new List<LessonActivityDataPoint>());
            _dashboardRepositoryMock.Setup(x => x.GetTopSchoolsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                                   .ReturnsAsync(new List<TopSchoolItem>());
            _dashboardRepositoryMock.Setup(x => x.GetUserRegistrationStatsAsync(It.IsAny<PeriodType>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                                   .ReturnsAsync(new List<UserRegistrationDataPoint>());
            _dashboardRepositoryMock.Setup(x => x.GetRevenueStatsAsync(It.IsAny<PeriodType>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                                   .ReturnsAsync(new List<RevenueDataPoint>());
        }

        private void VerifyAllRepositoryCalls(GetDashboardQuery request)
        {
            _dashboardRepositoryMock.Verify(x => x.GetSystemOverviewAsync(It.IsAny<CancellationToken>()), Times.Once);
            _dashboardRepositoryMock.Verify(x => x.GetLessonCreationActivityAsync(
                request.LessonActivityPeriod, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
            _dashboardRepositoryMock.Verify(x => x.GetTopSchoolsAsync(request.TopSchoolsCount, It.IsAny<CancellationToken>()), Times.Once);
            _dashboardRepositoryMock.Verify(x => x.GetUserRegistrationStatsAsync(
                request.UserRegistrationPeriod, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
            _dashboardRepositoryMock.Verify(x => x.GetRevenueStatsAsync(
                request.RevenuePeriod, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

    }
}