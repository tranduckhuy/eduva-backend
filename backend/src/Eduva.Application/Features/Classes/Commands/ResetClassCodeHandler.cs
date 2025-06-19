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
    public class ResetClassCodeHandler : IRequestHandler<ResetClassCodeCommand, ClassResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private static readonly string[] ClassCodeFailedMessage = { "Failed to reset class code." };

        public ResetClassCodeHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ClassResponse> Handle(ResetClassCodeCommand request, CancellationToken cancellationToken)
        {
            var classroomRepository = _unitOfWork.GetCustomRepository<IClassroomRepository>();
            
            // Get the classroom by ID
            var classroom = await classroomRepository.GetByIdAsync(request.Id);
            if (classroom == null)
            {
                throw new AppException(CustomCode.ClassNotFound, new[] { $"Class with ID {request.Id} not found" });
            }

            // Check if the teacher is authorized to update this class
            if (classroom.TeacherId != request.TeacherId)
            {
                throw new AppException(CustomCode.Unauthorized, new[] { "You are not authorized to reset class code" });
            }

            // Generate new unique class code with retry
            string newClassCode;
            bool codeExists;
            int maxAttempts = 5; // Limit retries to avoid infinite loop
            int attempt = 0;
            
            do {
                newClassCode = ClassCodeGenerator.GenerateClassCode();
                codeExists = await classroomRepository.ExistsAsync(c => 
                    c.Id != classroom.Id && c.ClassCode == newClassCode);
                attempt++;
            } while (codeExists && attempt < maxAttempts);
            
            if (codeExists)
            {
                throw new AppException(CustomCode.ClassCodeDuplicate, 
                    new[] { "Failed to generate unique class code after multiple attempts. Please try again." });
            }
            
            // Save old class code for reference if needed
            string oldClassCode = classroom.ClassCode;
            
            // Update class with new class code
            classroom.ClassCode = newClassCode;
            classroom.LastModifiedAt = DateTimeOffset.UtcNow;
            
            classroomRepository.Update(classroom);

            try
            {
                await _unitOfWork.CommitAsync();
                
                var response = AppMapper.Mapper.Map<ClassResponse>(classroom);
                response.TeacherName = classroom.Teacher?.FullName ?? string.Empty;
                response.SchoolName = classroom.School?.Name ?? string.Empty;
                
                return response;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new AppException(CustomCode.ClassUpdateFailed, 
                    new[] { $"Failed to reset class code: {ex.Message}" });
            }
        }
    }
}
