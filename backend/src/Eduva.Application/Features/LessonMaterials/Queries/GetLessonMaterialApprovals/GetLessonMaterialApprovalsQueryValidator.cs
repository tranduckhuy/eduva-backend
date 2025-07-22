using FluentValidation;

namespace Eduva.Application.Features.LessonMaterials.Queries.GetLessonMaterialApprovals
{
    public class GetLessonMaterialApprovalsQueryValidator : AbstractValidator<GetLessonMaterialApprovalsQuery>
    {
        public GetLessonMaterialApprovalsQueryValidator()
        {
            RuleFor(x => x.SpecParam).NotNull();
            RuleFor(x => x.SpecParam.PageSize).GreaterThan(0).LessThanOrEqualTo(50);
            RuleFor(x => x.SpecParam.PageIndex).GreaterThanOrEqualTo(0);
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.UserRoles).NotNull();
        }
    }
}