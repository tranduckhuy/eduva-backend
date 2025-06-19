using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Shared.Enums;
using MediatR;

namespace Eduva.Application.Features.Classes.Commands
{
    public class UpdateClassHandler : IRequestHandler<UpdateClassCommand, ClassResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateClassHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ClassResponse> Handle(UpdateClassCommand request, CancellationToken cancellationToken)
        {
            var classroomRepository = _unitOfWork.GetCustomRepository<IClassroomRepository>();

            // Get the classroom by ID
            var classroom = await classroomRepository.GetByIdAsync(request.Id)
                ?? throw new AppException(CustomCode.ClassNotFound);

            // Check if the teacher is authorized to update this class
            if (classroom.TeacherId != request.TeacherId)
            {
                throw new AppException(CustomCode.Unauthorized);
            }

            // Check if the school exists
            var schoolRepository = _unitOfWork.GetRepository<School, int>();
            var school = await schoolRepository.GetByIdAsync(classroom.SchoolId)
                ?? throw new AppException(CustomCode.SchoolNotFound);

            // Check if the new class name already exists in the school (excluding current class)
            if (classroom.Name.ToLower() != request.Name.ToLower())
            {
                bool classExists = await classroomRepository.ExistsAsync(c =>
                    c.Id != request.Id &&
                    c.SchoolId == classroom.SchoolId &&
                    c.Name.ToLower() == request.Name.ToLower());
                if (classExists)
                {
                    throw new AppException(CustomCode.ClassNameAlreadyExists);
                }
            }

            // Update only the fields that should be updated
            classroom.Name = request.Name;
            classroom.Status = request.Status;
            classroom.LastModifiedAt = DateTimeOffset.UtcNow;

            classroomRepository.Update(classroom);

            try
            {
                await _unitOfWork.CommitAsync();

                // Map the response with teacher and school information
                var response = AppMapper.Mapper.Map<ClassResponse>(classroom);
                response.TeacherName = classroom.Teacher?.FullName ?? string.Empty;
                response.SchoolName = school.Name;

                return response;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new AppException(CustomCode.ClassUpdateFailed);
            }
        }
    }
}
