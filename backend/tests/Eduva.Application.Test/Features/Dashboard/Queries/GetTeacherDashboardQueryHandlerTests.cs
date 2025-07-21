using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Dashboard.DTOs;
using Eduva.Application.Features.Dashboard.Queries;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.Dashboard.Queries
{
    [TestFixture]
    public class GetTeacherDashboardQueryHandlerTests
    {
        private Mock<IDashboardRepository> _dashboardRepositoryMock = null!;
        private Mock<IUserRepository> _userRepositoryMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private GetTeacherDashboardQueryHandler _handler = null!;

        #region Setup

        [SetUp]
        public void Setup()
        {
            _dashboardRepositoryMock = new Mock<IDashboardRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!
            );
            _handler = new GetTeacherDashboardQueryHandler(
                _dashboardRepositoryMock.Object,
                _userRepositoryMock.Object,
                _userManagerMock.Object
            );
        }

        #endregion

        #region Tests

        [Test]
        public async Task Handle_ShouldReturnCorrectData_ForNormalTeacher()
        {
            // Arrange
            var teacherId = Guid.NewGuid();
            var user = new ApplicationUser { Id = teacherId };
            var request = new GetTeacherDashboardQuery
            {
                TeacherId = teacherId,
                StartDate = DateTimeOffset.UtcNow.AddDays(-10),
                EndDate = DateTimeOffset.UtcNow,
                LessonActivityPeriod = PeriodType.Week,
                QuestionVolumePeriod = PeriodType.Week,
                ContentTypePeriod = PeriodType.Week,
                ReviewLessonsLimit = 2,
                RecentLessonsLimit = 2,
                UnAnswerQuestionsLimit = 2
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(teacherId)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Teacher" });

            var overview = new TeacherSystemOverviewDto { TotalStudents = 10, TotalClasses = 2, AIGeneratedLessons = 10, TotalLessons = 20, UploadedLessons = 20, RemainCreditPoints = 200, TotalPendingLessons = 2, UsedStorageGB = 100, UsedStorageBytes = 100000000, UnansweredQuestions = 2 };
            var lessonActivity = new List<LessonActivityDataPoint> { new() { Period = "2024-01", TotalCount = 6, AIGeneratedCount = 3, UploadedCount = 3 } };
            var questionVolume = new List<QuestionVolumeTrendDto> { new() { Period = "2024-01", TotalQuestions = 3, TotalAnswers = 2, Total = 5 } };
            var contentTypeStats = new List<ContentTypeStatsDto> { new() { Period = "2024-01", Pdf = 2, Total = 10, Audio = 2, Doc = 4, Video = 2, AudioPercentage = 20, DocPercentage = 40, PdfPercentage = 20, VideoPercentage = 20 } };
            var reviewLessons = new List<ReviewLessonDto> { new() { Id = Guid.NewGuid(), Title = "Review", ContentType = ContentType.PDF, CreatedAt = DateTimeOffset.UtcNow, LessonStatus = LessonMaterialStatus.Approved, OwnerName = "S" } };
            var recentLessons = new List<RecentLessonDto> { new() { Id = Guid.NewGuid(), Title = "Recent", ContentType = ContentType.PDF, CreatedAt = DateTimeOffset.UtcNow, LessonStatus = LessonMaterialStatus.Approved, OwnerName = "S" } };
            var unAnswerQuestions = new List<UnAnswerQuestionDto> { new() { Id = Guid.NewGuid(), Title = "Unanswered", OwnerName = "A", LessonName = "L" } };

            _dashboardRepositoryMock.Setup(x => x.GetTeacherSystemOverviewAsync(teacherId, It.IsAny<CancellationToken>())).ReturnsAsync(overview);
            _dashboardRepositoryMock.Setup(x => x.GetTeacherLessonActivityAsync(teacherId, request.LessonActivityPeriod, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(lessonActivity);
            _dashboardRepositoryMock.Setup(x => x.GetTeacherQuestionVolumeTrendAsync(teacherId, request.QuestionVolumePeriod, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(questionVolume);
            _dashboardRepositoryMock.Setup(x => x.GetTeacherContentTypeStatsAsync(teacherId, request.ContentTypePeriod, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(contentTypeStats);
            _dashboardRepositoryMock.Setup(x => x.GetTeacherRecentLessonsAsync(teacherId, request.RecentLessonsLimit, It.IsAny<CancellationToken>())).ReturnsAsync(recentLessons);
            _dashboardRepositoryMock.Setup(x => x.GetTeacherUnAnswerQuestionsAsync(teacherId, request.UnAnswerQuestionsLimit, It.IsAny<CancellationToken>())).ReturnsAsync(unAnswerQuestions);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.SystemOverview, Is.EqualTo(overview));
                Assert.That(result.LessonActivity, Is.EqualTo(lessonActivity));
                Assert.That(result.QuestionVolumeTrend, Is.EqualTo(questionVolume));
                Assert.That(result.ContentTypeStats, Is.EqualTo(contentTypeStats));
                Assert.That(result.ReviewLessons, Is.Empty); // Không phải content moderator
                Assert.That(result.RecentLessons, Is.EqualTo(recentLessons));
                Assert.That(result.UnAnswerQuestions, Is.EqualTo(unAnswerQuestions));
            });
        }

        [Test]
        public async Task Handle_ShouldReturnReviewLessons_ForContentModerator()
        {
            // Arrange
            var teacherId = Guid.NewGuid();
            var user = new ApplicationUser { Id = teacherId };
            var request = new GetTeacherDashboardQuery
            {
                TeacherId = teacherId,
                ReviewLessonsLimit = 3
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(teacherId)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Teacher", "ContentModerator" });

            _dashboardRepositoryMock.Setup(x => x.GetTeacherSystemOverviewAsync(teacherId, It.IsAny<CancellationToken>())).ReturnsAsync(new TeacherSystemOverviewDto());
            _dashboardRepositoryMock.Setup(x => x.GetTeacherLessonActivityAsync(teacherId, It.IsAny<PeriodType>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<LessonActivityDataPoint>());
            _dashboardRepositoryMock.Setup(x => x.GetTeacherQuestionVolumeTrendAsync(teacherId, It.IsAny<PeriodType>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<QuestionVolumeTrendDto>());
            _dashboardRepositoryMock.Setup(x => x.GetTeacherContentTypeStatsAsync(teacherId, It.IsAny<PeriodType?>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<ContentTypeStatsDto>());
            _dashboardRepositoryMock.Setup(x => x.GetTeacherRecentLessonsAsync(teacherId, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<RecentLessonDto>());
            _dashboardRepositoryMock.Setup(x => x.GetTeacherUnAnswerQuestionsAsync(teacherId, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<UnAnswerQuestionDto>());
            var reviewLessons = new List<ReviewLessonDto> { new() { Id = Guid.NewGuid(), Title = "Review", ContentType = ContentType.PDF } };
            _dashboardRepositoryMock.Setup(x => x.GetContentModeratorReviewLessonsAsync(teacherId, 3, It.IsAny<CancellationToken>())).ReturnsAsync(reviewLessons);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ReviewLessons, Is.EqualTo(reviewLessons));
        }

        [Test]
        public void Handle_ShouldThrowException_WhenUserNotFound()
        {
            // Arrange
            var teacherId = Guid.NewGuid();
            var request = new GetTeacherDashboardQuery { TeacherId = teacherId };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(teacherId)).ReturnsAsync((ApplicationUser?)null);

            // Act & Assert
            Assert.ThrowsAsync<AppException>(() => _handler.Handle(request, CancellationToken.None));
        }

        [Test]
        public void Handle_ShouldPropagateRepositoryException()
        {
            // Arrange
            var teacherId = Guid.NewGuid();
            var user = new ApplicationUser { Id = teacherId };
            var request = new GetTeacherDashboardQuery { TeacherId = teacherId };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(teacherId)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Teacher" });
            _dashboardRepositoryMock.Setup(x => x.GetTeacherSystemOverviewAsync(teacherId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("DB error"));

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(request, CancellationToken.None));
        }

        #endregion

    }
}