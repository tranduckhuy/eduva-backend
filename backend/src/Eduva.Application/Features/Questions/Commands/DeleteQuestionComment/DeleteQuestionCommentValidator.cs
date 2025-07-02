using FluentValidation;

namespace Eduva.Application.Features.Questions.Commands.DeleteQuestionComment
{
    public class DeleteQuestionCommentValidator : AbstractValidator<DeleteQuestionCommentCommand>
    {
        public DeleteQuestionCommentValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Comment ID is required.");

            RuleFor(x => x.DeletedByUserId)
                .NotEmpty()
                .WithMessage("User ID is required.");
        }
    }
}