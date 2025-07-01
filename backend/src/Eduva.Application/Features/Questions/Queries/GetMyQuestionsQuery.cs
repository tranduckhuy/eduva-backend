using Eduva.Application.Common.Models;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Features.Questions.Specifications;
using MediatR;

namespace Eduva.Application.Features.Questions.Queries
{
    public record GetMyQuestionsQuery(MyQuestionsSpecParam Param, Guid UserId) : IRequest<Pagination<QuestionResponse>>;
}