using Eduva.Application.Features.Dashboard.DTOs;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Eduva.Infrastructure.Persistence.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly AppDbContext _context;

        public DashboardRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SystemOverviewDto> GetSystemOverviewAsync(CancellationToken cancellationToken = default)
        {
            var totalUsers = await _context.Users
                .CountAsync(u => u.Status == EntityStatus.Active, cancellationToken);

            var usersByRole = await _context.UserRoles
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .Join(_context.Users.Where(u => u.Status == EntityStatus.Active),
                      urr => urr.UserId, u => u.Id, (urr, u) => urr.Name)
                .GroupBy(roleName => roleName)
                .Select(g => new { RoleName = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var schoolAdmins = usersByRole.FirstOrDefault(x => x.RoleName == nameof(Role.SchoolAdmin))?.Count ?? 0;
            var contentModerators = usersByRole.FirstOrDefault(x => x.RoleName == nameof(Role.ContentModerator))?.Count ?? 0;
            var teachers = usersByRole.FirstOrDefault(x => x.RoleName == nameof(Role.Teacher))?.Count ?? 0;
            var students = usersByRole.FirstOrDefault(x => x.RoleName == nameof(Role.Student))?.Count ?? 0;

            var totalLessons = await _context.LessonMaterials
                .CountAsync(lm => lm.Status == EntityStatus.Active, cancellationToken);

            var uploadedLessons = await _context.LessonMaterials
                .CountAsync(lm => lm.Status == EntityStatus.Active && !lm.IsAIContent, cancellationToken);

            var aiGeneratedLessons = await _context.LessonMaterials
                .CountAsync(lm => lm.Status == EntityStatus.Active && lm.IsAIContent, cancellationToken);

            var totalSchools = await _context.Schools
                .CountAsync(s => s.Status == EntityStatus.Active, cancellationToken);

            var creditPackRevenue = await _context.PaymentTransactions
                .Where(pt => pt.PaymentStatus == PaymentStatus.Paid && pt.PaymentPurpose == PaymentPurpose.CreditPackage)
                .SumAsync(pt => pt.Amount, cancellationToken);

            var subscriptionPlanRevenue = await _context.PaymentTransactions
                .Where(pt => pt.PaymentStatus == PaymentStatus.Paid && pt.PaymentPurpose == PaymentPurpose.SchoolSubscription)
                .SumAsync(pt => pt.Amount, cancellationToken);

            return new SystemOverviewDto
            {
                TotalUsers = totalUsers,
                SchoolAdmins = schoolAdmins,
                ContentModerators = contentModerators,
                Teachers = teachers,
                Students = students,
                TotalLessons = totalLessons,
                UploadedLessons = uploadedLessons,
                AIGeneratedLessons = aiGeneratedLessons,
                TotalSchools = totalSchools,
                CreditPackRevenue = creditPackRevenue,
                SubscriptionPlanRevenue = subscriptionPlanRevenue
            };
        }

        public async Task<List<LessonActivityDataPoint>> GetLessonCreationActivityAsync(
            PeriodType period,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            CancellationToken cancellationToken = default)
        {
            var lessons = await _context.LessonMaterials
                .Where(lm => lm.Status == EntityStatus.Active &&
                           lm.CreatedAt >= startDate &&
                           lm.CreatedAt <= endDate)
                .Select(lm => new { lm.CreatedAt, lm.IsAIContent })
                .ToListAsync(cancellationToken);

            var grouped = lessons.GroupBy(l => GetPeriodKey(l.CreatedAt, period))
                .Select(g => new LessonActivityDataPoint
                {
                    Period = g.Key,
                    UploadedCount = g.Count(x => !x.IsAIContent),
                    AIGeneratedCount = g.Count(x => x.IsAIContent),
                    TotalCount = g.Count()
                })
                .OrderBy(x => x.Period)
                .ToList();

            return grouped;
        }

        public async Task<List<TopSchoolItem>> GetTopSchoolsAsync(
            int topCount,
            CancellationToken cancellationToken = default)
        {
            var topSchools = await _context.Schools
                .Where(s => s.Status == EntityStatus.Active)
                .Select(s => new TopSchoolItem
                {
                    SchoolId = s.Id,
                    SchoolName = s.Name,
                    LessonCount = s.LessonMaterials.Count(lm => lm.Status == EntityStatus.Active),
                    UserCount = s.Users.Count(u => u.Status == EntityStatus.Active),
                    HasActiveSubscription = s.SchoolSubscriptions
                        .Any(sub => sub.SubscriptionStatus == SubscriptionStatus.Active)
                })
                .OrderByDescending(s => s.LessonCount)
                .Take(topCount)
                .ToListAsync(cancellationToken);

            return topSchools;
        }

        public async Task<List<UserRegistrationDataPoint>> GetUserRegistrationStatsAsync(
            PeriodType period,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            CancellationToken cancellationToken = default)
        {
            var users = await _context.Users
                .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                .Join(_context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur })
                .Join(_context.Roles, x => x.ur.RoleId, r => r.Id, (x, r) => new { x.u.CreatedAt, r.Name })
                .ToListAsync(cancellationToken);

            var grouped = users.GroupBy(u => GetPeriodKey(u.CreatedAt, period))
                .Select(g => new UserRegistrationDataPoint
                {
                    Period = g.Key,
                    TotalRegistrations = g.Count(),
                    ContentModerators = g.Count(x => x.Name == nameof(Role.ContentModerator)),
                    Teachers = g.Count(x => x.Name == nameof(Role.Teacher)),
                    Students = g.Count(x => x.Name == nameof(Role.Student))
                })
                .OrderBy(x => x.Period)
                .ToList();

            return grouped;
        }

        public async Task<List<RevenueDataPoint>> GetRevenueStatsAsync(
            PeriodType period,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            CancellationToken cancellationToken = default)
        {
            var payments = await _context.PaymentTransactions
                .Where(pt => pt.PaymentStatus == PaymentStatus.Paid &&
                           pt.CreatedAt >= startDate &&
                           pt.CreatedAt <= endDate)
                .Select(pt => new { pt.CreatedAt, pt.Amount, pt.PaymentPurpose })
                .ToListAsync(cancellationToken);

            var grouped = payments.GroupBy(p => GetPeriodKey(p.CreatedAt, period))
                .Select(g => new RevenueDataPoint
                {
                    Period = g.Key,
                    CreditPackRevenue = g.Where(x => x.PaymentPurpose == PaymentPurpose.CreditPackage)
                                        .Sum(x => x.Amount),
                    SubscriptionRevenue = g.Where(x => x.PaymentPurpose == PaymentPurpose.SchoolSubscription)
                                          .Sum(x => x.Amount),
                    TotalRevenue = g.Sum(x => x.Amount)
                })
                .OrderBy(x => x.Period)
                .ToList();

            return grouped;
        }

        private static string GetPeriodKey(DateTimeOffset date, PeriodType period)
        {
            return period switch
            {
                PeriodType.Day => $"{date.Year}-{date.Month:D2}-{date.Day:D2}",
                PeriodType.Week => $"{date.Year}-W{GetWeekOfYear(date)}",
                PeriodType.Month => $"{date.Year}-{date.Month:D2}",
                PeriodType.Year => $"{date.Year}",
                _ => $"{date.Year}-{date.Month:D2}"
            };
        }

        private static int GetWeekOfYear(DateTimeOffset date)
        {
            var jan1 = new DateTimeOffset(date.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var daysFromJan1 = (date - jan1).Days;
            return (daysFromJan1 / 7) + 1;
        }
    }
}