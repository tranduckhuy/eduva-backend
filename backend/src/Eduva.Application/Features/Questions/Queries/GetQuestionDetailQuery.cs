using Eduva.Application.Features.Questions.Responses;
using MediatR;

namespace Eduva.Application.Features.Questions.Queries
{
    public class GetQuestionDetailQuery : IRequest<QuestionDetailResponse>
    {
        public Guid QuestionId { get; set; }
        public Guid CurrentUserId { get; set; }

        public GetQuestionDetailQuery(Guid questionId, Guid currentUserId)
        {
            QuestionId = questionId;
            CurrentUserId = currentUserId;
        }
    }
}