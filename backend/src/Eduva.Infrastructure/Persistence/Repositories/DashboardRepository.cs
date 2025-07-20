using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Dashboard.DTOs;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Persistence.DbContext;
using Eduva.Shared.Enums;
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

        #region Dashboard for System Admin

        public async Task<SystemOverviewDto> GetSystemOverviewAsync(CancellationToken cancellationToken = default)
        {
            var totalUsers = await _context.Users
                .CountAsync(u => u.Status != EntityStatus.Deleted, cancellationToken);

            var usersByRole = await _context.UserRoles
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .Join(_context.Users.Where(u => u.Status != EntityStatus.Deleted),
                      urr => urr.UserId, u => u.Id, (urr, u) => urr.Name)
                .GroupBy(roleName => roleName)
                .Select(g => new { RoleName = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var systemAdmins = usersByRole.FirstOrDefault(x => x.RoleName == nameof(Role.SystemAdmin))?.Count ?? 0;
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
                .SumAsync(pt => (decimal?)pt.Amount, cancellationToken) ?? 0;

            var subscriptionPlanRevenue = await _context.PaymentTransactions
                .Where(pt => pt.PaymentStatus == PaymentStatus.Paid && pt.PaymentPurpose == PaymentPurpose.SchoolSubscription)
                .SumAsync(pt => (decimal?)pt.Amount, cancellationToken) ?? 0;

            const double bytesToGB = 1024.0 * 1024.0 * 1024.0;

            var totalStorageUsedBytes = await _context.LessonMaterials
                .Where(lm => lm.Status == EntityStatus.Active)
                .SumAsync(lm => (long)lm.FileSize, cancellationToken);

            var totalStorageUsedGB = Math.Round(totalStorageUsedBytes / bytesToGB, 2);

            return new SystemOverviewDto
            {
                TotalUsers = totalUsers,
                SystemAdmins = systemAdmins,
                SchoolAdmins = schoolAdmins,
                ContentModerators = contentModerators,
                Teachers = teachers,
                Students = students,
                TotalLessons = totalLessons,
                UploadedLessons = uploadedLessons,
                AIGeneratedLessons = aiGeneratedLessons,
                TotalSchools = totalSchools,
                CreditPackRevenue = creditPackRevenue,
                SubscriptionPlanRevenue = subscriptionPlanRevenue,
                TotalStorageUsedBytes = totalStorageUsedBytes,
                TotalStorageUsedGB = totalStorageUsedGB
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

        public async Task<List<TopSchoolItem>> GetTopSchoolsAsync(int topCount, CancellationToken cancellationToken = default)
        {
            var topSchools = await _context.Schools
                .Where(s => s.Status == EntityStatus.Active)
                .Select(s => new TopSchoolItem
                {
                    SchoolId = s.Id,
                    SchoolName = s.Name,
                    LessonCount = s.LessonMaterials.Count(lm => lm.Status == EntityStatus.Active),
                    UserCount = s.Users.Count(u => u.Status != EntityStatus.Deleted),
                    HasActiveSubscription = s.SchoolSubscriptions.Any(sub => sub.SubscriptionStatus == SubscriptionStatus.Active)
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
                .Where(x => x.Name != nameof(Role.SystemAdmin))
                .ToListAsync(cancellationToken);

            var grouped = users.GroupBy(u => GetPeriodKey(u.CreatedAt, period))
                .Select(g => new UserRegistrationDataPoint
                {
                    Period = g.Key,
                    TotalRegistrations = g.Count(),
                    SchoolAdmins = g.Count(x => x.Name == nameof(Role.SchoolAdmin)),
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

        #endregion

        #region Dashboard for School Admin

        // Dashboard for School Admin
        public async Task<int?> GetSchoolIdByAdminIdAsync(Guid schoolAdminId, CancellationToken cancellationToken)
        {
            return await _context.Users
                .Where(u => u.Id == schoolAdminId && u.Status != EntityStatus.Deleted)
                .Select(u => u.SchoolId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<SchoolAdminSystemOverviewDto> GetSchoolAdminSystemOverviewAsync(int schoolId, CancellationToken cancellationToken)
        {
            var totalUsers = await _context.Users
                .CountAsync(u => u.SchoolId == schoolId && u.Status != EntityStatus.Deleted, cancellationToken);

            var usersByRole = await _context.UserRoles
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .Join(_context.Users.Where(u => u.SchoolId == schoolId && u.Status != EntityStatus.Deleted),
                      urr => urr.UserId, u => u.Id, (urr, u) => urr.Name)
                .GroupBy(roleName => roleName)
                .Select(g => new { RoleName = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var schoolAdmins = usersByRole.FirstOrDefault(x => x.RoleName == nameof(Role.SchoolAdmin))?.Count ?? 0;
            var contentModerators = usersByRole.FirstOrDefault(x => x.RoleName == nameof(Role.ContentModerator))?.Count ?? 0;
            var teachers = usersByRole.FirstOrDefault(x => x.RoleName == nameof(Role.Teacher))?.Count ?? 0;
            var students = usersByRole.FirstOrDefault(x => x.RoleName == nameof(Role.Student))?.Count ?? 0;

            var totalLessons = await _context.LessonMaterials
                .CountAsync(lm => lm.SchoolId == schoolId && lm.Status == EntityStatus.Active, cancellationToken);

            var uploadedLessons = await _context.LessonMaterials
                .CountAsync(lm => lm.SchoolId == schoolId && lm.Status == EntityStatus.Active && !lm.IsAIContent, cancellationToken);

            var aiGeneratedLessons = await _context.LessonMaterials
                .CountAsync(lm => lm.SchoolId == schoolId && lm.Status == EntityStatus.Active && lm.IsAIContent, cancellationToken);

            var classes = await _context.Classes
                .CountAsync(c => c.SchoolId == schoolId && c.Status == EntityStatus.Active, cancellationToken);

            const double bytesToGB = 1024.0 * 1024.0 * 1024.0;

            var usedStorageBytes = await _context.LessonMaterials
                .Where(lm => lm.SchoolId == schoolId && lm.Status == EntityStatus.Active)
                .SumAsync(lm => (long)lm.FileSize, cancellationToken);

            var usedStorageGB = Math.Round(usedStorageBytes / bytesToGB, 2);

            var subscription = await _context.SchoolSubscriptions
                .Where(ss => ss.SchoolId == schoolId && ss.SubscriptionStatus == SubscriptionStatus.Active)
                .OrderByDescending(ss => ss.CreatedAt)
                .Select(ss => new
                {
                    ss.Id,
                    ss.Plan.Name,
                    ss.Plan.PriceMonthly,
                    ss.Plan.PricePerYear,
                    ss.Plan.StorageLimitGB,
                    ss.BillingCycle,
                    ss.StartDate,
                    ss.EndDate
                })
                .FirstOrDefaultAsync(cancellationToken);

            return new SchoolAdminSystemOverviewDto
            {
                TotalUsers = totalUsers,
                SchoolAdmin = schoolAdmins,
                ContentModerators = contentModerators,
                Teachers = teachers,
                Students = students,
                Classes = classes,
                TotalLessons = totalLessons,
                UploadedLessons = uploadedLessons,
                AIGeneratedLessons = aiGeneratedLessons,
                UsedStorageBytes = usedStorageBytes,
                UsedStorageGB = usedStorageGB,
                StorageUsagePercentage = subscription != null ? Math.Round((double)usedStorageBytes / ((double)subscription.StorageLimitGB * 1024 * 1024 * 1024) * 100, 2) : 0,
                CurrentSubscription = subscription != null ? new CurrentSubscriptionDto
                {
                    Id = subscription.Id,
                    Name = subscription.Name,
                    Price = subscription.BillingCycle == BillingCycle.Monthly ? subscription.PriceMonthly : subscription.PricePerYear,
                    MaxStorageBytes = (long)((double)subscription.StorageLimitGB * 1024 * 1024 * 1024),
                    MaxStorageGB = (double)subscription.StorageLimitGB,
                    BillingCycle = subscription.BillingCycle,
                    StartDate = subscription.StartDate,
                    EndDate = subscription.EndDate
                } : new CurrentSubscriptionDto()
            };
        }

        public async Task<List<LessonActivityDataPoint>> GetSchoolAdminLessonActivityAsync(
            int schoolId,
            PeriodType period,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            CancellationToken cancellationToken)
        {
            var lessons = await _context.LessonMaterials
                .Where(lm => lm.SchoolId == schoolId &&
                           lm.Status == EntityStatus.Active &&
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

        public async Task<List<LessonStatusStatsDto>> GetSchoolAdminLessonStatusStatsAsync(
            int schoolId,
            PeriodType period,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            CancellationToken cancellationToken)
        {
            var lessons = await _context.LessonMaterials
                .Where(lm => lm.SchoolId == schoolId &&
                           lm.Status == EntityStatus.Active &&
                           lm.CreatedAt >= startDate &&
                           lm.CreatedAt <= endDate)
                .Select(lm => new { lm.CreatedAt, lm.LessonStatus })
                .ToListAsync(cancellationToken);

            var grouped = lessons.GroupBy(l => GetPeriodKey(l.CreatedAt, period))
                .Select(g => new LessonStatusStatsDto
                {
                    Period = g.Key,
                    Total = g.Count(),
                    Pending = g.Count(x => x.LessonStatus == LessonMaterialStatus.Pending),
                    Approved = g.Count(x => x.LessonStatus == LessonMaterialStatus.Approved),
                    Rejected = g.Count(x => x.LessonStatus == LessonMaterialStatus.Rejected),
                    PendingPercentage = g.Any() ? Math.Round((double)g.Count(x => x.LessonStatus == LessonMaterialStatus.Pending) / g.Count() * 100, 2) : 0,
                    ApprovedPercentage = g.Any() ? Math.Round((double)g.Count(x => x.LessonStatus == LessonMaterialStatus.Approved) / g.Count() * 100, 2) : 0,
                    RejectedPercentage = g.Any() ? Math.Round((double)g.Count(x => x.LessonStatus == LessonMaterialStatus.Rejected) / g.Count() * 100, 2) : 0
                })
                .OrderBy(x => x.Period)
                .ToList();

            return grouped;
        }

        public async Task<List<ContentTypeStatsDto>> GetSchoolAdminContentTypeStatsAsync(
            int schoolId,
            PeriodType period,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            CancellationToken cancellationToken)
        {
            var lessons = await _context.LessonMaterials
                .Where(lm => lm.SchoolId == schoolId &&
                             lm.Status == EntityStatus.Active &&
                             lm.CreatedAt >= startDate &&
                             lm.CreatedAt <= endDate)
                .Select(lm => new { lm.CreatedAt, lm.ContentType })
                .ToListAsync(cancellationToken);

            var grouped = lessons.GroupBy(l => GetPeriodKey(l.CreatedAt, period))
                .Select(g => new ContentTypeStatsDto
                {
                    Period = g.Key,
                    Pdf = g.Count(x => x.ContentType == ContentType.PDF),
                    Doc = g.Count(x => x.ContentType == ContentType.DOCX),
                    Video = g.Count(x => x.ContentType == ContentType.Video),
                    Audio = g.Count(x => x.ContentType == ContentType.Audio),
                    Total = g.Count(),
                    PdfPercentage = g.Any() ? Math.Round((double)g.Count(x => x.ContentType == ContentType.PDF) / g.Count() * 100, 2) : 0,
                    DocPercentage = g.Any() ? Math.Round((double)g.Count(x => x.ContentType == ContentType.DOCX) / g.Count() * 100, 2) : 0,
                    VideoPercentage = g.Any() ? Math.Round((double)g.Count(x => x.ContentType == ContentType.Video) / g.Count() * 100, 2) : 0,
                    AudioPercentage = g.Any() ? Math.Round((double)g.Count(x => x.ContentType == ContentType.Audio) / g.Count() * 100, 2) : 0
                })
                .OrderBy(x => x.Period)
                .ToList();

            return grouped;
        }

        public async Task<List<TopTeachersDto>> GetSchoolAdminTopTeachersAsync(int schoolId, int limit, CancellationToken cancellationToken)
        {
            return await _context.Users
                .Where(u => u.SchoolId == schoolId &&
                            u.Status != EntityStatus.Deleted &&
                            _context.UserRoles.Any(ur => ur.UserId == u.Id &&
                                                       _context.Roles.Any(r => r.Id == ur.RoleId &&
                                                                              (r.Name == nameof(Role.Teacher) || r.Name == nameof(Role.ContentModerator)))))
                .Select(u => new TopTeachersDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    LessonCount = _context.LessonMaterials.Count(lm => lm.CreatedByUserId == u.Id &&
                                                                      lm.SchoolId == schoolId &&
                                                                      lm.Status == EntityStatus.Active),
                    ClassesCount = _context.Classes.Count(c => c.TeacherId == u.Id &&
                                                             c.SchoolId == schoolId &&
                                                             c.Status == EntityStatus.Active)
                })
                .OrderByDescending(t => t.LessonCount)
                .ThenByDescending(t => t.ClassesCount)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ReviewLessonDto>> GetSchoolAdminReviewLessonsAsync(int schoolId, int limit, CancellationToken cancellationToken)
        {
            return await _context.LessonMaterials
                .Where(lm => lm.SchoolId == schoolId &&
                             lm.Status == EntityStatus.Active &&
                             lm.LessonStatus == LessonMaterialStatus.Pending)
                .OrderByDescending(lm => lm.CreatedAt)
                .Take(limit)
                .Select(lm => new ReviewLessonDto
                {
                    Id = lm.Id,
                    Title = lm.Title,
                    ContentType = lm.ContentType,
                    LessonStatus = lm.LessonStatus,
                    CreatedAt = lm.CreatedAt,
                    OwnerName = lm.CreatedByUser.FullName ?? string.Empty
                })
                .ToListAsync(cancellationToken);
        }

        #endregion

        #region Dashboard for Teacher, Content Moderator

        public async Task<TeacherSystemOverviewDto> GetTeacherSystemOverviewAsync(Guid teacherId, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .Where(u => u.Id == teacherId && u.Status != EntityStatus.Deleted)
                .FirstOrDefaultAsync(cancellationToken) ?? throw new AppException(CustomCode.UserNotFound);

            var teacherSchoolId = user.SchoolId;

            var totalStudents = await _context.StudentClasses
                .Where(sc => sc.Class.TeacherId == teacherId &&
                             sc.Class.SchoolId == teacherSchoolId &&
                             sc.Class.Status == EntityStatus.Active)
                .Select(sc => sc.StudentId)
                .Distinct()
                .CountAsync(cancellationToken);

            var totalClasses = await _context.Classes
                .CountAsync(c => c.TeacherId == teacherId &&
                                c.SchoolId == teacherSchoolId &&
                                c.Status == EntityStatus.Active, cancellationToken);

            var totalLessons = await _context.LessonMaterials
                .CountAsync(lm => lm.CreatedByUserId == teacherId &&
                                 lm.SchoolId == teacherSchoolId &&
                                 lm.Status == EntityStatus.Active, cancellationToken);

            var uploadedLessons = await _context.LessonMaterials
                .CountAsync(lm => lm.CreatedByUserId == teacherId &&
                                 lm.SchoolId == teacherSchoolId &&
                                 lm.Status == EntityStatus.Active &&
                                 !lm.IsAIContent, cancellationToken);

            var aiGeneratedLessons = await _context.LessonMaterials
                .CountAsync(lm => lm.CreatedByUserId == teacherId &&
                                 lm.SchoolId == teacherSchoolId &&
                                 lm.Status == EntityStatus.Active &&
                                 lm.IsAIContent, cancellationToken);

            var totalPendingLessons = await _context.LessonMaterials
                .CountAsync(lm => lm.CreatedByUserId == teacherId &&
                                 lm.SchoolId == teacherSchoolId &&
                                 lm.Status == EntityStatus.Active &&
                                 lm.LessonStatus == LessonMaterialStatus.Pending, cancellationToken);

            var remainCreditPoints = user.TotalCredits;

            var unansweredQuestions = await _context.LessonMaterialQuestions
                .CountAsync(q => q.LessonMaterial.CreatedByUserId == teacherId &&
                                q.LessonMaterial.SchoolId == teacherSchoolId &&
                                q.Status == EntityStatus.Active &&
                                !q.Comments.Any(c => c.Status == EntityStatus.Active), cancellationToken);

            const double bytesToGB = 1024.0 * 1024.0 * 1024.0;
            var usedStorageBytes = await _context.LessonMaterials
                .Where(lm => lm.CreatedByUserId == teacherId &&
                            lm.SchoolId == teacherSchoolId &&
                            lm.Status == EntityStatus.Active)
                .SumAsync(lm => (long)lm.FileSize, cancellationToken);

            var usedStorageGB = Math.Round(usedStorageBytes / bytesToGB, 2);

            return new TeacherSystemOverviewDto
            {
                TotalStudents = totalStudents,
                TotalClasses = totalClasses,
                TotalLessons = totalLessons,
                UploadedLessons = uploadedLessons,
                AIGeneratedLessons = aiGeneratedLessons,
                TotalPendingLessons = totalPendingLessons,
                RemainCreditPoints = remainCreditPoints,
                UnansweredQuestions = unansweredQuestions,
                UsedStorageBytes = usedStorageBytes,
                UsedStorageGB = usedStorageGB
            };
        }

        public async Task<List<LessonActivityDataPoint>> GetTeacherLessonActivityAsync(
            Guid teacherId,
            PeriodType period,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            CancellationToken cancellationToken)
        {
            var teacherSchoolId = await _context.Users
                .Where(u => u.Id == teacherId)
                .Select(u => u.SchoolId)
                .FirstOrDefaultAsync(cancellationToken);

            var lessons = await _context.LessonMaterials
                .Where(lm => lm.CreatedByUserId == teacherId &&
                             lm.SchoolId == teacherSchoolId &&
                             lm.Status == EntityStatus.Active &&
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

        public async Task<List<QuestionVolumeTrendDto>> GetTeacherQuestionVolumeTrendAsync(
            Guid teacherId,
            PeriodType period,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            CancellationToken cancellationToken)
        {
            var teacherSchoolId = await _context.Users
                .Where(u => u.Id == teacherId)
                .Select(u => u.SchoolId)
                .FirstOrDefaultAsync(cancellationToken);

            // Get lessons that teacher is using (through folders)
            var teacherUsedLessonIds = await _context.FolderLessonMaterials
                .Include(flm => flm.Folder)
                    .ThenInclude(f => f.Class)
                .Where(flm => flm.Folder != null &&
                              flm.Folder.Class != null &&
                              flm.Folder.Class.TeacherId == teacherId &&
                              flm.Folder.Class.SchoolId == teacherSchoolId)
                .Select(flm => flm.LessonMaterialId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var studentQuestions = await _context.LessonMaterialQuestions
                .Include(q => q.CreatedByUser)
                .Where(q => (q.LessonMaterial.CreatedByUserId == teacherId || // Original lessons
                             teacherUsedLessonIds.Contains(q.LessonMaterialId)) && // Lessons teacher is using
                            q.LessonMaterial.SchoolId == teacherSchoolId &&
                            q.CreatedByUserId != teacherId && // Exclude teacher's own questions
                            q.Status == EntityStatus.Active &&
                            q.CreatedAt >= startDate &&
                            q.CreatedAt <= endDate &&
                            _context.StudentClasses.Any(sc => sc.StudentId == q.CreatedByUserId))
                .Select(q => new { q.CreatedAt, Type = "StudentQuestion" })
                .ToListAsync(cancellationToken);

            var teacherAnswers = await _context.QuestionComments
                .Include(c => c.CreatedByUser)
                .Where(c => (c.Question.LessonMaterial.CreatedByUserId == teacherId || // Original lessons
                             teacherUsedLessonIds.Contains(c.Question.LessonMaterialId)) && // Lessons teacher is using
                            c.Question.LessonMaterial.SchoolId == teacherSchoolId &&
                            c.Status == EntityStatus.Active &&
                            c.CreatedAt >= startDate &&
                            c.CreatedAt <= endDate &&
                            !_context.StudentClasses.Any(sc => sc.StudentId == c.CreatedByUserId))
                .Select(c => new { c.CreatedAt, Type = "TeacherAnswer" })
                .ToListAsync(cancellationToken);

            var allInteractions = studentQuestions.Concat(teacherAnswers).ToList();

            var grouped = allInteractions.GroupBy(x => GetPeriodKey(x.CreatedAt, period))
                .Select(g => new QuestionVolumeTrendDto
                {
                    Period = g.Key,
                    TotalQuestions = g.Count(x => x.Type == "StudentQuestion"),
                    TotalAnswers = g.Count(x => x.Type == "TeacherAnswer"),
                    Total = g.Count()
                })
                .OrderBy(x => x.Period)
                .ToList();

            return grouped;
        }

        public async Task<List<ContentTypeStatsDto>> GetTeacherContentTypeStatsAsync(
            Guid teacherId,
            PeriodType period,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            CancellationToken cancellationToken)
        {
            var teacherSchoolId = await _context.Users
                .Where(u => u.Id == teacherId)
                .Select(u => u.SchoolId)
                .FirstOrDefaultAsync(cancellationToken);

            var lessons = await _context.LessonMaterials
                .Where(lm => lm.CreatedByUserId == teacherId &&
                             lm.SchoolId == teacherSchoolId &&
                             lm.Status == EntityStatus.Active &&
                             lm.CreatedAt >= startDate &&
                             lm.CreatedAt <= endDate)
                .Select(lm => new { lm.CreatedAt, lm.ContentType })
                .ToListAsync(cancellationToken);

            var grouped = lessons.GroupBy(l => GetPeriodKey(l.CreatedAt, period))
                .Select(g => new ContentTypeStatsDto
                {
                    Period = g.Key,
                    Pdf = g.Count(x => x.ContentType == ContentType.PDF),
                    Doc = g.Count(x => x.ContentType == ContentType.DOCX),
                    Video = g.Count(x => x.ContentType == ContentType.Video),
                    Audio = g.Count(x => x.ContentType == ContentType.Audio),
                    Total = g.Count(),
                    PdfPercentage = g.Any() ? Math.Round((double)g.Count(x => x.ContentType == ContentType.PDF) / g.Count() * 100, 2) : 0,
                    DocPercentage = g.Any() ? Math.Round((double)g.Count(x => x.ContentType == ContentType.DOCX) / g.Count() * 100, 2) : 0,
                    VideoPercentage = g.Any() ? Math.Round((double)g.Count(x => x.ContentType == ContentType.Video) / g.Count() * 100, 2) : 0,
                    AudioPercentage = g.Any() ? Math.Round((double)g.Count(x => x.ContentType == ContentType.Audio) / g.Count() * 100, 2) : 0
                })
                .OrderBy(x => x.Period)
                .ToList();

            return grouped;
        }

        public async Task<List<ReviewLessonDto>> GetContentModeratorReviewLessonsAsync(Guid contentModeratorId, int limit, CancellationToken cancellationToken)
        {
            var contentModeratorSchoolId = await _context.Users
                .Where(u => u.Id == contentModeratorId)
                .Select(u => u.SchoolId)
                .FirstOrDefaultAsync(cancellationToken);

            return await _context.LessonMaterials
                .Include(lm => lm.CreatedByUser)
                .Where(lm => lm.SchoolId == contentModeratorSchoolId &&
                             lm.CreatedByUserId != contentModeratorId &&
                             lm.Status == EntityStatus.Active &&
                             lm.LessonStatus == LessonMaterialStatus.Pending)
                .OrderByDescending(lm => lm.CreatedAt)
                .Take(limit)
                .Select(lm => new ReviewLessonDto
                {
                    Id = lm.Id,
                    Title = lm.Title,
                    ContentType = lm.ContentType,
                    LessonStatus = lm.LessonStatus,
                    CreatedAt = lm.CreatedAt,
                    OwnerName = lm.CreatedByUser != null ? lm.CreatedByUser.FullName ?? string.Empty : string.Empty
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<RecentLessonDto>> GetTeacherRecentLessonsAsync(Guid teacherId, int limit, CancellationToken cancellationToken)
        {
            var teacherSchoolId = await _context.Users
                .Where(u => u.Id == teacherId)
                .Select(u => u.SchoolId)
                .FirstOrDefaultAsync(cancellationToken);

            return await _context.LessonMaterials
                .Include(lm => lm.CreatedByUser)
                .Where(lm => lm.CreatedByUserId == teacherId &&
                             lm.SchoolId == teacherSchoolId &&
                             lm.Status == EntityStatus.Active)
                .OrderByDescending(lm => lm.CreatedAt)
                .Take(limit)
                .Select(lm => new RecentLessonDto
                {
                    Id = lm.Id,
                    Title = lm.Title,
                    ContentType = lm.ContentType,
                    LessonStatus = lm.LessonStatus,
                    CreatedAt = lm.CreatedAt,
                    OwnerName = lm.CreatedByUser != null ? lm.CreatedByUser.FullName ?? string.Empty : string.Empty
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<UnAnswerQuestionDto>> GetTeacherUnAnswerQuestionsAsync(Guid teacherId, int limit, CancellationToken cancellationToken)
        {
            var teacherSchoolId = await _context.Users
                .Where(u => u.Id == teacherId)
                .Select(u => u.SchoolId)
                .FirstOrDefaultAsync(cancellationToken);

            // Get lessons that teacher is using (through folders)
            var teacherUsedLessonIds = await _context.FolderLessonMaterials
                .Include(flm => flm.Folder)
                    .ThenInclude(f => f.Class)
                .Where(flm => flm.Folder != null &&
                              flm.Folder.Class != null &&
                              flm.Folder.Class.TeacherId == teacherId &&
                              flm.Folder.Class.SchoolId == teacherSchoolId)
                .Select(flm => flm.LessonMaterialId)
                .Distinct()
                .ToListAsync(cancellationToken);

            return await _context.LessonMaterialQuestions
                .Include(q => q.LessonMaterial)
                .Include(q => q.CreatedByUser)
                .Include(q => q.Comments)
                    .ThenInclude(c => c.Replies)
                .Where(q => (q.LessonMaterial.CreatedByUserId == teacherId || // Original lessons
                             teacherUsedLessonIds.Contains(q.LessonMaterialId)) && // Lessons teacher is using
                            q.LessonMaterial.SchoolId == teacherSchoolId &&
                            q.Status == EntityStatus.Active &&
                            _context.StudentClasses.Any(sc => sc.StudentId == q.CreatedByUserId) &&
                            !q.Comments.Any(c =>
                                (c.Status == EntityStatus.Active &&
                                 !_context.StudentClasses.Any(sc => sc.StudentId == c.CreatedByUserId)) ||
                                (c.Status == EntityStatus.Active &&
                                 c.Replies.Any(r => r.Status == EntityStatus.Active &&
                                                   !_context.StudentClasses.Any(sc => sc.StudentId == r.CreatedByUserId)))))
                .OrderByDescending(q => q.CreatedAt)
                .Take(limit)
                .Select(q => new UnAnswerQuestionDto
                {
                    Id = q.Id,
                    Title = q.Title,
                    OwnerName = q.CreatedByUser != null ? q.CreatedByUser.FullName ?? string.Empty : string.Empty,
                    LessonName = q.LessonMaterial.Title
                })
                .ToListAsync(cancellationToken);
        }

        #endregion

        #region Helper Methods

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

        #endregion

    }
}