using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;

namespace Eduva.Application.Features.Classes.Commands
{
    public class DeleteClassHandler : IRequestHandler<DeleteClassCommand, ClassResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeleteClassHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ClassResponse> Handle(DeleteClassCommand request, CancellationToken cancellationToken)
        {
            var classroomRepository = _unitOfWork.GetCustomRepository<IClassroomRepository>();
            var classroom = await classroomRepository.GetByIdAsync(request.Id)
                ?? throw new AppException(CustomCode.ClassNotFound);
            if (classroom.TeacherId != request.TeacherId)
            {
                throw new AppException(CustomCode.Unauthorized);
            }

            classroom.Status = EntityStatus.Archived;
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
                throw new AppException(CustomCode.ClassArchiveFailed);
            }
        }
    }
}
