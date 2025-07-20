using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Features.Dashboard.Responses;
using Eduva.Application.Interfaces.Repositories;
using MediatR;

namespace Eduva.Application.Features.Dashboard.Queries
{
    public class GetSchoolAdminDashboardQueryHandler : IRequestHandler<GetSchoolAdminDashboardQuery, SchoolAdminDashboardResponse>
    {
        private readonly IDashboardRepository _dashboardRepository;

        public GetSchoolAdminDashboardQueryHandler(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public async Task<SchoolAdminDashboardResponse> Handle(GetSchoolAdminDashboardQuery request, CancellationToken cancellationToken)
        {
            var schoolId = await _dashboardRepository.GetSchoolIdByAdminIdAsync(request.SchoolAdminId, cancellationToken);

            if (!schoolId.HasValue)
            {
                throw new UserNotPartOfSchoolException();
            }

            var startDate = request.StartDate ?? DateTimeOffset.UtcNow.AddDays(-30);
            var endDate = request.EndDate ?? DateTimeOffset.UtcNow;

            var systemOverview = await _dashboardRepository.GetSchoolAdminSystemOverviewAsync(schoolId.Value, cancellationToken);
            var lessonActivity = await _dashboardRepository.GetSchoolAdminLessonActivityAsync(schoolId.Value, request.LessonActivityPeriod, startDate, endDate, cancellationToken);
            var lessonStatusStats = await _dashboardRepository.GetSchoolAdminLessonStatusStatsAsync(schoolId.Value, request.LessonStatusPeriod, startDate, endDate, cancellationToken);
            var contentTypeStats = await _dashboardRepository.GetSchoolAdminContentTypeStatsAsync(schoolId.Value, cancellationToken, request.ContentTypePeriod, startDate, endDate);
            var topTeachers = await _dashboardRepository.GetSchoolAdminTopTeachersAsync(schoolId.Value, request.TopTeachersLimit, cancellationToken);
            var reviewLessons = await _dashboardRepository.GetSchoolAdminReviewLessonsAsync(schoolId.Value, request.ReviewLessonsLimit, cancellationToken);

            return new SchoolAdminDashboardResponse
            {
                SystemOverview = systemOverview,
                LessonActivity = lessonActivity,
                ReviewLessons = reviewLessons,
                ContentTypeStats = contentTypeStats,
                LessonStatusStats = lessonStatusStats,
                TopTeachers = topTeachers
            };
        }
    }
}