using Eduva.Application.Features.Dashboard.DTOs;
using Eduva.Domain.Enums;

namespace Eduva.Application.Interfaces.Repositories
{
    public interface IDashboardRepository
    {
        Task<SystemOverviewDto> GetSystemOverviewAsync(CancellationToken cancellationToken = default);
        Task<List<LessonActivityDataPoint>> GetLessonCreationActivityAsync(
            PeriodType period,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            CancellationToken cancellationToken = default);
        Task<List<TopSchoolItem>> GetTopSchoolsAsync(
            int topCount,
            CancellationToken cancellationToken = default);
        Task<List<UserRegistrationDataPoint>> GetUserRegistrationStatsAsync(
            PeriodType period,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            CancellationToken cancellationToken = default);
        Task<List<RevenueDataPoint>> GetRevenueStatsAsync(
            PeriodType period,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            CancellationToken cancellationToken = default);

        Task<int?> GetSchoolIdByAdminIdAsync(Guid schoolAdminId, CancellationToken cancellationToken = default);
        Task<SchoolAdminSystemOverviewDto> GetSchoolAdminSystemOverviewAsync(int schoolId, CancellationToken cancellationToken = default);
        Task<List<LessonActivityDataPoint>> GetSchoolAdminLessonActivityAsync(
            int schoolId,
            PeriodType period,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            CancellationToken cancellationToken = default);
        Task<List<LessonStatusStatsDto>> GetSchoolAdminLessonStatusStatsAsync(
            int schoolId,
            PeriodType period,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            CancellationToken cancellationToken = default);
        Task<List<ContentTypeStatsDto>> GetSchoolAdminContentTypeStatsAsync(
            int schoolId,
            PeriodType period,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            CancellationToken cancellationToken = default);
        Task<List<TopTeachersDto>> GetSchoolAdminTopTeachersAsync(int schoolId, int limit, CancellationToken cancellationToken = default);
        Task<List<ReviewLessonDto>> GetSchoolAdminReviewLessonsAsync(int schoolId, int limit, CancellationToken cancellationToken = default);

        Task<TeacherSystemOverviewDto> GetTeacherSystemOverviewAsync(Guid teacherId, CancellationToken cancellationToken = default);
        Task<List<LessonActivityDataPoint>> GetTeacherLessonActivityAsync(Guid teacherId, PeriodType period, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default);
        Task<List<QuestionVolumeTrendDto>> GetTeacherQuestionVolumeTrendAsync(Guid teacherId, PeriodType period, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default);
        Task<List<ContentTypeStatsDto>> GetTeacherContentTypeStatsAsync(Guid teacherId, PeriodType period, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default);
        Task<List<ReviewLessonDto>> GetContentModeratorReviewLessonsAsync(Guid teacherId, int limit, CancellationToken cancellationToken = default); // Tái sử dụng ReviewLessonDto
        Task<List<RecentLessonDto>> GetTeacherRecentLessonsAsync(Guid teacherId, int limit, CancellationToken cancellationToken = default);
        Task<List<UnAnswerQuestionDto>> GetTeacherUnAnswerQuestionsAsync(Guid teacherId, int limit, CancellationToken cancellationToken = default);
    }
}