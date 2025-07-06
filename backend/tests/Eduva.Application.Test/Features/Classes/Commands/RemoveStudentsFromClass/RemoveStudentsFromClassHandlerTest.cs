using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Classes.Commands.RemoveStudentsFromClass;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Shared.Enums;
using MediatR;
using Moq;

namespace Eduva.Application.Test.Features.Classes.Commands.RemoveStudentsFromClass
{
    [TestFixture]
    public class RemoveStudentsFromClassHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IGenericRepository<Classroom, Guid>> _classRepoMock = null!;
        private Mock<IGenericRepository<StudentClass, Guid>> _studentClassRepoMock = null!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepoMock = null!;
        private RemoveStudentsFromClassHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _classRepoMock = new Mock<IGenericRepository<Classroom, Guid>>();
            _studentClassRepoMock = new Mock<IGenericRepository<StudentClass, Guid>>();
            _userRepoMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();

            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>())
                .Returns(_classRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<StudentClass, Guid>())
                .Returns(_studentClassRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>())
                .Returns(_userRepoMock.Object);

            _handler = new RemoveStudentsFromClassHandler(_unitOfWorkMock.Object);
        }

        [Test]
        public async Task Handle_Should_Remove_Students_When_SystemAdmin()
        {
            // Arrange
            var classId = Guid.NewGuid();
            var studentId1 = Guid.NewGuid();
            var studentId2 = Guid.NewGuid();
            var classroom = new Classroom { Id = classId, SchoolId = 1, TeacherId = Guid.NewGuid() };
            var studentClasses = new List<StudentClass>
            {
                new StudentClass { Id = Guid.NewGuid(), ClassId = classId, StudentId = studentId1 },
                new StudentClass { Id = Guid.NewGuid(), ClassId = classId, StudentId = studentId2 }
            };

            var command = new RemoveStudentsFromClassCommand
            {
                ClassId = classId,
                StudentIds = new List<Guid> { studentId1, studentId2 },
                RequestUserId = Guid.NewGuid(),
                IsSystemAdmin = true
            };

            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _studentClassRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(studentClasses);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            _studentClassRepoMock.Verify(r => r.Remove(It.Is<StudentClass>(sc => sc.StudentId == studentId1)), Times.Once);
            _studentClassRepoMock.Verify(r => r.Remove(It.Is<StudentClass>(sc => sc.StudentId == studentId2)), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
            Assert.That(result, Is.EqualTo(Unit.Value));
        }

        [Test]
        public async Task Handle_Should_Remove_Students_When_Teacher_Owns_Class()
        {
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var classroom = new Classroom { Id = classId, SchoolId = 1, TeacherId = teacherId };
            var studentClasses = new List<StudentClass>
            {
                new StudentClass { Id = Guid.NewGuid(), ClassId = classId, StudentId = studentId }
            };

            var command = new RemoveStudentsFromClassCommand
            {
                ClassId = classId,
                StudentIds = new List<Guid> { studentId },
                RequestUserId = teacherId,
                IsTeacher = true
            };

            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _studentClassRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(studentClasses);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            _studentClassRepoMock.Verify(r => r.Remove(It.Is<StudentClass>(sc => sc.StudentId == studentId)), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
            Assert.That(result, Is.EqualTo(Unit.Value));
        }

        [Test]
        public async Task Handle_Should_Remove_Students_When_SchoolAdmin_SameSchool()
        {
            var classId = Guid.NewGuid();
            var schoolId = 5;
            var adminId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var classroom = new Classroom { Id = classId, SchoolId = schoolId, TeacherId = Guid.NewGuid() };
            var admin = new ApplicationUser { Id = adminId, SchoolId = schoolId };
            var studentClasses = new List<StudentClass>
            {
                new StudentClass { Id = Guid.NewGuid(), ClassId = classId, StudentId = studentId }
            };

            var command = new RemoveStudentsFromClassCommand
            {
                ClassId = classId,
                StudentIds = new List<Guid> { studentId },
                RequestUserId = adminId,
                IsSchoolAdmin = true
            };

            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(adminId)).ReturnsAsync(admin);
            _studentClassRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(studentClasses);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            _studentClassRepoMock.Verify(r => r.Remove(It.Is<StudentClass>(sc => sc.StudentId == studentId)), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
            Assert.That(result, Is.EqualTo(Unit.Value));
        }

        [Test]
        public void Handle_Should_Throw_When_Class_Not_Found()
        {
            var command = new RemoveStudentsFromClassCommand
            {
                ClassId = Guid.NewGuid(),
                StudentIds = new List<Guid> { Guid.NewGuid() },
                RequestUserId = Guid.NewGuid(),
                IsSystemAdmin = true
            };

            _classRepoMock.Setup(r => r.GetByIdAsync(command.ClassId)).ReturnsAsync((Classroom?)null);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ClassNotFound));
        }

        [Test]
        public void Handle_Should_Throw_When_User_Not_Authorized()
        {
            var classId = Guid.NewGuid();
            var classroom = new Classroom { Id = classId, SchoolId = 1, TeacherId = Guid.NewGuid() };
            var command = new RemoveStudentsFromClassCommand
            {
                ClassId = classId,
                StudentIds = new List<Guid> { Guid.NewGuid() },
                RequestUserId = Guid.NewGuid(),
                IsTeacher = true // nhưng không phải teacher của lớp
            };

            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.Unauthorized));
        }

        [Test]
        public void Handle_Should_Throw_When_Student_Not_Found_In_Class()
        {
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var command = new RemoveStudentsFromClassCommand
            {
                ClassId = classId,
                StudentIds = new List<Guid> { Guid.NewGuid() },
                RequestUserId = teacherId,
                IsTeacher = true
            };
            var classroom = new Classroom { Id = classId, SchoolId = 1, TeacherId = teacherId };

            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _studentClassRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<StudentClass>());

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.StudentNotFoundInClass));
        }

        [Test]
        public void Handle_Should_Throw_When_SchoolAdmin_Different_School()
        {
            var classId = Guid.NewGuid();
            var schoolId = 1;
            var adminId = Guid.NewGuid();
            var classroom = new Classroom { Id = classId, SchoolId = schoolId, TeacherId = Guid.NewGuid() };
            var admin = new ApplicationUser { Id = adminId, SchoolId = 99 }; // khác trường
            var command = new RemoveStudentsFromClassCommand
            {
                ClassId = classId,
                StudentIds = new List<Guid> { Guid.NewGuid() },
                RequestUserId = adminId,
                IsSchoolAdmin = true
            };

            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(adminId)).ReturnsAsync(admin);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.Unauthorized));
        }

        [Test]
        public void Handle_Should_Throw_When_SchoolAdmin_User_Not_Found()
        {
            var classId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var classroom = new Classroom { Id = classId, SchoolId = 1, TeacherId = Guid.NewGuid() };
            var command = new RemoveStudentsFromClassCommand
            {
                ClassId = classId,
                StudentIds = new List<Guid> { Guid.NewGuid() },
                RequestUserId = adminId,
                IsSchoolAdmin = true
            };

            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(adminId)).ReturnsAsync((ApplicationUser?)null);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.Unauthorized));
        }
    }
}