using FluentValidation;

namespace Eduva.Application.Features.Users.Commands
{
    public class UpdateUserProfileValidator : AbstractValidator<UpdateUserProfileCommand>
    {
        public UpdateUserProfileValidator()
        {
            RuleFor(x => x.FullName)
                .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.")
                .When(x => !string.IsNullOrEmpty(x.FullName));

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^((03|05|07|08|09)\d{8}|02\d{9})$").WithMessage("Invalid phone number format")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            RuleFor(x => x.AvatarUrl)
                .MaximumLength(255).WithMessage("Avatar URL must not exceed 255 characters.")
                .Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute))
                .WithMessage("Avatar URL must be a valid absolute URL.")
                .When(x => !string.IsNullOrEmpty(x.AvatarUrl));
        }
    }
}