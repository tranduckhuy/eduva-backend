using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Classes.Queries.GetStudentById;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Moq;

namespace Eduva.Application.Test.Features.Classes.Queries.GetStudentById
{
    [TestFixture]
    public class GetStudentByIdHandlerTests
    {
        #region Setup
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IStudentClassRepository> _studentClassRepoMock = null!;
        private Mock<IUserRepository> _userRepoMock = null!;
        private Mock<IClassroomRepository> _classroomRepoMock = null!;
        private Mock<ISchoolRepository> _schoolRepoMock = null!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userGenericRepoMock = null!;
        private GetStudentByIdHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _studentClassRepoMock = new Mock<IStudentClassRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _classroomRepoMock = new Mock<IClassroomRepository>();
            _schoolRepoMock = new Mock<ISchoolRepository>();
            _userGenericRepoMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();

            // Setup repositories
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IStudentClassRepository>())
                .Returns(_studentClassRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IUserRepository>())
                .Returns(_userRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IClassroomRepository>())
                .Returns(_classroomRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<ISchoolRepository>())
                .Returns(_schoolRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>())
                .Returns(_userGenericRepoMock.Object);

            // Create handler with mocked dependencies
            _handler = new GetStudentByIdHandler(_unitOfWorkMock.Object);
        }
        #endregion

        #region Tests
        [Test]
        public async Task Handle_ShouldReturnStudentClassResponse_WhenStudentExists_AndUserIsStudent()
        {
            // Arrange
            var studentClassId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var schoolId = 1;

            var studentClass = new StudentClass
            {
                Id = studentClassId,
                StudentId = studentId,
                ClassId = classId,
                EnrolledAt = DateTime.UtcNow.AddDays(-10)
            };

            var student = new ApplicationUser
            {
                Id = studentId,
                FullName = "Test Student",
                AvatarUrl = "student-avatar.jpg"
            };

            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = teacherId,
                SchoolId = schoolId,
                Name = "Test Class"
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

            var query = new GetStudentByIdQuery(studentClassId, studentId);

            // Setup repository mocks
            _studentClassRepoMock.Setup(r => r.GetStudentClassByIdAsync(studentClassId))
                .ReturnsAsync(studentClass);
            _userRepoMock.Setup(r => r.GetByIdAsync(studentId))
                .ReturnsAsync(student);
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId))
                .ReturnsAsync(classroom);
            _schoolRepoMock.Setup(r => r.GetByIdAsync(schoolId))
                .ReturnsAsync(school);
            _userRepoMock.Setup(r => r.GetByIdAsync(teacherId))
                .ReturnsAsync(teacher);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(studentClassId));
                Assert.That(result.StudentName, Is.EqualTo("Test Student"));
                Assert.That(result.ClassName, Is.EqualTo("Test Class"));
                Assert.That(result.TeacherName, Is.EqualTo("Test Teacher"));
                Assert.That(result.SchoolName, Is.EqualTo("Test School"));
                Assert.That(result.StudentAvatarUrl, Is.EqualTo("student-avatar.jpg"));
                Assert.That(result.TeacherAvatarUrl, Is.EqualTo("teacher-avatar.jpg"));
            });
        }

        [Test]
        public async Task Handle_ShouldReturnStudentClassResponse_WhenStudentExists_AndUserIsTeacher()
        {
            // Arrange
            var studentClassId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var schoolId = 1;

            var studentClass = new StudentClass
            {
                Id = studentClassId,
                StudentId = studentId,
                ClassId = classId,
                Class = new Classroom
                {
                    Id = classId,
                    TeacherId = teacherId
                }
            };

            var student = new ApplicationUser
            {
                Id = studentId,
                FullName = "Test Student"
            };

            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = teacherId,
                SchoolId = schoolId,
                Name = "Test Class"
            };

            var school = new School
            {
                Id = schoolId,
                Name = "Test School"
            };

            var query = new GetStudentByIdQuery(studentClassId, teacherId);

            // Setup repository mocks
            _studentClassRepoMock.Setup(r => r.GetStudentClassByIdAsync(studentClassId))
                .ReturnsAsync(studentClass);
            _userRepoMock.Setup(r => r.GetByIdAsync(studentId))
                .ReturnsAsync(student);
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId))
                .ReturnsAsync(classroom);
            _schoolRepoMock.Setup(r => r.GetByIdAsync(schoolId))
                .ReturnsAsync(school);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(studentClassId));
                Assert.That(result.StudentName, Is.EqualTo("Test Student"));
            });
        }

        [Test]
        public async Task Handle_ShouldReturnStudentClassResponse_WhenStudentExists_AndUserIsSchoolAdmin()
        {
            // Arrange
            var studentClassId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var schoolId = 1;

            var studentClass = new StudentClass
            {
                Id = studentClassId,
                StudentId = studentId,
                ClassId = classId,
                Class = new Classroom
                {
                    Id = classId,
                    SchoolId = schoolId
                }
            };

            var student = new ApplicationUser
            {
                Id = studentId,
                FullName = "Test Student"
            };

            var admin = new ApplicationUser
            {
                Id = adminId,
                FullName = "School Admin",
                SchoolId = schoolId
            };

            var classroom = new Classroom
            {
                Id = classId,
                SchoolId = schoolId,
                Name = "Test Class"
            };

            var school = new School
            {
                Id = schoolId,
                Name = "Test School"
            };

            var query = new GetStudentByIdQuery(studentClassId, adminId);

            // Setup repository mocks
            _studentClassRepoMock.Setup(r => r.GetStudentClassByIdAsync(studentClassId))
                .ReturnsAsync(studentClass);
            _userRepoMock.Setup(r => r.GetByIdAsync(studentId))
                .ReturnsAsync(student);
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId))
                .ReturnsAsync(classroom);
            _schoolRepoMock.Setup(r => r.GetByIdAsync(schoolId))
                .ReturnsAsync(school);
            _userGenericRepoMock.Setup(r => r.GetByIdAsync(adminId))
                .ReturnsAsync(admin);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(studentClassId));
                Assert.That(result.StudentName, Is.EqualTo("Test Student"));
            });
        }

        [Test]
        public void Handle_ShouldThrowException_WhenStudentClassNotFound()
        {
            // Arrange
            var studentClassId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var query = new GetStudentByIdQuery(studentClassId, userId);

            // Setup repository mock to return null
            _studentClassRepoMock.Setup(r => r.GetStudentClassByIdAsync(studentClassId))
                .ReturnsAsync((StudentClass?)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));

            Assert.That(exception.Message, Does.Contain("User ID not found"));
        }

        [Test]
        public void Handle_ShouldThrowException_WhenStudentNotFound()
        {
            // Arrange
            var studentClassId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();

            var studentClass = new StudentClass
            {
                Id = studentClassId,
                StudentId = studentId,
                ClassId = classId
            };

            var query = new GetStudentByIdQuery(studentClassId, userId);

            // Setup repository mocks
            _studentClassRepoMock.Setup(r => r.GetStudentClassByIdAsync(studentClassId))
                .ReturnsAsync(studentClass);
            _userRepoMock.Setup(r => r.GetByIdAsync(studentId))
                .ReturnsAsync((ApplicationUser?)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));

            Assert.That(exception.Message, Does.Contain("User not found"));
        }

        [Test]
        public void Handle_ShouldThrowException_WhenClassNotFound()
        {
            // Arrange
            var studentClassId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();

            var studentClass = new StudentClass
            {
                Id = studentClassId,
                StudentId = studentId,
                ClassId = classId
            };

            var student = new ApplicationUser
            {
                Id = studentId,
                FullName = "Test Student"
            };

            var query = new GetStudentByIdQuery(studentClassId, userId);

            // Setup repository mocks
            _studentClassRepoMock.Setup(r => r.GetStudentClassByIdAsync(studentClassId))
                .ReturnsAsync(studentClass);
            _userRepoMock.Setup(r => r.GetByIdAsync(studentId))
                .ReturnsAsync(student);
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId))
                .ReturnsAsync((Classroom?)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));

            Assert.That(exception.Message, Does.Contain("Class not found"));
        }

        [Test]
        public void Handle_ShouldThrowException_WhenUserDoesNotHaveAccess()
        {
            // Arrange
            var studentClassId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var unauthorizedId = Guid.NewGuid();
            var schoolId = 1;
            var differentSchoolId = 2;

            var studentClass = new StudentClass
            {
                Id = studentClassId,
                StudentId = studentId,
                ClassId = classId,
                Class = new Classroom
                {
                    Id = classId,
                    TeacherId = teacherId,
                    SchoolId = schoolId
                }
            };

            var student = new ApplicationUser
            {
                Id = studentId,
                FullName = "Test Student"
            };

            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = teacherId,
                SchoolId = schoolId,
                Name = "Test Class"
            };

            var unauthorizedUser = new ApplicationUser
            {
                Id = unauthorizedId,
                FullName = "Unauthorized User",
                SchoolId = differentSchoolId
            };

            var school = new School
            {
                Id = schoolId,
                Name = "Test School"
            };

            var query = new GetStudentByIdQuery(studentClassId, unauthorizedId);

            // Setup repository mocks
            _studentClassRepoMock.Setup(r => r.GetStudentClassByIdAsync(studentClassId))
                .ReturnsAsync(studentClass);
            _userRepoMock.Setup(r => r.GetByIdAsync(studentId))
                .ReturnsAsync(student);
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId))
                .ReturnsAsync(classroom);
            _schoolRepoMock.Setup(r => r.GetByIdAsync(schoolId))
                .ReturnsAsync(school);
            _userGenericRepoMock.Setup(r => r.GetByIdAsync(unauthorizedId))
                .ReturnsAsync(unauthorizedUser);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));

            Assert.That(exception.Message, Does.Contain("Unauthorized"));
        }

        [Test]
        public async Task Handle_ShouldHandleNullValues_AndProvideEmptyStrings()
        {
            // Arrange
            var studentClassId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var schoolId = 1;

            var studentClass = new StudentClass
            {
                Id = studentClassId,
                StudentId = studentId,
                ClassId = classId
            };

            var student = new ApplicationUser
            {
                Id = studentId,
                FullName = null,
                AvatarUrl = string.Empty  // Đảm bảo AvatarUrl null
            };

            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = Guid.Empty,
                SchoolId = schoolId,
                Name = "Test Class"
            };

            var query = new GetStudentByIdQuery(studentClassId, studentId);

            // Setup repository mocks
            _studentClassRepoMock.Setup(r => r.GetStudentClassByIdAsync(studentClassId))
                .ReturnsAsync(studentClass);
            _userRepoMock.Setup(r => r.GetByIdAsync(studentId))
                .ReturnsAsync(student);
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId))
                .ReturnsAsync(classroom);
            _schoolRepoMock.Setup(r => r.GetByIdAsync(schoolId))
                .ReturnsAsync((School?)null);
            _userRepoMock.Setup(r => r.GetByIdAsync(Guid.Empty))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.StudentName, Is.EqualTo(string.Empty));
                Assert.That(result.TeacherName, Is.EqualTo(string.Empty));
                Assert.That(result.SchoolName, Is.EqualTo(string.Empty));
                Assert.That(result.StudentAvatarUrl, Is.EqualTo(string.Empty));
                Assert.That(result.TeacherAvatarUrl, Is.Null);
            });
        }
        #endregion
    }
}