using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Classes.Commands.AddMaterialsToFolder;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;
using System.Linq.Expressions;

namespace Eduva.Application.Test.Features.Classes.Commands.AddMaterialsToFolder
{
    [TestFixture]
    public class AddMaterialsToFolderHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private Mock<IGenericRepository<Folder, Guid>> _folderRepoMock = null!;
        private Mock<IGenericRepository<LessonMaterial, Guid>> _lessonMaterialRepoMock = null!;
        private Mock<IGenericRepository<FolderLessonMaterial, int>> _folderLessonMaterialRepoMock = null!;
        private Mock<IGenericRepository<Classroom, Guid>> _classroomRepoMock = null!;
        private Mock<IGenericRepository<StudentClass, Guid>> _studentClassRepoMock = null!;
        private AddMaterialsToFolderHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

            _folderRepoMock = new Mock<IGenericRepository<Folder, Guid>>();
            _lessonMaterialRepoMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            _folderLessonMaterialRepoMock = new Mock<IGenericRepository<FolderLessonMaterial, int>>();
            _classroomRepoMock = new Mock<IGenericRepository<Classroom, Guid>>();
            _studentClassRepoMock = new Mock<IGenericRepository<StudentClass, Guid>>();

            _unitOfWorkMock.Setup(u => u.GetRepository<Folder, Guid>()).Returns(_folderRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<LessonMaterial, Guid>()).Returns(_lessonMaterialRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<FolderLessonMaterial, int>()).Returns(_folderLessonMaterialRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>()).Returns(_classroomRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<StudentClass, Guid>()).Returns(_studentClassRepoMock.Object);

