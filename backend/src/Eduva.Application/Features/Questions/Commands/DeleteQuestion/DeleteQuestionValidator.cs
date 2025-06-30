using FluentValidation;

namespace Eduva.Application.Features.Questions.Commands.DeleteQuestion
{
    public class DeleteQuestionValidator : AbstractValidator<DeleteQuestionCommand>
    {
        public DeleteQuestionValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Question Id is required");
        }
    }
}