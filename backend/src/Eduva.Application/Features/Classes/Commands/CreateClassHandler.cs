using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Features.Classes.Utilities;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Shared.Enums;
using MediatR;

namespace Eduva.Application.Features.Classes.Commands
{
    public class CreateClassHandler : IRequestHandler<CreateClassCommand, ClassResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateClassHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ClassResponse> Handle(CreateClassCommand request, CancellationToken cancellationToken)
        {
            var classroomRepository = _unitOfWork.GetCustomRepository<IClassroomRepository>();
            // Check the existence of SchoolId
            var schoolRepository = _unitOfWork.GetRepository<School, int>();
            var school = await schoolRepository.GetByIdAsync(request.SchoolId);
            if (school == null)
            {
                throw new AppException(CustomCode.SchoolNotFound, new[] { $"School with ID {request.SchoolId} not found" });
            }

            // Check if class name already exists in the school
            bool classExists = await classroomRepository.ExistsAsync(c =>
                c.SchoolId == request.SchoolId &&
                c.Name.ToLower() == request.Name.ToLower());
            if (classExists)
            {
                throw new AppException(CustomCode.ClassNameAlreadyExists,
                    new[] { $"Class with name '{request.Name}' already exists in this school" });
            }

            var classroom = AppMapper.Mapper.Map<Classroom>(request);

            // Automatically create classcode 8 characters (with retry for duplicates)
            string classCode;
            bool codeExists;
            int maxAttempts = 5;
            int attempt = 0;
            do
            {
                classCode = ClassCodeGenerator.GenerateClassCode();
                codeExists = await classroomRepository.ExistsAsync(c => c.ClassCode == classCode);
                attempt++;
            } while (codeExists && attempt < maxAttempts);

            if (codeExists)
            {
                throw new AppException(CustomCode.ClassCodeDuplicate,
                    new[] { "Failed to generate unique class code after multiple attempts. Please try again." });
            }

            classroom.ClassCode = classCode;

            await classroomRepository.AddAsync(classroom);

            try
            {
                await _unitOfWork.CommitAsync();
                return AppMapper.Mapper.Map<ClassResponse>(classroom);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new AppException(CustomCode.ClassCreateFailed, new[] { $"Failed to create class: {ex.Message}" });
            }
        }
    }
}
