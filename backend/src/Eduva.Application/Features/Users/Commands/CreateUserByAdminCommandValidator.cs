using Eduva.Domain.Enums;
using FluentValidation;

namespace Eduva.Application.Features.Users.Commands
{
    public class CreateUserByAdminCommandValidator : AbstractValidator<CreateUserByAdminCommand>
    {
        public CreateUserByAdminCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required")
                .EmailAddress().WithMessage("Invalid email address");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name is required")
                .MaximumLength(100).WithMessage("Full name must be less than 100 characters");

            RuleFor(x => x.Role)
                .IsInEnum().WithMessage("Invalid role")
                .NotEqual(Role.SystemAdmin).WithMessage("Role cannot be SystemAdmin")
                .NotEqual(Role.SchoolAdmin).WithMessage("Role cannot be SchoolAdmin");

            RuleFor(x => x.InitialPassword)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long")
                .MaximumLength(255).WithMessage("Password must be less than or equal to 255 characters");
        }
    }
}
