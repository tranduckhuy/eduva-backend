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
                .NotEmpty().WithMessage("At least one file is required.")
                .Must(files => files.Count <= 5).WithMessage("No more than 5 files are allowed.");

            RuleForEach(x => x.File)
                .NotNull().WithMessage("Each file must not be null.")
                .Must(file =>
                {
                    var allowedContentTypes = new[]
                    {
                                "application/pdf",
                                "application/msword",
                                "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                    };
                    return allowedContentTypes.Contains(file.ContentType);
                }).WithMessage("Only PDF or Word documents (.pdf, .doc, .docx) are allowed.");

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
