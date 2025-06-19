using FluentValidation;

namespace Eduva.Application.Features.Classes.Commands
{
    public class DeleteClassValidator : AbstractValidator<DeleteClassCommand>
    {
        public DeleteClassValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Class ID is required.");
        }
    }
}
