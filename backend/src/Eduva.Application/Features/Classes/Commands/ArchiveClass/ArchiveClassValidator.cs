using FluentValidation;

namespace Eduva.Application.Features.Classes.Commands.ArchiveClass
{
    public class ArchiveClassValidator : AbstractValidator<ArchiveClassCommand>
    {
        public ArchiveClassValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Class ID is required.");
        }
    }
}
