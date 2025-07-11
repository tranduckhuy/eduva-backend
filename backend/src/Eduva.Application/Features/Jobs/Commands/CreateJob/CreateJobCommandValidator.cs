using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using FluentValidation;

namespace Eduva.Application.Features.Jobs.Commands.CreateJob
{
    public class CreateJobCommandValidator : AbstractValidator<CreateJobCommand>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateJobCommandValidator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required.")
                .MustAsync(IsValidUserId).WithMessage("User does not exist.");

            RuleFor(x => x.File)
                .NotNull().WithMessage("File is required.")
                .Must(file => file.Count > 0).WithMessage("File cannot be empty.");
            RuleFor(x => x.Topic)
                .NotEmpty().WithMessage("Topic is required.")
                .MaximumLength(2000).WithMessage("Topic cannot exceed 2000 characters.");
        }

        private async Task<bool> IsValidUserId(Guid userId, CancellationToken cancellationToken = default)
        {
            var userRepository = _unitOfWork.GetCustomRepository<IUserRepository>();
            return await userRepository.ExistsAsync(userId);
        }
    }
}
