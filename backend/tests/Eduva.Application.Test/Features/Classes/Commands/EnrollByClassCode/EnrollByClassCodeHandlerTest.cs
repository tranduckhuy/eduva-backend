using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Classes.Commands.EnrollByClassCode;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.Classes.Commands.EnrollByClassCode
{
    [TestFixture]
    public class EnrollByClassCodeHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepoMock = null!;
        private Mock<IClassroomRepository> _classroomRepoMock = null!;
        private Mock<IStudentClassRepository> _studentClassRepoMock = null!;
        private Mock<IGenericRepository<StudentClass, Guid>> _studentClassGenericRepoMock = null!;
        private EnrollByClassCodeHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
            _userRepoMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            _classroomRepoMock = new Mock<IClassroomRepository>();
            _studentClassRepoMock = new Mock<IStudentClassRepository>();
            _studentClassGenericRepoMock = new Mock<IGenericRepository<StudentClass, Guid>>();

            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>())
                .Returns(_userRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IClassroomRepository>())
                .Returns(_classroomRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IStudentClassRepository>())
                .Returns(_studentClassRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<StudentClass, Guid>())
                .Returns(_studentClassGenericRepoMock.Object);

            _handler = new EnrollByClassCodeHandler(_unitOfWorkMock.Object, _userManagerMock.Object);
        }

        [Test]
        public async Task Handle_Should_Enroll_Student_When_Valid()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var schoolId = 1;
            var command = new EnrollByClassCodeCommand { StudentId = studentId, ClassCode = "ABC123" };

            // Setup student
            var student = new ApplicationUser
            {
                Id = studentId,
                SchoolId = schoolId,
                FullName = "Student Name",
                Email = "student@example.com"
            };

            // Setup classroom with all required properties
            var classroom = new Classroom
            {
                Id = classId,
                ClassCode = "ABC123",
                SchoolId = schoolId,
                Status = EntityStatus.Active,
                Name = "Test Class",
                TeacherId = Guid.NewGuid(),
                Teacher = new ApplicationUser { FullName = "Teacher Name" },
                School = new School { Id = schoolId, Name = "Test School" }
            };

            // Setup user repository mock
            var userRepoMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            userRepoMock.Setup(r => r.GetByIdAsync(studentId)).ReturnsAsync(student);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>()).Returns(userRepoMock.Object);

            // Setup role check
            _userManagerMock.Setup(m => m.GetRolesAsync(student)).ReturnsAsync(new List<string> { nameof(Role.Student) });

            // Setup classroom repository - THIS IS KEY TO FIXING THE NULL REFERENCE
            var classroomRepoMock = new Mock<IGenericRepository<Classroom, Guid>>();
            classroomRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Classroom> { classroom });
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>()).Returns(classroomRepoMock.Object);

            // Setup student class repository
            var studentClassRepoMock = new Mock<IStudentClassRepository>();
            studentClassRepoMock.Setup(r => r.IsStudentEnrolledInClassAsync(studentId, classId)).ReturnsAsync(false);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IStudentClassRepository>()).Returns(studentClassRepoMock.Object);

            // Setup student class generic repository for AddAsync
            var studentClassGenericRepoMock = new Mock<IGenericRepository<StudentClass, Guid>>();
            studentClassGenericRepoMock.Setup(r => r.AddAsync(It.IsAny<StudentClass>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.GetRepository<StudentClass, Guid>()).Returns(studentClassGenericRepoMock.Object);

            // Setup unit of work commit
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.StudentId, Is.EqualTo(studentId));
                Assert.That(result.ClassId, Is.EqualTo(classId));
                Assert.That(result.ClassName, Is.EqualTo("Test Class"));
                Assert.That(result.TeacherName, Is.EqualTo("Teacher Name"));
                Assert.That(result.SchoolName, Is.EqualTo("Test School"));
                Assert.That(result.ClassCode, Is.EqualTo("ABC123"));
                Assert.That(result.ClassStatus, Is.EqualTo(EntityStatus.Active));
            });

            // Verify StudentClass was created with correct values
            studentClassGenericRepoMock.Verify(r => r.AddAsync(It.Is<StudentClass>(sc =>
                sc.StudentId == studentId &&
                sc.ClassId == classId)),
                Times.Once);

            // Verify commit was called
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Test]
        public void Handle_Should_Throw_When_Student_Not_Exist()
        {
            var command = new EnrollByClassCodeCommand { StudentId = Guid.NewGuid(), ClassCode = "ABC123" };
            _userRepoMock.Setup(r => r.GetByIdAsync(command.StudentId)).ReturnsAsync((ApplicationUser?)null);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.UserNotExists));
        }

        [Test]
        public void Handle_Should_Throw_When_User_Not_Student()
        {
            var studentId = Guid.NewGuid();
            var command = new EnrollByClassCodeCommand { StudentId = studentId, ClassCode = "ABC123" };
            var student = new ApplicationUser { Id = studentId };

            _userRepoMock.Setup(r => r.GetByIdAsync(studentId)).ReturnsAsync(student);
            _userManagerMock.Setup(m => m.GetRolesAsync(student)).ReturnsAsync(new List<string> { nameof(Role.Teacher) });

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.UserNotStudent));
        }

        [Test]
        public void Handle_Should_Throw_When_Class_Not_Found()
        {
            var studentId = Guid.NewGuid();
            var command = new EnrollByClassCodeCommand { StudentId = studentId, ClassCode = "ABC123" };
            var student = new ApplicationUser { Id = studentId, SchoolId = 1 };

            _userRepoMock.Setup(r => r.GetByIdAsync(studentId)).ReturnsAsync(student);
            _userManagerMock.Setup(m => m.GetRolesAsync(student)).ReturnsAsync(new List<string> { nameof(Role.Student) });

            var classroomRepoMock = new Mock<IGenericRepository<Classroom, Guid>>();
            classroomRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Classroom>());
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>()).Returns(classroomRepoMock.Object);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ClassNotFound));
        }

        [Test]
        public void Handle_Should_Throw_When_Class_Not_Active()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var schoolId = 1;
            var command = new EnrollByClassCodeCommand { StudentId = studentId, ClassCode = "ABC123" };

            var student = new ApplicationUser
            {
                Id = studentId,
                SchoolId = schoolId
            };

            var classroom = new Classroom
            {
                Id = classId,
                ClassCode = "ABC123",
                SchoolId = schoolId,
                Status = EntityStatus.Inactive,
                Teacher = new ApplicationUser { FullName = "Teacher Name" },
                School = new School { Name = "School Name" }
            };

            var userRepoMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            userRepoMock.Setup(r => r.GetByIdAsync(studentId)).ReturnsAsync(student);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>()).Returns(userRepoMock.Object);

            _userManagerMock.Setup(m => m.GetRolesAsync(student)).ReturnsAsync(new List<string> { nameof(Role.Student) });

            var classroomRepoMock = new Mock<IGenericRepository<Classroom, Guid>>();
            classroomRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Classroom> { classroom });
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>()).Returns(classroomRepoMock.Object);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ClassNotActive));
        }

        [Test]
        public void Handle_Should_Throw_When_Already_Enrolled()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var schoolId = 1;
            var command = new EnrollByClassCodeCommand { StudentId = studentId, ClassCode = "ABC123" };

            // Setup student
            var student = new ApplicationUser { Id = studentId, SchoolId = schoolId };

            // Setup classroom with proper properties
            var classroom = new Classroom
            {
                Id = classId,
                ClassCode = "ABC123",
                SchoolId = schoolId,
                Status = EntityStatus.Active,
                Name = "Test Class",
                Teacher = new ApplicationUser { FullName = "Teacher Name" },
                School = new School { Name = "Test School" }
            };

            // Setup user repository
            var userRepoMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            userRepoMock.Setup(r => r.GetByIdAsync(studentId)).ReturnsAsync(student);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>()).Returns(userRepoMock.Object);

            // Setup role check
            _userManagerMock.Setup(m => m.GetRolesAsync(student)).ReturnsAsync(new List<string> { nameof(Role.Student) });

            // Setup classroom repository - Fix for NullReferenceException
            var classroomRepoMock = new Mock<IGenericRepository<Classroom, Guid>>();
            classroomRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Classroom> { classroom });
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>()).Returns(classroomRepoMock.Object);

            // Setup student class repository to return "already enrolled" = true
            var studentClassRepoMock = new Mock<IStudentClassRepository>();
            studentClassRepoMock.Setup(r => r.IsStudentEnrolledInClassAsync(studentId, classId)).ReturnsAsync(true);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IStudentClassRepository>()).Returns(studentClassRepoMock.Object);

            // Act & Assert - Now should throw AppException with StudentAlreadyEnrolled code
            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.StudentAlreadyEnrolled));
        }

        [Test]
        public void Handle_Should_Throw_When_Enroll_Different_School()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var studentSchoolId = 1;
            var classSchoolId = 2;
            var classId = Guid.NewGuid();
            var command = new EnrollByClassCodeCommand { StudentId = studentId, ClassCode = "ABC123" };

            var student = new ApplicationUser { Id = studentId, SchoolId = studentSchoolId };

            var classroom = new Classroom
            {
                Id = classId,
                ClassCode = "ABC123",
                SchoolId = classSchoolId,
                Status = EntityStatus.Active,
                Teacher = new ApplicationUser { FullName = "Teacher Name" },
                School = new School { Name = "Different School" }
            };

            _userRepoMock.Setup(r => r.GetByIdAsync(studentId)).ReturnsAsync(student);
            _userManagerMock.Setup(m => m.GetRolesAsync(student)).ReturnsAsync(new List<string> { nameof(Role.Student) });

            var classroomRepoGenericMock = new Mock<IGenericRepository<Classroom, Guid>>();
            classroomRepoGenericMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Classroom> { classroom });
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>()).Returns(classroomRepoGenericMock.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.StudentCannotEnrollDifferentSchool));
        }

        [Test]
        public void Handle_Should_Throw_When_Commit_Fails()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var schoolId = 1;
            var command = new EnrollByClassCodeCommand { StudentId = studentId, ClassCode = "ABC123" };

            // Setup student
            var student = new ApplicationUser { Id = studentId, SchoolId = schoolId };

            var classroom = new Classroom
            {
                Id = classId,
                ClassCode = "ABC123",
                SchoolId = schoolId,
                Status = EntityStatus.Active,
                Name = "Test Class",
                Teacher = new ApplicationUser { FullName = "Teacher Name" },
                School = new School { Name = "School Name" }
            };

            var userRepoMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            userRepoMock.Setup(r => r.GetByIdAsync(studentId)).ReturnsAsync(student);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>()).Returns(userRepoMock.Object);

            _userManagerMock.Setup(m => m.GetRolesAsync(student)).ReturnsAsync(new List<string> { nameof(Role.Student) });

            var classroomRepoMock = new Mock<IGenericRepository<Classroom, Guid>>();
            classroomRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Classroom> { classroom });
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>()).Returns(classroomRepoMock.Object);

            var studentClassRepoMock = new Mock<IStudentClassRepository>();
            studentClassRepoMock.Setup(r => r.IsStudentEnrolledInClassAsync(studentId, classId)).ReturnsAsync(false);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IStudentClassRepository>()).Returns(studentClassRepoMock.Object);

            var studentClassGenericRepoMock = new Mock<IGenericRepository<StudentClass, Guid>>();
            studentClassGenericRepoMock.Setup(r => r.AddAsync(It.IsAny<StudentClass>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.GetRepository<StudentClass, Guid>()).Returns(studentClassGenericRepoMock.Object);

            _unitOfWorkMock.Setup(u => u.CommitAsync()).ThrowsAsync(new Exception("Database error"));

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.EnrollmentFailed));
        }

        [Test]
        public void Handle_Should_Throw_When_Student_Has_No_School()
        {
            var studentId = Guid.NewGuid();
            var command = new EnrollByClassCodeCommand { StudentId = studentId, ClassCode = "ABC123" };

            var student = new ApplicationUser { Id = studentId, SchoolId = null };

            _userRepoMock.Setup(r => r.GetByIdAsync(studentId)).ReturnsAsync(student);
            _userManagerMock.Setup(m => m.GetRolesAsync(student)).ReturnsAsync(new List<string> { nameof(Role.Student) });

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.SchoolNotFound));
        }

        [Test]
        public void Handle_Should_Throw_When_Duplicate_ClassCode_Same_School()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var classId1 = Guid.NewGuid();
            var classId2 = Guid.NewGuid();
            var schoolId = 1;
            var command = new EnrollByClassCodeCommand { StudentId = studentId, ClassCode = "DUPLICATE" };

            var student = new ApplicationUser { Id = studentId, SchoolId = schoolId };

            var classroom1 = new Classroom
            {
                Id = classId1,
                ClassCode = "DUPLICATE",
                SchoolId = schoolId,
                Status = EntityStatus.Active
            };
            var classroom2 = new Classroom
            {
                Id = classId2,
                ClassCode = "DUPLICATE",
                SchoolId = schoolId,
                Status = EntityStatus.Active
            };

            _userRepoMock.Setup(r => r.GetByIdAsync(studentId)).ReturnsAsync(student);
            _userManagerMock.Setup(m => m.GetRolesAsync(student)).ReturnsAsync(new List<string> { nameof(Role.Student) });

            var classroomRepoMock = new Mock<IGenericRepository<Classroom, Guid>>();
            classroomRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Classroom> { classroom1, classroom2 });
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>()).Returns(classroomRepoMock.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.DuplicateClassCodeSameSchool));
        }

        [Test]
        public void Handle_Should_Throw_When_Student_SchoolId_Different_From_Classroom()
        {
            var studentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var studentSchoolId = 1;
            var classroomSchoolId = 2;
            var command = new EnrollByClassCodeCommand { StudentId = studentId, ClassCode = "ABC123" };

            var student = new ApplicationUser { Id = studentId, SchoolId = studentSchoolId };

            var classroom = new Classroom
            {
                Id = classId,
                ClassCode = "ABC123",
                SchoolId = classroomSchoolId,
                Status = EntityStatus.Active,
                Teacher = new ApplicationUser { FullName = "Teacher Name" },
                School = new School { Name = "Other School" }
            };

            _userRepoMock.Setup(r => r.GetByIdAsync(studentId)).ReturnsAsync(student);
            _userManagerMock.Setup(m => m.GetRolesAsync(student)).ReturnsAsync(new List<string> { nameof(Role.Student) });

            var classroomRepoMock = new Mock<IGenericRepository<Classroom, Guid>>();
            classroomRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Classroom> { classroom });
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>()).Returns(classroomRepoMock.Object);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.StudentCannotEnrollDifferentSchool));
        }
    }
}