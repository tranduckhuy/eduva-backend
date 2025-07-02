using FluentValidation;

namespace Eduva.Application.Features.Questions.Commands.CreateQuestionComment
{
    public class CreateQuestionCommentValidator : AbstractValidator<CreateQuestionCommentCommand>
    {
        public CreateQuestionCommentValidator()
        {
            RuleFor(x => x.QuestionId)
                .NotEmpty()
                .WithMessage("QuestionId is required");

            RuleFor(x => x.Content)
                .NotEmpty()
                .WithMessage("Content is required");

            RuleFor(x => x.CreatedByUserId)
                .NotEmpty()
                .WithMessage("CreatedByUserId is required");
        }
    }
}