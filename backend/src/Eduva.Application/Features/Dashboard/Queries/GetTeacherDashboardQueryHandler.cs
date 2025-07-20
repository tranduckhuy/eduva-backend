using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Dashboard.DTOs;
using Eduva.Application.Features.Dashboard.Responses;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Dashboard.Queries
{
    public class GetTeacherDashboardQueryHandler : IRequestHandler<GetTeacherDashboardQuery, TeacherDashboardResponse>
    {
        private readonly IDashboardRepository _dashboardRepository;
        private readonly IUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public GetTeacherDashboardQueryHandler(
           IDashboardRepository dashboardRepository,
           IUserRepository userRepository,
           UserManager<ApplicationUser> userManager)
        {
            _dashboardRepository = dashboardRepository;
            _userRepository = userRepository;
            _userManager = userManager;
        }


        public async Task<TeacherDashboardResponse> Handle(GetTeacherDashboardQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.TeacherId) ?? throw new AppException(CustomCode.UserNotFound);
            var userRoles = await _userManager.GetRolesAsync(user);
            var isContentModerator = userRoles.Contains(nameof(Role.ContentModerator));

            var endDate = request.EndDate ?? DateTimeOffset.UtcNow;
            var startDate = request.StartDate ?? endDate.AddDays(-30);

            var systemOverview = await _dashboardRepository.GetTeacherSystemOverviewAsync(request.TeacherId, cancellationToken);
            var lessonActivity = await _dashboardRepository.GetTeacherLessonActivityAsync(request.TeacherId, request.LessonActivityPeriod, startDate, endDate, cancellationToken);
            var questionVolumeTrend = await _dashboardRepository.GetTeacherQuestionVolumeTrendAsync(request.TeacherId, request.QuestionVolumePeriod, startDate, endDate, cancellationToken);
            var contentTypeStats = await _dashboardRepository.GetTeacherContentTypeStatsAsync(request.TeacherId, request.ContentTypePeriod, startDate, endDate, cancellationToken);
            var recentLessons = await _dashboardRepository.GetTeacherRecentLessonsAsync(request.TeacherId, request.RecentLessonsLimit, cancellationToken);
            var unAnswerQuestions = await _dashboardRepository.GetTeacherUnAnswerQuestionsAsync(request.TeacherId, request.UnAnswerQuestionsLimit, cancellationToken);

            List<ReviewLessonDto> reviewLessons = [];
            if (isContentModerator)
            {
                reviewLessons = await _dashboardRepository.GetContentModeratorReviewLessonsAsync(request.TeacherId, request.ReviewLessonsLimit, cancellationToken);
            }

            return new TeacherDashboardResponse
            {
                SystemOverview = systemOverview,
                LessonActivity = lessonActivity,
                QuestionVolumeTrend = questionVolumeTrend,
                ContentTypeStats = contentTypeStats,
                ReviewLessons = reviewLessons,
                RecentLessons = recentLessons,
                UnAnswerQuestions = unAnswerQuestions
            };
        }
    }
}