using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Classes.Queries.GetClassById;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;
using System.Linq.Expressions;

namespace Eduva.Application.Test.Features.Classes.Queries.GetClassById
{
    [TestFixture]
    public class GetClassByIdHandlerTests
    {
        #region Setup
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private Mock<IClassroomRepository> _classroomRepoMock = null!;
        private Mock<IUserRepository> _userRepoMock = null!;
        private Mock<ISchoolRepository> _schoolRepoMock = null!;
        private Mock<ILessonMaterialRepository> _lessonMaterialRepoMock = null!;
        private Mock<IGenericRepository<Folder, Guid>> _folderRepoMock = null!;
        private Mock<IGenericRepository<StudentClass, Guid>> _studentClassRepoMock = null!;
        private GetClassByIdHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object,
                null!, null!, null!, null!, null!, null!, null!, null!);

            // Setup repositories
            _classroomRepoMock = new Mock<IClassroomRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _schoolRepoMock = new Mock<ISchoolRepository>();
            _lessonMaterialRepoMock = new Mock<ILessonMaterialRepository>();
            _folderRepoMock = new Mock<IGenericRepository<Folder, Guid>>();
            _studentClassRepoMock = new Mock<IGenericRepository<StudentClass, Guid>>();

            // Setup UnitOfWork to return our mocked repositories
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IClassroomRepository>())
                .Returns(_classroomRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IUserRepository>())
                .Returns(_userRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<ISchoolRepository>())
                .Returns(_schoolRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<ILessonMaterialRepository>())
                .Returns(_lessonMaterialRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<Folder, Guid>())
                .Returns(_folderRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<StudentClass, Guid>())
                .Returns(_studentClassRepoMock.Object);

            // Create handler with mocked dependencies
            _handler = new GetClassByIdHandler(_unitOfWorkMock.Object, _userManagerMock.Object);
        }
        #endregion

        #region Tests
        [Test]
        public async Task Handle_ShouldReturnClassResponse_WhenClassExists_AndUserIsTeacher()
        {
            // Arrange
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var schoolId = 1;

            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = teacherId,
                SchoolId = schoolId,
                Name = "Test Class",
                ClassCode = "TC123"
            };

            var teacher = new ApplicationUser
            {
                Id = teacherId,
                FullName = "Test Teacher",
                AvatarUrl = "teacher-avatar.jpg"
            };

            var school = new School
            {
                Id = schoolId,
                Name = "Test School"
            };

            // Setup repository mocks
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId))
                .ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(teacherId))
                .ReturnsAsync(teacher);
            _schoolRepoMock.Setup(r => r.GetByIdAsync(schoolId))
                .ReturnsAsync(school);

            // Setup folder repository
            var folders = new List<Folder>
            {
                new Folder { Id = Guid.NewGuid(), ClassId = classId, OwnerType = OwnerType.Class },
                new Folder { Id = Guid.NewGuid(), ClassId = classId, OwnerType = OwnerType.Class }
            };
            _folderRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(folders);

            var folderIds = folders.Select(f => f.Id).ToList();
            _lessonMaterialRepoMock.Setup(r => r.CountApprovedMaterialsInFoldersAsync(
                It.Is<List<Guid>>(ids => ids.Count == folderIds.Count && ids.All(id => folderIds.Contains(id))),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(5);

            // Act
            var result = await _handler.Handle(new GetClassByIdQuery(classId, teacherId), CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(classId));
            Assert.That(result.Name, Is.EqualTo("Test Class"));
            Assert.That(result.ClassCode, Is.EqualTo("TC123"));
            Assert.That(result.TeacherId, Is.EqualTo(teacherId));
            Assert.That(result.TeacherName, Is.EqualTo("Test Teacher"));
            Assert.That(result.SchoolName, Is.EqualTo("Test School"));
            Assert.That(result.TeacherAvatarUrl, Is.EqualTo("teacher-avatar.jpg"));
            Assert.That(result.CountLessonMaterial, Is.EqualTo(5));
        }

        [Test]
        public async Task Handle_ShouldReturnClassResponse_WhenClassExists_AndUserIsStudent_EnrolledInClass()
        {
            // Arrange
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var schoolId = 1;

            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = teacherId,
                SchoolId = schoolId,
                Name = "Test Class",
                ClassCode = "TC123"
            };

            var teacher = new ApplicationUser
            {
                Id = teacherId,
                FullName = "Test Teacher",
                AvatarUrl = "teacher-avatar.jpg"
            };

            var student = new ApplicationUser
            {
                Id = studentId,
                FullName = "Test Student"
            };

            var school = new School
            {
                Id = schoolId,
                Name = "Test School"
            };

            // Setup repository mocks
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId))
                .ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(teacherId))
                .ReturnsAsync(teacher);
            _schoolRepoMock.Setup(r => r.GetByIdAsync(schoolId))
                .ReturnsAsync(school);

            // Setup UserManager mock
            _userManagerMock.Setup(um => um.FindByIdAsync(studentId.ToString()))
                .ReturnsAsync(student);
            _userManagerMock.Setup(um => um.GetRolesAsync(student))
                .ReturnsAsync(new List<string> { nameof(Role.Student) });

            // Setup student class enrollment
            _studentClassRepoMock.Setup(r => r.ExistsAsync(
                It.IsAny<Expression<Func<StudentClass, bool>>>()))
                .ReturnsAsync(true);

            // Setup folder repository
            var folders = new List<Folder>
            {
                new Folder { Id = Guid.NewGuid(), ClassId = classId, OwnerType = OwnerType.Class }
            };
            _folderRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(folders);

            var folderIds = folders.Select(f => f.Id).ToList();
            _lessonMaterialRepoMock.Setup(r => r.CountApprovedMaterialsInFoldersAsync(
                It.Is<List<Guid>>(ids => ids.Count == folderIds.Count && ids.All(id => folderIds.Contains(id))),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(3);

            // Act
            var result = await _handler.Handle(new GetClassByIdQuery(classId, studentId), CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(classId));
            Assert.That(result.CountLessonMaterial, Is.EqualTo(3));
        }

        [Test]
        public async Task Handle_ShouldReturnClassResponse_WhenClassExists_AndUserIsSystemAdmin()
        {
            // Arrange
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var schoolId = 1;

            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = teacherId,
                SchoolId = schoolId
            };

            var teacher = new ApplicationUser
            {
                Id = teacherId,
                FullName = "Test Teacher"
            };

            var admin = new ApplicationUser
            {
                Id = adminId,
                FullName = "System Admin"
            };

            var school = new School
            {
                Id = schoolId,
                Name = "Test School"
            };

            // Setup repository mocks
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId))
                .ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(teacherId))
                .ReturnsAsync(teacher);
            _schoolRepoMock.Setup(r => r.GetByIdAsync(schoolId))
                .ReturnsAsync(school);

            // Setup UserManager mock
            _userManagerMock.Setup(um => um.FindByIdAsync(adminId.ToString()))
                .ReturnsAsync(admin);
            _userManagerMock.Setup(um => um.GetRolesAsync(admin))
                .ReturnsAsync(new List<string> { nameof(Role.SystemAdmin) });

            // Setup empty folders
            _folderRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Folder>());

            // Act
            var result = await _handler.Handle(new GetClassByIdQuery(classId, adminId), CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(classId));
            Assert.That(result.CountLessonMaterial, Is.EqualTo(0));
        }

        [Test]
        public void Handle_ShouldThrowUnauthorized_WhenUserDoesNotHaveAccess()
        {
            // Arrange
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var unauthorizedId = Guid.NewGuid();
            var schoolId = 1;

            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = teacherId,
                SchoolId = schoolId
            };

            var unauthorizedUser = new ApplicationUser
            {
                Id = unauthorizedId,
                SchoolId = 2 // Different school
            };

            // Setup repository mocks
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId))
                .ReturnsAsync(classroom);

            // Setup UserManager mock
            _userManagerMock.Setup(um => um.FindByIdAsync(unauthorizedId.ToString()))
                .ReturnsAsync(unauthorizedUser);
            _userManagerMock.Setup(um => um.GetRolesAsync(unauthorizedUser))
                .ReturnsAsync(new List<string> { nameof(Role.SchoolAdmin) });

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(new GetClassByIdQuery(classId, unauthorizedId), CancellationToken.None));

            Assert.That(exception.Message, Does.Contain("Unauthorized"));
        }

        [Test]
        public void Handle_ShouldThrowClassNotFound_WhenClassDoesNotExist()
        {
            // Arrange
            var classId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            // Setup repository mock to return null
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId))
                .ReturnsAsync((Classroom?)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(new GetClassByIdQuery(classId, userId), CancellationToken.None));

            // Thay đổi dòng này từ kiểm tra CustomCode sang kiểm tra Message
            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.ClassNotFound));
        }
        #endregion
    }
}