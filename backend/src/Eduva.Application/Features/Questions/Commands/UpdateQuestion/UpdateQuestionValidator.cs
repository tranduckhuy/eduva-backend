using FluentValidation;

namespace Eduva.Application.Features.Questions.Commands.UpdateQuestion
{
    public class UpdateQuestionValidator : AbstractValidator<UpdateQuestionCommand>
    {
        public UpdateQuestionValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Question Id is required");

            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Title is required")
                .MaximumLength(255)
                .WithMessage("Title must not exceed 255 characters");

            RuleFor(x => x.Content)
                .NotEmpty()
                .WithMessage("Content is required");
        }
    }
}