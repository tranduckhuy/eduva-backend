using AutoMapper;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Classes.Queries.GetAllStudentsInClass;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Features.Classes.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.Classes.Queries.GetAllStudentsInClass
{
    [TestFixture]
    public class GetAllStudentsInClassQueryHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IMapper> _mapperMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private Mock<IGenericRepository<Classroom, Guid>> _classroomRepoMock = null!;
        private Mock<IGenericRepository<StudentClass, Guid>> _studentClassRepoMock = null!;
        private GetAllStudentsInClassQueryHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object,
                null!, null!, null!, null!, null!, null!, null!, null!);

            _classroomRepoMock = new Mock<IGenericRepository<Classroom, Guid>>();
            _studentClassRepoMock = new Mock<IGenericRepository<StudentClass, Guid>>();

            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>())
                .Returns(_classroomRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<StudentClass, Guid>())
                .Returns(_studentClassRepoMock.Object);

            _handler = new GetAllStudentsInClassQueryHandler(
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _userManagerMock.Object);
        }

        [Test]
        public async Task Handle_ShouldReturnPagination_WhenUserIsSystemAdmin()
        {
            // Arrange
            var classId = Guid.NewGuid();
            var sysAdminId = Guid.NewGuid();
            var classroom = new Classroom { Id = classId, SchoolId = 1, TeacherId = Guid.NewGuid() };
            var sysAdmin = new ApplicationUser { Id = sysAdminId, FullName = "SysAdmin" };

            var studentClass = new StudentClass { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), ClassId = classId, Student = new ApplicationUser { FullName = "Student 1" } };
            var studentClassList = new List<StudentClass> { studentClass };
            var pagination = new Pagination<StudentClass>(1, 10, 1, studentClassList);

            var specParam = new StudentClassSpecParam { PageIndex = 1, PageSize = 10 };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userManagerMock.Setup(m => m.FindByIdAsync(sysAdminId.ToString())).ReturnsAsync(sysAdmin);
            _userManagerMock.Setup(m => m.GetRolesAsync(sysAdmin)).ReturnsAsync(new List<string> { nameof(Role.SystemAdmin) });

            _studentClassRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<StudentClassSpecification>())).ReturnsAsync(pagination);

            _mapperMock.Setup(m => m.Map<IReadOnlyList<StudentClassResponse>>(studentClassList))
                .Returns(studentClassList.Select(sc => new StudentClassResponse { Id = sc.Id, StudentId = sc.StudentId }).ToList());

            var query = new GetAllStudentsInClassQuery(classId, specParam, sysAdminId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data.Count, Is.EqualTo(1));
            Assert.That(result.Data.First().StudentName, Is.EqualTo("Student 1"));
        }

        [Test]
        public async Task Handle_ShouldReturnPagination_WhenUserIsSchoolAdminAndSameSchool()
        {
            // Arrange
            var classId = Guid.NewGuid();
            var schoolAdminId = Guid.NewGuid();
            var schoolId = 77;
            var classroom = new Classroom { Id = classId, SchoolId = schoolId, TeacherId = Guid.NewGuid() };
            var schoolAdmin = new ApplicationUser { Id = schoolAdminId, SchoolId = schoolId };

            var studentClass = new StudentClass { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), ClassId = classId, Student = new ApplicationUser { FullName = "S2" } };
            var studentClassList = new List<StudentClass> { studentClass };
            var pagination = new Pagination<StudentClass>(1, 10, 1, studentClassList);

            var specParam = new StudentClassSpecParam { PageIndex = 1, PageSize = 10 };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userManagerMock.Setup(m => m.FindByIdAsync(schoolAdminId.ToString())).ReturnsAsync(schoolAdmin);
            _userManagerMock.Setup(m => m.GetRolesAsync(schoolAdmin)).ReturnsAsync(new List<string> { nameof(Role.SchoolAdmin) });

            _studentClassRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<StudentClassSpecification>())).ReturnsAsync(pagination);

            _mapperMock.Setup(m => m.Map<IReadOnlyList<StudentClassResponse>>(studentClassList))
                .Returns(studentClassList.Select(sc => new StudentClassResponse { Id = sc.Id, StudentId = sc.StudentId }).ToList());

            var query = new GetAllStudentsInClassQuery(classId, specParam, schoolAdminId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result.Data, Has.Count.EqualTo(1));
            Assert.That(result.Data.First().StudentName, Is.EqualTo("S2"));
        }

        [Test]
        public async Task Handle_ShouldReturnPagination_WhenUserIsTeacherOfClass()
        {
            // Arrange
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var classroom = new Classroom { Id = classId, SchoolId = 2, TeacherId = teacherId };
            var teacher = new ApplicationUser { Id = teacherId, SchoolId = 2 };

            var studentClass = new StudentClass { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), ClassId = classId, Student = new ApplicationUser { FullName = "TStudent" } };
            var studentClassList = new List<StudentClass> { studentClass };
            var pagination = new Pagination<StudentClass>(1, 10, 1, studentClassList);

            var specParam = new StudentClassSpecParam { PageIndex = 1, PageSize = 10 };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userManagerMock.Setup(m => m.FindByIdAsync(teacherId.ToString())).ReturnsAsync(teacher);
            _userManagerMock.Setup(m => m.GetRolesAsync(teacher)).ReturnsAsync(new List<string> { nameof(Role.Teacher) });

            _studentClassRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<StudentClassSpecification>())).ReturnsAsync(pagination);

            _mapperMock.Setup(m => m.Map<IReadOnlyList<StudentClassResponse>>(studentClassList))
                .Returns(studentClassList.Select(sc => new StudentClassResponse { Id = sc.Id, StudentId = sc.StudentId }).ToList());

            var query = new GetAllStudentsInClassQuery(classId, specParam, teacherId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result.Data, Has.Count.EqualTo(1));
            Assert.That(result.Data.First().StudentName, Is.EqualTo("TStudent"));
        }

        [Test]
        public void Handle_ShouldThrowClassNotFound_WhenClassNotExist()
        {
            var classId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync((Classroom?)null);

            var query = new GetAllStudentsInClassQuery(classId, new StudentClassSpecParam { }, requesterId);

            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.ClassNotFound));
        }

        [Test]
        public void Handle_ShouldThrowUserNotFound_WhenUserNotExist()
        {
            var classId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(new Classroom { Id = classId });

            _userManagerMock.Setup(m => m.FindByIdAsync(requesterId.ToString())).ReturnsAsync((ApplicationUser?)null);

            var query = new GetAllStudentsInClassQuery(classId, new StudentClassSpecParam { }, requesterId);

            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.UserNotFound));
        }

        [Test]
        public void Handle_ShouldThrowForbidden_WhenUserHasNoRole()
        {
            var classId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(new Classroom { Id = classId, SchoolId = 1 });
            var requester = new ApplicationUser { Id = requesterId, SchoolId = 1 };
            _userManagerMock.Setup(m => m.FindByIdAsync(requesterId.ToString())).ReturnsAsync(requester);
            _userManagerMock.Setup(m => m.GetRolesAsync(requester)).ReturnsAsync(new List<string>());

            var query = new GetAllStudentsInClassQuery(classId, new StudentClassSpecParam { }, requesterId);

            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.Forbidden));
        }

        [Test]
        public void Handle_ShouldThrowForbidden_WhenSchoolAdminNotSameSchool()
        {
            var classId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(new Classroom { Id = classId, SchoolId = 10 });
            var requester = new ApplicationUser { Id = requesterId, SchoolId = 999 };
            _userManagerMock.Setup(m => m.FindByIdAsync(requesterId.ToString())).ReturnsAsync(requester);
            _userManagerMock.Setup(m => m.GetRolesAsync(requester)).ReturnsAsync(new List<string> { nameof(Role.SchoolAdmin) });

            var query = new GetAllStudentsInClassQuery(classId, new StudentClassSpecParam { }, requesterId);

            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.Forbidden));
        }

        [Test]
        public void Handle_ShouldThrowForbidden_WhenTeacherNotOwnerOrWrongSchool()
        {
            var classId = Guid.NewGuid();
            var requesterId = Guid.NewGuid();
            var classroom = new Classroom { Id = classId, SchoolId = 1, TeacherId = Guid.NewGuid() };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);

            var requester = new ApplicationUser { Id = requesterId, SchoolId = 999 };
            _userManagerMock.Setup(m => m.FindByIdAsync(requesterId.ToString())).ReturnsAsync(requester);
            _userManagerMock.Setup(m => m.GetRolesAsync(requester)).ReturnsAsync(new List<string> { nameof(Role.Teacher) });

            var query = new GetAllStudentsInClassQuery(classId, new StudentClassSpecParam { }, requesterId);

            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.Forbidden));
        }
        [Test]
        public void Handle_ShouldThrowForbidden_WhenSchoolAdminHasNoSchoolId()
        {
            var classId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var classroom = new Classroom { Id = classId, SchoolId = 10 };

            var admin = new ApplicationUser { Id = adminId, SchoolId = null };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userManagerMock.Setup(m => m.FindByIdAsync(adminId.ToString())).ReturnsAsync(admin);
            _userManagerMock.Setup(m => m.GetRolesAsync(admin)).ReturnsAsync(new List<string> { nameof(Role.SchoolAdmin) });

            var query = new GetAllStudentsInClassQuery(classId, new StudentClassSpecParam { }, adminId);

            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.Forbidden));
        }

        [Test]
        public void Handle_ShouldThrowForbidden_WhenTeacherHasNoSchoolId()
        {
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var classroom = new Classroom { Id = classId, SchoolId = 123, TeacherId = teacherId };
            var teacher = new ApplicationUser { Id = teacherId, SchoolId = null };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userManagerMock.Setup(m => m.FindByIdAsync(teacherId.ToString())).ReturnsAsync(teacher);
            _userManagerMock.Setup(m => m.GetRolesAsync(teacher)).ReturnsAsync(new List<string> { nameof(Role.Teacher) });

            var query = new GetAllStudentsInClassQuery(classId, new StudentClassSpecParam { }, teacherId);

            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.Forbidden));
        }

        [Test]
        public void Handle_ShouldThrowForbidden_WhenSchoolAdmin_ClassroomHasNoSchoolId()
        {
            var classId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var classroom = new Classroom { Id = classId, SchoolId = 0 };
            var admin = new ApplicationUser { Id = adminId, SchoolId = 1 };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userManagerMock.Setup(m => m.FindByIdAsync(adminId.ToString())).ReturnsAsync(admin);
            _userManagerMock.Setup(m => m.GetRolesAsync(admin)).ReturnsAsync(new List<string> { nameof(Role.SchoolAdmin) });

            var query = new GetAllStudentsInClassQuery(classId, new StudentClassSpecParam { }, adminId);

            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.Forbidden));
        }

        [Test]
        public async Task Handle_ShouldSetStudentName_FromStudentEntity()
        {
            var classId = Guid.NewGuid();
            var sysAdminId = Guid.NewGuid();
            var classroom = new Classroom { Id = classId, SchoolId = 1 };
            var sysAdmin = new ApplicationUser { Id = sysAdminId, FullName = "SysAdmin" };

            var student = new ApplicationUser { Id = Guid.NewGuid(), FullName = "Mapped Student" };
            var studentClass = new StudentClass { Id = Guid.NewGuid(), Student = student, StudentId = student.Id };
            var studentClassList = new List<StudentClass> { studentClass };
            var pagination = new Pagination<StudentClass>(1, 10, 1, studentClassList);

            var mapped = new List<StudentClassResponse>
    {
        new StudentClassResponse { Id = studentClass.Id, StudentId = student.Id }
    };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userManagerMock.Setup(m => m.FindByIdAsync(sysAdminId.ToString())).ReturnsAsync(sysAdmin);
            _userManagerMock.Setup(m => m.GetRolesAsync(sysAdmin)).ReturnsAsync(new List<string> { nameof(Role.SystemAdmin) });
            _studentClassRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<StudentClassSpecification>())).ReturnsAsync(pagination);
            _mapperMock.Setup(m => m.Map<IReadOnlyList<StudentClassResponse>>(It.IsAny<IReadOnlyList<StudentClass>>())).Returns(mapped);

            var query = new GetAllStudentsInClassQuery(classId, new StudentClassSpecParam(), sysAdminId);

            var result = await _handler.Handle(query, CancellationToken.None);
            Assert.That(result.Data.ElementAt(0).StudentName, Is.EqualTo("Mapped Student"));
        }

        [Test]
        public async Task Handle_ShouldSetStudentNameToEmpty_WhenStudentFullNameIsNull()
        {
            var classId = Guid.NewGuid();
            var sysAdminId = Guid.NewGuid();
            var classroom = new Classroom { Id = classId, SchoolId = 1 };
            var sysAdmin = new ApplicationUser { Id = sysAdminId, FullName = "SysAdmin" };

            var student = new ApplicationUser { Id = Guid.NewGuid(), FullName = null };
            var studentClass = new StudentClass { Id = Guid.NewGuid(), Student = student, StudentId = student.Id };
            var studentClassList = new List<StudentClass> { studentClass };
            var pagination = new Pagination<StudentClass>(1, 10, 1, studentClassList);

            var mapped = new List<StudentClassResponse>
    {
        new StudentClassResponse { Id = studentClass.Id, StudentId = student.Id }
    };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userManagerMock.Setup(m => m.FindByIdAsync(sysAdminId.ToString())).ReturnsAsync(sysAdmin);
            _userManagerMock.Setup(m => m.GetRolesAsync(sysAdmin)).ReturnsAsync(new List<string> { nameof(Role.SystemAdmin) });
            _studentClassRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<StudentClassSpecification>())).ReturnsAsync(pagination);
            _mapperMock.Setup(m => m.Map<IReadOnlyList<StudentClassResponse>>(It.IsAny<IReadOnlyList<StudentClass>>())).Returns(mapped);

            var query = new GetAllStudentsInClassQuery(classId, new StudentClassSpecParam(), sysAdminId);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result.Data.ElementAt(0).StudentName, Is.EqualTo(string.Empty));
        }
    }
}
