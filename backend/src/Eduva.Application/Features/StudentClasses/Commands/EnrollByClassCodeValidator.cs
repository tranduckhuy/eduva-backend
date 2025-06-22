using FluentValidation;

namespace Eduva.Application.Features.StudentClasses.Commands
{
    public class EnrollByClassCodeValidator : AbstractValidator<EnrollByClassCodeCommand>
    {
        public EnrollByClassCodeValidator()
        {
            RuleFor(x => x.ClassCode)
                .NotEmpty().WithMessage("Class code is required")
                .MaximumLength(20).WithMessage("Class code cannot exceed 20 characters");
        }
    }
}
