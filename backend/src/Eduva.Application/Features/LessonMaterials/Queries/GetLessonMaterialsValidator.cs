using Eduva.Application.Interfaces;
using FluentValidation;

namespace Eduva.Application.Features.LessonMaterials.Queries
{
    public class GetLessonMaterialsValidator : AbstractValidator<GetLessonMaterialsQuery>
    {
        private readonly IUnitOfWork _unitOfWork;
        public GetLessonMaterialsValidator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            RuleFor(x => x.LessonMaterialSpecParam.SearchTerm)
                .MaximumLength(255).WithMessage("Search term must not exceed 255 characters.");

            RuleFor(x => x.LessonMaterialSpecParam.Tag)
                .MaximumLength(100).WithMessage("Tag must not exceed 100 characters.");
        }
    }
}
