using Eduva.Application.Common.Models;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Features.Questions.Specifications;
using MediatR;

namespace Eduva.Application.Features.Questions.Queries
{
    public record GetQuestionsByLessonQuery(QuestionsByLessonSpecParam Param, Guid LessonMaterialId, Guid CurrentUserId) : IRequest<Pagination<QuestionResponse>>;
}