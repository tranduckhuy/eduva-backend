using FluentValidation;

namespace Eduva.Application.Features.Classes.Queries.GetTeacherClasses
{
    public class GetTeacherClassesValidator : AbstractValidator<GetTeacherClassesQuery>
    {
        public GetTeacherClassesValidator()
        {
            RuleFor(x => x.TeacherId)
                .NotEmpty().WithMessage("TeacherId is required");
                
            RuleFor(x => x.ClassSpecParam)
                .NotNull().WithMessage("ClassSpecParam is required");
        }
    }
}
