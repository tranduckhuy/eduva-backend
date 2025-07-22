using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Classes.Commands.EnrollByClassCode
{
    public class EnrollByClassCodeHandler : IRequestHandler<EnrollByClassCodeCommand, StudentClassResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public EnrollByClassCodeHandler(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<StudentClassResponse> Handle(EnrollByClassCodeCommand request, CancellationToken cancellationToken)
        {
            // Check if student exists
            var userRepository = _unitOfWork.GetRepository<ApplicationUser, Guid>();
            var student = await userRepository.GetByIdAsync(request.StudentId)
                ?? throw new AppException(CustomCode.UserNotExists);

            // Check if user has the Student role
            var studentRoles = await _userManager.GetRolesAsync(student);
            if (!studentRoles.Contains(nameof(Role.Student)))
            {
                throw new AppException(CustomCode.UserNotStudent);
            }

            if (student.SchoolId == null)
            {
                throw new AppException(CustomCode.SchoolNotFound);
            }

            var classroomRepository = _unitOfWork.GetRepository<Classroom, Guid>();
            var allClassrooms = await classroomRepository.GetAllAsync();
            var classrooms = allClassrooms
                .Where(c => c.ClassCode == request.ClassCode)
                .ToList();

            if (classrooms.Count == 0)
            {
                throw new AppException(CustomCode.ClassNotFound);
            }

            var sameSchoolClasses = classrooms
                .Where(c => c.SchoolId == student.SchoolId)
                .ToList();

            if (sameSchoolClasses.Count > 1)
            {
                throw new AppException(CustomCode.DuplicateClassCodeSameSchool);
            }

            var classroom = sameSchoolClasses.FirstOrDefault();
            if (classroom == null)
            {
                throw new AppException(CustomCode.StudentCannotEnrollDifferentSchool);
            }

            if (classroom.Status != EntityStatus.Active)
            {
                throw new AppException(CustomCode.ClassNotActive);
            }

            if (student.SchoolId == null)
            {
                throw new AppException(CustomCode.SchoolNotFound);
            }
            if (student.SchoolId != classroom.SchoolId)
            {
                throw new AppException(CustomCode.StudentCannotEnrollDifferentSchool);
            }

            // Use specialized StudentClassRepository
            var studentClassRepository = _unitOfWork.GetCustomRepository<IStudentClassRepository>();

            // Check if student is already enrolled in this class
            bool alreadyEnrolled = await studentClassRepository.IsStudentEnrolledInClassAsync(request.StudentId, classroom.Id);
            if (alreadyEnrolled)
            {
                throw new AppException(CustomCode.StudentAlreadyEnrolled);
            }

            // Create new StudentClass record
            var studentClass = new StudentClass
            {
                StudentId = request.StudentId,
                ClassId = classroom.Id,
                EnrolledAt = DateTimeOffset.UtcNow
            };
            await _unitOfWork.GetRepository<StudentClass, Guid>().AddAsync(studentClass);

            try
            {
                await _unitOfWork.CommitAsync();

                // Create response
                var response = new StudentClassResponse
                {
                    StudentId = studentClass.StudentId,
                    ClassId = classroom.Id,
                    ClassName = classroom.Name,
                    TeacherName = classroom.Teacher?.FullName ?? string.Empty,
                    SchoolName = classroom.School?.Name ?? string.Empty,
                    ClassCode = classroom.ClassCode ?? string.Empty,
                    EnrolledAt = studentClass.EnrolledAt,
                    ClassStatus = classroom.Status
                };

                return response;
            }
            catch (Exception)
            {
                throw new AppException(CustomCode.EnrollmentFailed);
            }
        }
    }
}