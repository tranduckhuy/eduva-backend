using FluentValidation;

namespace Eduva.Application.Features.Questions.Commands.UpdateQuestionComment
{
    public class UpdateQuestionCommentValidator : AbstractValidator<UpdateQuestionCommentCommand>
    {
        public UpdateQuestionCommentValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Comment ID is required");

            RuleFor(x => x.Content)
                .NotEmpty()
                .WithMessage("Content is required");

            RuleFor(x => x.UpdatedByUserId)
                .NotEmpty()
                .WithMessage("UpdatedByUserId is required");
        }
    }
}