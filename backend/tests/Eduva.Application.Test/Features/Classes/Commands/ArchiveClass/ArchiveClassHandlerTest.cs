using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Classes.Commands.ArchiveClass;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.Classes.Commands.ArchiveClass
{
    [TestFixture]
    public class ArchiveClassHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private Mock<IClassroomRepository> _classroomRepoMock = null!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepoMock = null!;
        private ArchiveClassHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
            _classroomRepoMock = new Mock<IClassroomRepository>();
            _userRepoMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IClassroomRepository>())
                .Returns(_classroomRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>())
                .Returns(_userRepoMock.Object);

            _handler = new ArchiveClassHandler(_unitOfWorkMock.Object, _userManagerMock.Object);
        }

        [Test]
        public async Task Handle_ShouldArchiveClass_WhenTeacherIsOwner()
        {
            // Arrange
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = teacherId,
                Status = EntityStatus.Active
            };
            var teacher = new ApplicationUser { Id = teacherId };

            var command = new ArchiveClassCommand { Id = classId, TeacherId = teacherId };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(teacherId)).ReturnsAsync(teacher);
            _userManagerMock.Setup(m => m.GetRolesAsync(teacher)).ReturnsAsync(new List<string> { nameof(Role.Teacher) });
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(classroom.Status, Is.EqualTo(EntityStatus.Archived));
            _classroomRepoMock.Verify(r => r.Update(It.Is<Classroom>(c => c.Id == classId && c.Status == EntityStatus.Archived)), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
            Assert.That(result, Is.EqualTo(MediatR.Unit.Value));
        }

        [Test]
        public async Task Handle_ShouldArchiveClass_WhenUserIsAdmin()
        {
            // Arrange
            var classId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = Guid.NewGuid(),
                Status = EntityStatus.Active
            };
            var admin = new ApplicationUser { Id = adminId };

            var command = new ArchiveClassCommand { Id = classId, TeacherId = adminId };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(adminId)).ReturnsAsync(admin);
            _userManagerMock.Setup(m => m.GetRolesAsync(admin)).ReturnsAsync(new List<string> { nameof(Role.SystemAdmin) });
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(classroom.Status, Is.EqualTo(EntityStatus.Archived));
            _classroomRepoMock.Verify(r => r.Update(It.Is<Classroom>(c => c.Id == classId && c.Status == EntityStatus.Archived)), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
            Assert.That(result, Is.EqualTo(MediatR.Unit.Value));
        }

        [Test]
        public void Handle_ShouldThrow_WhenClassNotFound()
        {
            // Arrange
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var command = new ArchiveClassCommand { Id = classId, TeacherId = teacherId };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync((Classroom?)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ClassNotFound));
        }

        [Test]
        public void Handle_ShouldThrow_WhenClassAlreadyArchived()
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
            var command = new ArchiveClassCommand { Id = classId, TeacherId = teacherId };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ClassAlreadyArchived));
        }

        [Test]
        public void Handle_ShouldThrow_WhenUserNotFound()
        {
            // Arrange
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = teacherId,
                Status = EntityStatus.Active
            };
            var command = new ArchiveClassCommand { Id = classId, TeacherId = teacherId };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(teacherId)).ReturnsAsync((ApplicationUser?)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.UserNotExists));
        }

        [Test]
        public void Handle_ShouldThrow_WhenUserIsNotTeacherOrAdmin()
        {
            // Arrange
            var classId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = Guid.NewGuid(), // khác userId
                Status = EntityStatus.Active
            };
            var user = new ApplicationUser { Id = userId };
            var command = new ArchiveClassCommand { Id = classId, TeacherId = userId };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.Student) });

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.NotTeacherOfClass));
        }

        [Test]
        public void Handle_ShouldThrow_WhenExceptionOccurs()
        {
            // Arrange
            var classId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = teacherId,
                Status = EntityStatus.Active
            };
            var teacher = new ApplicationUser { Id = teacherId };
            var command = new ArchiveClassCommand { Id = classId, TeacherId = teacherId };

            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(teacherId)).ReturnsAsync(teacher);
            _userManagerMock.Setup(m => m.GetRolesAsync(teacher)).ReturnsAsync(new List<string> { nameof(Role.Teacher) });
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ThrowsAsync(new Exception("DB error"));

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ClassArchiveFailed));
        }
    }
}