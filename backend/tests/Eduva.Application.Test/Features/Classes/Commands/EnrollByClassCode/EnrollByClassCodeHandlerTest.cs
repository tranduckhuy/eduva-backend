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
            var student = new ApplicationUser { Id = studentId, SchoolId = schoolId };
            var classroom = new Classroom
            {
                Id = classId,
                Name = "Math",
                Status = EntityStatus.Active,
                SchoolId = schoolId,
                Teacher = new ApplicationUser { FullName = "Teacher Name" },
                School = new School { Name = "School Name" },
                ClassCode = "ABC123"
            };

            _userRepoMock.Setup(r => r.GetByIdAsync(studentId)).ReturnsAsync(student);
            _userManagerMock.Setup(m => m.GetRolesAsync(student)).ReturnsAsync(new List<string> { nameof(Role.Student) });
            _classroomRepoMock.Setup(r => r.FindByClassCodeAsync("ABC123")).ReturnsAsync(classroom);
            _studentClassRepoMock.Setup(r => r.IsStudentEnrolledInClassAsync(studentId, classId)).ReturnsAsync(false);
            _studentClassRepoMock.Setup(r => r.GetClassesForStudentAsync(studentId)).ReturnsAsync(new List<Classroom>());
            _studentClassGenericRepoMock.Setup(r => r.AddAsync(It.IsAny<StudentClass>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.StudentId, Is.EqualTo(studentId));
                Assert.That(result.ClassId, Is.EqualTo(classId));
                Assert.That(result.ClassName, Is.EqualTo("Math"));
                Assert.That(result.TeacherName, Is.EqualTo("Teacher Name"));
                Assert.That(result.SchoolName, Is.EqualTo("School Name"));
                Assert.That(result.ClassCode, Is.EqualTo("ABC123"));
                Assert.That(result.ClassStatus, Is.EqualTo(EntityStatus.Active));
            });
            _studentClassGenericRepoMock.Verify(r => r.AddAsync(It.IsAny<StudentClass>()), Times.Once);
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
            var student = new ApplicationUser { Id = studentId };

            _userRepoMock.Setup(r => r.GetByIdAsync(studentId)).ReturnsAsync(student);
            _userManagerMock.Setup(m => m.GetRolesAsync(student)).ReturnsAsync(new List<string> { nameof(Role.Student) });
            _classroomRepoMock.Setup(r => r.FindByClassCodeAsync("ABC123")).ReturnsAsync((Classroom?)null);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ClassNotFound));
        }

        [Test]
        public void Handle_Should_Throw_When_Class_Not_Active()
        {
            var studentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var command = new EnrollByClassCodeCommand { StudentId = studentId, ClassCode = "ABC123" };
            var student = new ApplicationUser { Id = studentId };
            var classroom = new Classroom { Id = classId, Status = EntityStatus.Inactive };

            _userRepoMock.Setup(r => r.GetByIdAsync(studentId)).ReturnsAsync(student);
            _userManagerMock.Setup(m => m.GetRolesAsync(student)).ReturnsAsync(new List<string> { nameof(Role.Student) });
            _classroomRepoMock.Setup(r => r.FindByClassCodeAsync("ABC123")).ReturnsAsync(classroom);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ClassNotActive));
        }

        [Test]
        public void Handle_Should_Throw_When_Already_Enrolled()
        {
            var studentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var schoolId = 1;
            var command = new EnrollByClassCodeCommand { StudentId = studentId, ClassCode = "ABC123" };
            var student = new ApplicationUser { Id = studentId, SchoolId = schoolId };
            var classroom = new Classroom { Id = classId, Status = EntityStatus.Active, SchoolId = schoolId };

            _userRepoMock.Setup(r => r.GetByIdAsync(studentId)).ReturnsAsync(student);
            _userManagerMock.Setup(m => m.GetRolesAsync(student)).ReturnsAsync(new List<string> { nameof(Role.Student) });
            _classroomRepoMock.Setup(r => r.FindByClassCodeAsync("ABC123")).ReturnsAsync(classroom);
            _studentClassRepoMock.Setup(r => r.IsStudentEnrolledInClassAsync(studentId, classId)).ReturnsAsync(true);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.StudentAlreadyEnrolled));
        }


        [Test]
        public void Handle_Should_Throw_When_Enroll_Different_School()
        {
            var studentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var command = new EnrollByClassCodeCommand { StudentId = studentId, ClassCode = "ABC123" };
            var student = new ApplicationUser { Id = studentId, SchoolId = 1 };
            var classroom = new Classroom { Id = classId, Status = EntityStatus.Active, SchoolId = 2 };
            var existingClass = new Classroom { Id = Guid.NewGuid(), SchoolId = 1 };

            _userRepoMock.Setup(r => r.GetByIdAsync(studentId)).ReturnsAsync(student);
            _userManagerMock.Setup(m => m.GetRolesAsync(student)).ReturnsAsync(new List<string> { nameof(Role.Student) });
            _classroomRepoMock.Setup(r => r.FindByClassCodeAsync("ABC123")).ReturnsAsync(classroom);
            _studentClassRepoMock.Setup(r => r.IsStudentEnrolledInClassAsync(studentId, classId)).ReturnsAsync(false);
            _studentClassRepoMock.Setup(r => r.GetClassesForStudentAsync(studentId)).ReturnsAsync(new List<Classroom> { existingClass });

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.StudentCannotEnrollDifferentSchool));
        }

        [Test]
        public void Handle_Should_Throw_When_Commit_Fails()
        {
            var studentId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var schoolId = 1;
            var command = new EnrollByClassCodeCommand { StudentId = studentId, ClassCode = "ABC123" };
            var student = new ApplicationUser { Id = studentId, SchoolId = schoolId };
            var classroom = new Classroom
            {
                Id = classId,
                Name = "Math",
                Status = EntityStatus.Active,
                SchoolId = schoolId,
                Teacher = new ApplicationUser { FullName = "Teacher Name" },
                School = new School { Name = "School Name" },
                ClassCode = "ABC123"
            };

            _userRepoMock.Setup(r => r.GetByIdAsync(studentId)).ReturnsAsync(student);
            _userManagerMock.Setup(m => m.GetRolesAsync(student)).ReturnsAsync(new List<string> { nameof(Role.Student) });
            _classroomRepoMock.Setup(r => r.FindByClassCodeAsync("ABC123")).ReturnsAsync(classroom);
            _studentClassRepoMock.Setup(r => r.IsStudentEnrolledInClassAsync(studentId, classId)).ReturnsAsync(false);
            _studentClassRepoMock.Setup(r => r.GetClassesForStudentAsync(studentId)).ReturnsAsync(new List<Classroom>());
            _studentClassGenericRepoMock.Setup(r => r.AddAsync(It.IsAny<StudentClass>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ThrowsAsync(new Exception("DB error"));

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.EnrollmentFailed));
        }
    }
}