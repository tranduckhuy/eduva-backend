using Eduva.Application.Features.Dashboard.Responses;
using Eduva.Application.Interfaces.Repositories;
using MediatR;

namespace Eduva.Application.Features.Dashboard.Queries
{
    public class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardResponse>
    {
        private readonly IDashboardRepository _dashboardRepository;

        public GetDashboardQueryHandler(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public async Task<DashboardResponse> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
        {
            // Set default date range if not provided (last 30 days)
            var endDate = request.EndDate ?? DateTimeOffset.UtcNow;
            var startDate = request.StartDate ?? endDate.AddDays(-30);

            // Execute operations sequentially to avoid DbContext threading issues
            var systemOverview = await _dashboardRepository.GetSystemOverviewAsync(cancellationToken);

            var lessonActivity = await _dashboardRepository.GetLessonCreationActivityAsync(
                request.LessonActivityPeriod, startDate, endDate, cancellationToken);

            var topSchools = await _dashboardRepository.GetTopSchoolsAsync(
                request.TopSchoolsCount, cancellationToken);

            var userRegistrations = await _dashboardRepository.GetUserRegistrationStatsAsync(
                request.UserRegistrationPeriod, startDate, endDate, cancellationToken);

            var revenueStats = await _dashboardRepository.GetRevenueStatsAsync(
                request.RevenuePeriod, startDate, endDate, cancellationToken);

            return new DashboardResponse
            {
                SystemOverview = systemOverview,
                LessonActivity = lessonActivity,
                TopSchools = topSchools,
                UserRegistrations = userRegistrations,
                RevenueStats = revenueStats
            };
        }
    }
}