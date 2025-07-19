using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.LessonMaterials.Queries.GetSchoolPublicLessonMaterials
{
    public class GetSchoolPublicLessonMaterialsValidator : AbstractValidator<GetSchoolPublicLessonMaterialsQuery>
    {
        public GetSchoolPublicLessonMaterialsValidator(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            RuleFor(x => x.LessonMaterialSpecParam.SearchTerm)
                .MaximumLength(255).WithMessage("Search term must not exceed 255 characters.");

            RuleFor(x => x.LessonMaterialSpecParam.ContentType)
                .IsInEnum().WithMessage("Invalid content type specified.");
        }
    }
}