            _handler = new AddMaterialsToFolderHandler(_unitOfWorkMock.Object, _userManagerMock.Object);
        }

        [Test]
        public async Task Handle_ShouldAddMaterialsToFolder_WhenValid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var materialId = Guid.NewGuid();

            var folder = new Folder { Id = folderId, ClassId = classId, OwnerType = OwnerType.Class };
            var material = new LessonMaterial { Id = materialId, CreatedByUserId = userId, LessonStatus = LessonMaterialStatus.Approved };
            var user = new ApplicationUser { Id = userId };

            var command = new AddMaterialsToFolderCommand
            {
                FolderId = folderId,
                ClassId = classId,
                MaterialIds = new List<Guid> { materialId },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _lessonMaterialRepoMock.Setup(r => r.GetByIdAsync(materialId)).ReturnsAsync(material);
            _folderLessonMaterialRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>()))
    .ReturnsAsync(false);
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.SystemAdmin) });
            _folderLessonMaterialRepoMock.Setup(r => r.AddAsync(It.IsAny<FolderLessonMaterial>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.True);
            _folderLessonMaterialRepoMock.Verify(r => r.AddAsync(It.Is<FolderLessonMaterial>(
                flm => flm.FolderId == folderId && flm.LessonMaterialId == materialId)), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Test]
        public void Handle_ShouldThrow_WhenFolderNotFound()
        {
            // Arrange
            var command = new AddMaterialsToFolderCommand
            {
                FolderId = Guid.NewGuid(),
                ClassId = Guid.NewGuid(),
                MaterialIds = new List<Guid> { Guid.NewGuid() },
                CurrentUserId = Guid.NewGuid()
            };
            _folderRepoMock.Setup(r => r.GetByIdAsync(command.FolderId)).ReturnsAsync((Folder?)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.Message, Is.EqualTo("Folder not found"));
        }

        [Test]
        public void Handle_ShouldThrow_WhenFolderClassIdMismatch()
        {
            // Arrange
            var folder = new Folder { Id = Guid.NewGuid(), ClassId = Guid.NewGuid(), OwnerType = OwnerType.Class };
            var command = new AddMaterialsToFolderCommand
            {
                FolderId = folder.Id,
                ClassId = Guid.NewGuid(), // khác với folder.ClassId
                MaterialIds = new List<Guid> { Guid.NewGuid() },
                CurrentUserId = Guid.NewGuid()
            };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folder.Id)).ReturnsAsync(folder);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.Message, Is.EqualTo(CustomCode.Unauthorized.ToString()));
        }

        [Test]
        public void Handle_ShouldThrow_WhenMaterialNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var materialId = Guid.NewGuid();

            var folder = new Folder { Id = folderId, ClassId = classId, OwnerType = OwnerType.Class };

            var command = new AddMaterialsToFolderCommand
            {
                FolderId = folderId,
                ClassId = classId,
                MaterialIds = new List<Guid> { materialId },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(new ApplicationUser { Id = userId });
            _userManagerMock.Setup(u => u.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { nameof(Role.SystemAdmin) });
            _lessonMaterialRepoMock.Setup(r => r.GetByIdAsync(materialId)).ReturnsAsync((LessonMaterial?)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.Message, Does.Contain("Lesson material not found"));
        }

        [Test]
        public void Handle_ShouldThrow_WhenMaterialNotOwnedByUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var materialId = Guid.NewGuid();

            var folder = new Folder { Id = folderId, ClassId = classId, OwnerType = OwnerType.Class };
            var material = new LessonMaterial { Id = materialId, CreatedByUserId = Guid.NewGuid(), LessonStatus = LessonMaterialStatus.Approved };

            var command = new AddMaterialsToFolderCommand
            {
                FolderId = folderId,
                ClassId = classId,
                MaterialIds = new List<Guid> { materialId },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(new ApplicationUser { Id = userId });
            _userManagerMock.Setup(u => u.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { nameof(Role.SystemAdmin) });
            _lessonMaterialRepoMock.Setup(r => r.GetByIdAsync(materialId)).ReturnsAsync(material);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.Message, Is.EqualTo(CustomCode.Unauthorized.ToString()));
        }

        [Test]
        public async Task Handle_ShouldNotAddMaterial_IfAlreadyInFolder()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var materialId = Guid.NewGuid();

            var folder = new Folder { Id = folderId, ClassId = classId, OwnerType = OwnerType.Class };
            var material = new LessonMaterial { Id = materialId, CreatedByUserId = userId, LessonStatus = LessonMaterialStatus.Approved };
            var user = new ApplicationUser { Id = userId };

            var command = new AddMaterialsToFolderCommand
            {
                FolderId = folderId,
                ClassId = classId,
                MaterialIds = new List<Guid> { materialId },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _lessonMaterialRepoMock.Setup(r => r.GetByIdAsync(materialId)).ReturnsAsync(material);
            _folderLessonMaterialRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>()))
    .ReturnsAsync(true);
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.SystemAdmin) });
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.True);
            _folderLessonMaterialRepoMock.Verify(r => r.AddAsync(It.IsAny<FolderLessonMaterial>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Test]
        public void Handle_ShouldThrow_WhenUserNotExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var materialId = Guid.NewGuid();

            var folder = new Folder { Id = folderId, ClassId = classId, OwnerType = OwnerType.Class };

            var command = new AddMaterialsToFolderCommand
            {
                FolderId = folderId,
                ClassId = classId,
                MaterialIds = new List<Guid> { materialId },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync((ApplicationUser?)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.UserNotExists));
        }

        [Test]
        public async Task Handle_ShouldAllow_WhenPersonalFolderAndOwner()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var materialId = Guid.NewGuid();

            var folder = new Folder { Id = folderId, OwnerType = OwnerType.Personal, UserId = userId };
            var material = new LessonMaterial { Id = materialId, CreatedByUserId = userId, LessonStatus = LessonMaterialStatus.Approved };
            var user = new ApplicationUser { Id = userId };

            var command = new AddMaterialsToFolderCommand
            {
                FolderId = folderId,
                MaterialIds = new List<Guid> { materialId },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _lessonMaterialRepoMock.Setup(r => r.GetByIdAsync(materialId)).ReturnsAsync(material);
            _folderLessonMaterialRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>())).ReturnsAsync(false);
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.Student) });
            _folderLessonMaterialRepoMock.Setup(r => r.AddAsync(It.IsAny<FolderLessonMaterial>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void Handle_ShouldThrow_WhenClassNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var materialId = Guid.NewGuid();

            var folder = new Folder { Id = folderId, OwnerType = OwnerType.Class, ClassId = classId };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };

            var command = new AddMaterialsToFolderCommand
            {
                FolderId = folderId,
                ClassId = classId,
                MaterialIds = new List<Guid> { materialId },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.Teacher) });
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync((Classroom?)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ClassNotFound));
        }

        [Test]
        public async Task Handle_ShouldAllow_WhenTeacherOfClass()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var materialId = Guid.NewGuid();

            var folder = new Folder { Id = folderId, OwnerType = OwnerType.Class, ClassId = classId };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var classroom = new Classroom { Id = classId, TeacherId = userId, SchoolId = 1 };
            var material = new LessonMaterial { Id = materialId, CreatedByUserId = userId, LessonStatus = LessonMaterialStatus.Approved };

            var command = new AddMaterialsToFolderCommand
            {
                FolderId = folderId,
                ClassId = classId,
                MaterialIds = new List<Guid> { materialId },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.Teacher) });
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _lessonMaterialRepoMock.Setup(r => r.GetByIdAsync(materialId)).ReturnsAsync(material);
            _folderLessonMaterialRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>())).ReturnsAsync(false);
            _folderLessonMaterialRepoMock.Setup(r => r.AddAsync(It.IsAny<FolderLessonMaterial>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task Handle_ShouldAllow_WhenSchoolAdminOfSameSchool()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var materialId = Guid.NewGuid();

            var folder = new Folder { Id = folderId, OwnerType = OwnerType.Class, ClassId = classId };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var classroom = new Classroom { Id = classId, TeacherId = Guid.NewGuid(), SchoolId = 1 };
            var material = new LessonMaterial { Id = materialId, CreatedByUserId = userId, LessonStatus = LessonMaterialStatus.Approved };

            var command = new AddMaterialsToFolderCommand
            {
                FolderId = folderId,
                ClassId = classId,
                MaterialIds = new List<Guid> { materialId },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.SchoolAdmin) });
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _lessonMaterialRepoMock.Setup(r => r.GetByIdAsync(materialId)).ReturnsAsync(material);
            _folderLessonMaterialRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>())).ReturnsAsync(false);
            _folderLessonMaterialRepoMock.Setup(r => r.AddAsync(It.IsAny<FolderLessonMaterial>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task Handle_ShouldAllow_WhenStudentEnrolledInClass()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var materialId = Guid.NewGuid();

            var folder = new Folder { Id = folderId, OwnerType = OwnerType.Class, ClassId = classId };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var classroom = new Classroom { Id = classId, TeacherId = Guid.NewGuid(), SchoolId = 1 };
            var material = new LessonMaterial { Id = materialId, CreatedByUserId = userId, LessonStatus = LessonMaterialStatus.Approved };

            var command = new AddMaterialsToFolderCommand
            {
                FolderId = folderId,
                ClassId = classId,
                MaterialIds = new List<Guid> { materialId },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.Student) });
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _studentClassRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<StudentClass, bool>>>())).ReturnsAsync(true);
            _lessonMaterialRepoMock.Setup(r => r.GetByIdAsync(materialId)).ReturnsAsync(material);
            _folderLessonMaterialRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<FolderLessonMaterial, bool>>>())).ReturnsAsync(false);
            _folderLessonMaterialRepoMock.Setup(r => r.AddAsync(It.IsAny<FolderLessonMaterial>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void Handle_ShouldThrow_WhenStudentNotEnrolledInClass()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var materialId = Guid.NewGuid();

            var folder = new Folder { Id = folderId, OwnerType = OwnerType.Class, ClassId = classId };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var classroom = new Classroom { Id = classId, TeacherId = Guid.NewGuid(), SchoolId = 1 };

            var command = new AddMaterialsToFolderCommand
            {
                FolderId = folderId,
                ClassId = classId,
                MaterialIds = new List<Guid> { materialId },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.Student) });
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _studentClassRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<StudentClass, bool>>>())).ReturnsAsync(false);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.Unauthorized));
        }
    }
}