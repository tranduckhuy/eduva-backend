using FluentValidation;

namespace Eduva.Application.Features.Classes.Queries.GetClasses
{
    public class GetClassesValidator : AbstractValidator<GetClassesQuery>
    {
        public GetClassesValidator()
        {
            RuleFor(x => x.ClassSpecParam)
                .NotNull().WithMessage("Class specification parameters are required");
                
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }
}
