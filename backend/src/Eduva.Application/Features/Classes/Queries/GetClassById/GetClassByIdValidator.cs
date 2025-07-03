using FluentValidation;

namespace Eduva.Application.Features.Classes.Queries.GetClassById
{
    public class GetClassByIdValidator : AbstractValidator<GetClassByIdQuery>
    {
        public GetClassByIdValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Class ID is required");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required");
        }
    }
}
