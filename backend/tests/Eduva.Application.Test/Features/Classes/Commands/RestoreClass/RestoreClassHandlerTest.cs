using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Classes.Commands.RestoreClass;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.Classes.Commands.RestoreClass
{
    [TestFixture]
    public class RestoreClassHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IClassroomRepository> _classroomRepoMock = null!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepoMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private RestoreClassHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _classroomRepoMock = new Mock<IClassroomRepository>();
            _userRepoMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IClassroomRepository>())
                .Returns(_classroomRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>())
                .Returns(_userRepoMock.Object);

            _handler = new RestoreClassHandler(_unitOfWorkMock.Object, _userManagerMock.Object);
        }

        [Test]
        public async Task Handle_Should_Restore_Class_When_Teacher_Or_Admin()
        {
            // Arrange
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = teacherId,
                Status = EntityStatus.Archived
            };
            var teacher = new ApplicationUser { Id = teacherId };

            var command = new RestoreClassCommand { Id = classId, TeacherId = teacherId };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(teacherId)).ReturnsAsync(teacher);
            _userManagerMock.Setup(m => m.GetRolesAsync(teacher)).ReturnsAsync(new List<string> { nameof(Role.Teacher) });
            _classroomRepoMock.Setup(r => r.Update(It.IsAny<Classroom>()));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(classroom.Status, Is.EqualTo(EntityStatus.Active));
            _classroomRepoMock.Verify(r => r.Update(It.Is<Classroom>(c => c.Id == classId && c.Status == EntityStatus.Active)), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
            Assert.That(result, Is.EqualTo(Unit.Value));
        }

        [Test]
        public void Handle_Should_Throw_When_Class_Not_Found()
        {
            var command = new RestoreClassCommand { Id = Guid.NewGuid(), TeacherId = Guid.NewGuid() };
            _classroomRepoMock.Setup(r => r.GetByIdAsync(command.Id)).ReturnsAsync((Classroom?)null);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ClassNotFound));
        }

        [Test]
        public void Handle_Should_Throw_When_Class_Not_Archived()
        {
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = teacherId,
                Status = EntityStatus.Active
            };
            var command = new RestoreClassCommand { Id = classId, TeacherId = teacherId };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ClassNotArchived));
        }

        [Test]
        public void Handle_Should_Throw_When_User_Not_Found()
        {
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = teacherId,
                Status = EntityStatus.Archived
            };
            var command = new RestoreClassCommand { Id = classId, TeacherId = teacherId };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(teacherId)).ReturnsAsync((ApplicationUser?)null);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.UserNotExists));
        }

        [Test]
        public void Handle_Should_Throw_When_Not_Teacher_Or_Admin()
        {
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = Guid.NewGuid(), // khác teacherId
                Status = EntityStatus.Archived
            };
            var user = new ApplicationUser { Id = teacherId };
            var command = new RestoreClassCommand { Id = classId, TeacherId = teacherId };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(teacherId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.Student) });

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.NotTeacherOfClass));
        }

        [Test]
        public void Handle_Should_Throw_When_Commit_Fails()
        {
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = teacherId,
                Status = EntityStatus.Archived
            };
            var teacher = new ApplicationUser { Id = teacherId };
            var command = new RestoreClassCommand { Id = classId, TeacherId = teacherId };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(teacherId)).ReturnsAsync(teacher);
            _userManagerMock.Setup(m => m.GetRolesAsync(teacher)).ReturnsAsync(new List<string> { nameof(Role.Teacher) });
            _classroomRepoMock.Setup(r => r.Update(It.IsAny<Classroom>()));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ThrowsAsync(new Exception("DB error"));

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ClassRestoreFailed));
        }

        [Test]
        public async Task Handle_Should_Restore_Class_When_SchoolAdmin()
        {
            // Arrange
            var classId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = Guid.NewGuid(),
                Status = EntityStatus.Archived
            };
            var admin = new ApplicationUser { Id = adminId };

            var command = new RestoreClassCommand { Id = classId, TeacherId = adminId };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(adminId)).ReturnsAsync(admin);
            _userManagerMock.Setup(m => m.GetRolesAsync(admin)).ReturnsAsync(new List<string> { nameof(Role.SchoolAdmin) });
            _classroomRepoMock.Setup(r => r.Update(It.IsAny<Classroom>()));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(classroom.Status, Is.EqualTo(EntityStatus.Active));
            _classroomRepoMock.Verify(r => r.Update(It.Is<Classroom>(c => c.Id == classId && c.Status == EntityStatus.Active)), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
            Assert.That(result, Is.EqualTo(Unit.Value));
        }
    }
}