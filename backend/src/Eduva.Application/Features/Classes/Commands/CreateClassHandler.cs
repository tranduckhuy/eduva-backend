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
            // Check the existence of SchoolId
            var schoolRepository = _unitOfWork.GetRepository<School, int>();
            var school = await schoolRepository.GetByIdAsync(request.SchoolId);
            if (school == null)
            {
                throw new AppException(CustomCode.UserNotExists, new[] { $"School with ID {request.SchoolId} not found" });
            }

            var classroom = AppMapper.Mapper.Map<Classroom>(request);
            
            // Automatically create classcode 8 characters
            classroom.ClassCode = GenerateUniqueClassCode();

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

        private static string GenerateUniqueClassCode()
        {
            // Create random codes 8 characters including capital letters and numbers
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var code = new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            
            return code;
        }
    }
}
