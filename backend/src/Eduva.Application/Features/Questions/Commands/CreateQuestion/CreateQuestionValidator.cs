using FluentValidation;

namespace Eduva.Application.Features.Questions.Commands.CreateQuestion
{
    public class CreateQuestionValidator : AbstractValidator<CreateQuestionCommand>
    {
        public CreateQuestionValidator()
        {
            RuleFor(x => x.LessonMaterialId)
                .NotEmpty()
                .WithMessage("LessonMaterialId is required");

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