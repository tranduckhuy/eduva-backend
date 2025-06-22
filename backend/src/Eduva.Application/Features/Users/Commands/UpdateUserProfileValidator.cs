using FluentValidation;

namespace Eduva.Application.Features.Users.Commands
{    public class UpdateUserProfileValidator : AbstractValidator<UpdateUserProfileCommand>
    {
        public UpdateUserProfileValidator()
        {
            RuleFor(x => x.FullName)
                .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.")
                .When(x => !string.IsNullOrEmpty(x.FullName));

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\d{10,15}$").WithMessage("Phone number must be between 10 and 15 digits.")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            RuleFor(x => x.AvatarUrl)
                .Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute))
                .WithMessage("Avatar URL must be a valid absolute URL.")
                .When(x => !string.IsNullOrEmpty(x.AvatarUrl));
        }
    }
}
