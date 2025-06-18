using Eduva.Application.Features.Classes.Commands;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using FluentValidation;

namespace Eduva.Application.Features.Classes.Commands
{
    public class CreateClassValidator : AbstractValidator<CreateClassCommand>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateClassValidator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Class name is required")
                .MaximumLength(100).WithMessage("Class name must not exceed 100 characters");

            RuleFor(x => x.SchoolId)
                .NotEmpty().WithMessage("School ID is required")
                .MustAsync(SchoolExists).WithMessage("School with specified ID does not exist");
        }

        private async Task<bool> SchoolExists(int schoolId, CancellationToken cancellationToken)
        {
            if (schoolId <= 0) return false;
            
            var schoolRepository = _unitOfWork.GetRepository<School, int>();
            return await schoolRepository.ExistsAsync(schoolId);
        }
    }
}
