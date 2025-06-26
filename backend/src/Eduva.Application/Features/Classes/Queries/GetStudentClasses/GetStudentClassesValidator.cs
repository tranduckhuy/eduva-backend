using Eduva.Application.Common.Specifications;
using FluentValidation;

namespace Eduva.Application.Features.Classes.Queries.GetStudentClasses
{
    public class GetStudentClassesValidator : AbstractValidator<GetStudentClassesQuery>
    {
        public GetStudentClassesValidator()
        {
            RuleFor(x => x.StudentClassSpecParam.PageIndex)
                .GreaterThan(0).WithMessage("Page index must be greater than 0");

            RuleFor(x => x.StudentClassSpecParam.PageSize)
                .GreaterThan(0).WithMessage("Page size must be greater than 0")
                .LessThanOrEqualTo(BaseSpecParam.MaxPageSize).WithMessage($"Page size must be less than or equal to {BaseSpecParam.MaxPageSize}");

            RuleFor(x => x.StudentClassSpecParam.SearchTerm)
                .MaximumLength(255).WithMessage("Search term must not exceed 255 characters.");
        }
    }
}
