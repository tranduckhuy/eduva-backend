using Eduva.Application.Features.Classes.Queries;
using FluentValidation;

namespace Eduva.Application.Features.Classes.Queries
{
    public class GetClassesValidator : AbstractValidator<GetClassesQuery>
    {
        public GetClassesValidator()
        {
            RuleFor(x => x.ClassSpecParam)
                .NotNull().WithMessage("Class specification parameters are required");
        }
    }
}
