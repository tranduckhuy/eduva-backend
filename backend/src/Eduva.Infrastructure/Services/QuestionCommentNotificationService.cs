using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Interfaces.Services;
using Eduva.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Eduva.Infrastructure.Services
{
    public class QuestionCommentNotificationService : IQuestionCommentNotificationService
    {
        private readonly IHubContext<QuestionCommentHub> _hubContext;
        private readonly ILogger<QuestionCommentNotificationService> _logger;

        public QuestionCommentNotificationService(
            IHubContext<QuestionCommentHub> hubContext,
            ILogger<QuestionCommentNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyQuestionCreatedAsync(CreateQuestionResponse question, Guid lessonMaterialId)
        {
            try
            {
                var groupName = $"Lesson_{lessonMaterialId}";

                _logger.LogInformation("[SignalR] Starting notification for new question. " +
                    "QuestionId: {QuestionId}, LessonId: {LessonId}, GroupName: {GroupName}, " +
                    "Title: {Title}, CreatedBy: {CreatedBy}",
                    question.Id, lessonMaterialId, groupName, question.Title, question.CreatedByName);

                // Prepare notification data
                var notificationData = new
                {
                    questionId = question.Id,
                    lessonMaterialId,
                    title = question.Title,
                    content = question.Content,
                    createdAt = question.CreatedAt,
                    createdByUserId = question.CreatedByUserId,
                    createdByName = question.CreatedByName,
                    createdByAvatar = question.CreatedByAvatar,
                    createdByRole = question.CreatedByRole,
                    commentCount = question.CommentCount
                };

                _logger.LogInformation("[SignalR] Notification data prepared: {@NotificationData}", notificationData);

                // Send to all users in the lesson group
                await _hubContext.Clients.Group(groupName)
                    .SendAsync("QuestionCreated", notificationData);

                _logger.LogInformation("[SignalR] Question creation notification sent successfully! " +
                    "Event: QuestionCreated, Group: {GroupName}, QuestionId: {QuestionId}",
                    groupName, question.Id);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SignalR] Failed to send question creation notification. " +
                    "QuestionId: {QuestionId}, LessonId: {LessonId}, Error: {ErrorMessage}",
                    question.Id, lessonMaterialId, ex.Message);

            }
        }
    }
}