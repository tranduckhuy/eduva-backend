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
    }
}