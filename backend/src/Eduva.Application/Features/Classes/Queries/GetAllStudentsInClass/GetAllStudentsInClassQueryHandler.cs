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
            var classroom = await _unitOfWork.GetRepository<Classroom, Guid>().GetByIdAsync(request.ClassId);
            if (classroom is null)
            {
                throw new AppException(CustomCode.ClassNotFound);
            }

            var requester = await _userManager.FindByIdAsync(request.RequesterId.ToString());
            if (requester is null)
            {
                throw new AppException(CustomCode.UserNotFound);
            }

            var roles = await _userManager.GetRolesAsync(requester);

            StudentClassSpecParam specParamToUse = request.StudentClassSpecParam;

            if (roles.Contains(Role.SystemAdmin.ToString()))
            {
                // System Admin can see all students in any class
            }
            else if (roles.Contains(Role.SchoolAdmin.ToString()))
            {
                var requesterSchoolId = requester.SchoolId;
                if (requesterSchoolId is null || classroom.SchoolId != requesterSchoolId)
                {
                    throw new AppException(CustomCode.Forbidden);
                }
                // Create a copy with SchoolId set
                specParamToUse = new StudentClassSpecParam
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
                    SchoolId = requesterSchoolId.Value
                };
            }
            else if (roles.Contains(Role.Teacher.ToString()))
            {
                if (classroom.TeacherId != request.RequesterId)
                {
                    throw new AppException(CustomCode.Forbidden);
                }
                // Check if teacher's school matches classroom's school
                if (requester.SchoolId == null || classroom.SchoolId != requester.SchoolId)
                {
                    throw new AppException(CustomCode.Forbidden);
                }
            }
            else
            {
                throw new AppException(CustomCode.Forbidden);
            }

            var spec = new StudentClassSpecification(specParamToUse, request.ClassId);
            var studentClasses = await _unitOfWork.GetRepository<StudentClass, Guid>().GetWithSpecAsync(spec);

            var mappedStudentClasses = _mapper.Map<IReadOnlyList<StudentClassResponse>>(studentClasses.Data);

            // Optimize: Build a dictionary for fast lookup
            var studentDict = studentClasses.Data.ToDictionary(sc => sc.Id, sc => sc.Student);

            foreach (var studentClassResponse in mappedStudentClasses)
            {
                if (studentDict.TryGetValue(studentClassResponse.Id, out var student) && student != null)
                {
                    studentClassResponse.StudentName = student.FullName ?? string.Empty;
                }
            }

            return new Pagination<StudentClassResponse>(specParamToUse.PageIndex, specParamToUse.PageSize, (int)studentClasses.Count, mappedStudentClasses);
        }
    }
}
