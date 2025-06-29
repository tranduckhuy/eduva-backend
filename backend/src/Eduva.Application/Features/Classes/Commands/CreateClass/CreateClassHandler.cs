using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Features.Classes.Utilities;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;

namespace Eduva.Application.Features.Classes.Commands.CreateClass
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

            // Check if the school exists and is active
            var schoolRepository = _unitOfWork.GetRepository<School, int>();
            var school = await schoolRepository.GetByIdAsync(request.SchoolId);

            if (school == null)
            {
                throw new AppException(CustomCode.SchoolNotFound);
            }
            if (school.Status != EntityStatus.Active)
            {
                throw new AppException(CustomCode.CannotCreateClassForInactiveSchool);
            }

            // Check if the class name already exists for this teacher
            bool classExistsForTeacher = await classroomRepository.ExistsAsync(c =>
                c.TeacherId == request.TeacherId &&
                c.Name.ToLower() == request.Name.ToLower());
            if (classExistsForTeacher)
            {
                throw new AppException(CustomCode.ClassNameAlreadyExistsForTeacher);
            }

            if (string.IsNullOrWhiteSpace(request.BackgroundImageUrl))
            {
                throw new AppException(CustomCode.ProvidedInformationIsInValid);
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
                throw new AppException(CustomCode.ClassCodeDuplicate);
            }

            classroom.ClassCode = classCode;

            if (classroom.Id == Guid.Empty)
                classroom.Id = Guid.NewGuid();

            await classroomRepository.AddAsync(classroom);

            var folderRepository = _unitOfWork.GetCustomRepository<IFolderRepository>();
            var folder = new Folder
            {
                Name = $"Thư mục lớp {classroom.Name}",
                ClassId = classroom.Id,
                OwnerType = OwnerType.Class,
                UserId = null,
                Order = 1,
                Status = EntityStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                LastModifiedAt = DateTimeOffset.UtcNow
            };
            await folderRepository.AddAsync(folder);

            try
            {
                await _unitOfWork.CommitAsync();
                return AppMapper.Mapper.Map<ClassResponse>(classroom);
            }
            catch (Exception)
            {
                throw new AppException(CustomCode.ClassCreateFailed);
            }
        }
    }
}
