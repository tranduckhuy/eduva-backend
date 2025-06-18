using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.Classes.Commands;
using Eduva.Application.Features.Classes.Responses;
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
              // Kiểm tra sự tồn tại của SchoolId
            var schoolRepository = _unitOfWork.GetRepository<School, int>();
            var school = await schoolRepository.GetByIdAsync(request.SchoolId);
            if (school == null)
            {
                throw new AppException(CustomCode.UserNotExists, new[] { $"School with ID {request.SchoolId} not found" });
            }

            var classroom = AppMapper.Mapper.Map<Classroom>(request);

            await classroomRepository.AddAsync(classroom);

            try
            {
                await _unitOfWork.CommitAsync();
                return AppMapper.Mapper.Map<ClassResponse>(classroom);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }
    }
}
