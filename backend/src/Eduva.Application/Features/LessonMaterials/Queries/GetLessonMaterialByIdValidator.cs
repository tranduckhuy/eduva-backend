using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.LessonMaterials.Queries
{
    public class GetLessonMaterialByIdValidator : AbstractValidator<GetLessonMaterialByIdQuery>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public GetLessonMaterialByIdValidator(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;

            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Lesson material ID must not be empty.")
                .Must(id => id != Guid.Empty).WithMessage("Lesson material ID must not be an empty GUID.");

            // School Exists validation
            RuleFor(x => x.SchoolId)
                .MustAsync(SchoolExistsAsync).WithMessage("The specified school does not exist.")
                .When(x => x.SchoolId.HasValue, ApplyConditionTo.CurrentValidator);
        }

        private async Task<bool> SchoolExistsAsync(int? schoolId, CancellationToken cancellationToken)
        {
            if (!schoolId.HasValue)
            {
                return true;
            }

            var school = await _unitOfWork.GetCustomRepository<ISchoolRepository>().GetByIdAsync(schoolId.Value);

            return school != null;
        }
    }
}
