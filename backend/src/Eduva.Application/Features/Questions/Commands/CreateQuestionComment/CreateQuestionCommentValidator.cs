using FluentValidation;

namespace Eduva.Application.Features.Questions.Commands.CreateQuestionComment
{
    public class CreateQuestionCommentValidator : AbstractValidator<CreateQuestionCommentCommand>
    {
        public CreateQuestionCommentValidator()
        {
            RuleFor(x => x.QuestionId)
                .NotEmpty()
                .WithMessage("Question ID is required")
                .Must(id => id != Guid.Empty)
                .WithMessage("Invalid Question ID format");

            RuleFor(x => x.Content)
                .NotEmpty()
                .WithMessage("Content is required");

            RuleFor(x => x.ParentCommentId)
                .Must(id => !id.HasValue || id.Value != Guid.Empty)
                .WithMessage("Parent Comment ID must be valid when provided");

            RuleFor(x => x.CreatedByUserId)
                .NotEmpty()
                .WithMessage("User ID is required")
                .Must(id => id != Guid.Empty)
                .WithMessage("Invalid User ID format");
        }
    }
}