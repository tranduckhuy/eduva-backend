using AutoMapper;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Features.Classes.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Classes.Queries.GetAllStudentsInClass
{
    public class GetAllStudentsInClassQueryHandler : IRequestHandler<GetAllStudentsInClassQuery, Pagination<StudentClassResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;

        public GetAllStudentsInClassQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userManager = userManager;
        }

        public async Task<Pagination<StudentClassResponse>> Handle(GetAllStudentsInClassQuery request, CancellationToken cancellationToken)
        {
            // Get the classroom
            var classroom = await _unitOfWork.GetRepository<Classroom, Guid>().GetByIdAsync(request.ClassId)
                ?? throw new AppException(CustomCode.ClassNotFound);

            // Get the requester
            var requester = await _userManager.FindByIdAsync(request.RequesterId.ToString())
                ?? throw new AppException(CustomCode.UserNotFound);

            // Check user roles
            var userRoles = await _userManager.GetRolesAsync(requester);
            bool isSystemAdmin = userRoles.Contains(nameof(Role.SystemAdmin));
            bool isSchoolAdmin = userRoles.Contains(nameof(Role.SchoolAdmin));
            bool isTeacher = userRoles.Contains(nameof(Role.Teacher));

            if (!isSystemAdmin && !isSchoolAdmin && !isTeacher)
            {
                throw new AppException(CustomCode.Forbidden);
            }

            var param = request.StudentClassSpecParam;

            if (isSchoolAdmin && !isSystemAdmin)
            {
                if (requester.SchoolId == null || classroom.SchoolId != requester.SchoolId)
                {
                    throw new AppException(CustomCode.Forbidden);
                }
                param = new StudentClassSpecParam
                {
                    PageIndex = request.StudentClassSpecParam.PageIndex,
                    PageSize = request.StudentClassSpecParam.PageSize,
                    SortBy = request.StudentClassSpecParam.SortBy,
                    SortDirection = request.StudentClassSpecParam.SortDirection,
                    SearchTerm = request.StudentClassSpecParam.SearchTerm,
                    StudentId = request.StudentClassSpecParam.StudentId,
                    ClassName = request.StudentClassSpecParam.ClassName,
                    TeacherName = request.StudentClassSpecParam.TeacherName,
                    SchoolName = request.StudentClassSpecParam.SchoolName,
                    ClassCode = request.StudentClassSpecParam.ClassCode,
                    ClassStatus = request.StudentClassSpecParam.ClassStatus,
                    SchoolId = requester.SchoolId.Value
                };
            }

            if (isTeacher && !isSystemAdmin && !isSchoolAdmin)
            {
                if (classroom.TeacherId != request.RequesterId)
                {
                    throw new AppException(CustomCode.Forbidden);
                }
                if (requester.SchoolId == null || classroom.SchoolId != requester.SchoolId)
                {
                    throw new AppException(CustomCode.Forbidden);
                }
            }

            var spec = new StudentClassSpecification(param, request.ClassId);
            var studentClasses = await _unitOfWork.GetRepository<StudentClass, Guid>().GetWithSpecAsync(spec);

            var mappedStudentClasses = _mapper.Map<IReadOnlyList<StudentClassResponse>>(studentClasses.Data);

            var studentDict = studentClasses.Data.ToDictionary(sc => sc.Id, sc => sc.Student);

            foreach (var studentClassResponse in mappedStudentClasses)
            {
                if (studentDict.TryGetValue(studentClassResponse.Id, out var student) && student != null)
                {
                    studentClassResponse.StudentName = student.FullName ?? string.Empty;
                }
            }

            return new Pagination<StudentClassResponse>(param.PageIndex, param.PageSize, (int)studentClasses.Count, mappedStudentClasses);
        }
    }
}
