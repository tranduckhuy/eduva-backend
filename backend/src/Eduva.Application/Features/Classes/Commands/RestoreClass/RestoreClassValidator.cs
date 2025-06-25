using FluentValidation;

namespace Eduva.Application.Features.Classes.Commands.RestoreClass
{
    public class RestoreClassValidator : AbstractValidator<RestoreClassCommand>
    {
        public RestoreClassValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Class ID is required.");
        }
    }
}
