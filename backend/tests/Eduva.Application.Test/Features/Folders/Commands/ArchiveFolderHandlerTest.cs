using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Folders.Commands;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace Eduva.Application.Test.Features.Folders.Commands
{
    [TestFixture]
    public class ArchiveFolderHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<ILogger<ArchiveFolderHandler>> _loggerMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private Mock<IGenericRepository<Folder, Guid>> _folderRepoMock = null!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepoMock = null!;
        private Mock<IGenericRepository<Classroom, Guid>> _classroomRepoMock = null!;
        private ArchiveFolderHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<ArchiveFolderHandler>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
            _folderRepoMock = new Mock<IGenericRepository<Folder, Guid>>();
            _userRepoMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            _classroomRepoMock = new Mock<IGenericRepository<Classroom, Guid>>();

            _unitOfWorkMock.Setup(u => u.GetRepository<Folder, Guid>())
                .Returns(_folderRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>())
                .Returns(_userRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>())
                .Returns(_classroomRepoMock.Object);

            _handler = new ArchiveFolderHandler(_unitOfWorkMock.Object, _loggerMock.Object, _userManagerMock.Object);
        }

        [Test]
        public void Handle_Should_Throw_When_Folder_Not_Found()
        {
            var command = new ArchiveFolderCommand { Id = Guid.NewGuid(), CurrentUserId = Guid.NewGuid() };
            _folderRepoMock.Setup(r => r.GetByIdAsync(command.Id)).ReturnsAsync((Folder?)null);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.FolderNotFound));
        }

        [Test]
        public void Handle_Should_Throw_When_Folder_Already_Archived()
        {
            var folder = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Archived };
            var command = new ArchiveFolderCommand { Id = folder.Id, CurrentUserId = Guid.NewGuid() };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folder.Id)).ReturnsAsync(folder);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.FolderAlreadyArchived));
        }

        [Test]
        public void Handle_Should_Throw_When_Folder_Deleted()
        {
            var folder = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Deleted };
            var command = new ArchiveFolderCommand { Id = folder.Id, CurrentUserId = Guid.NewGuid() };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folder.Id)).ReturnsAsync(folder);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.FolderDeleteFailed));
        }

        [Test]
        public void Handle_Should_Throw_When_User_Has_No_Permission()
        {
            var folder = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = Guid.NewGuid() };
            var command = new ArchiveFolderCommand { Id = folder.Id, CurrentUserId = Guid.NewGuid() };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folder.Id)).ReturnsAsync(folder);
            _userRepoMock.Setup(r => r.GetByIdAsync(command.CurrentUserId)).ReturnsAsync((ApplicationUser?)null);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.Forbidden));
        }

        [Test]
        public async Task Handle_Should_Archive_When_SystemAdmin()
        {
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var folder = new Folder { Id = folderId, Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId };
            var user = new ApplicationUser { Id = userId };
            var command = new ArchiveFolderCommand { Id = folder.Id, CurrentUserId = userId };

            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, Status = EntityStatus.Active };
            var folderLessonMaterial = new FolderLessonMaterial
            {
                FolderId = folderId,
                LessonMaterialId = lessonMaterialId,
                LessonMaterial = lessonMaterial
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.SystemAdmin) });
            _folderRepoMock.Setup(r => r.Update(It.IsAny<Folder>()));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            // Setup folder-lesson material repo
            var folderLessonMaterialRepoMock = new Mock<IGenericRepository<FolderLessonMaterial, int>>();
            folderLessonMaterialRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<FolderLessonMaterial> { folderLessonMaterial });
            _unitOfWorkMock.Setup(u => u.GetRepository<FolderLessonMaterial, int>())
                .Returns(folderLessonMaterialRepoMock.Object);

            // Setup lesson material repo
            var lessonMaterialRepoMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            lessonMaterialRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<LessonMaterial> { lessonMaterial });
            lessonMaterialRepoMock.Setup(r => r.Update(It.IsAny<LessonMaterial>()));
            _unitOfWorkMock.Setup(u => u.GetRepository<LessonMaterial, Guid>())
                .Returns(lessonMaterialRepoMock.Object);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.That(folder.Status, Is.EqualTo(EntityStatus.Archived));
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folder.Id && f.Status == EntityStatus.Archived)), Times.Once);
            lessonMaterialRepoMock.Verify(r => r.Update(It.Is<LessonMaterial>(lm => lm.Status == EntityStatus.Deleted)), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
            Assert.That(result, Is.EqualTo(Unit.Value));
        }

        [Test]
        public async Task Handle_Should_Archive_When_Owner_Of_Personal_Folder()
        {
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var folder = new Folder { Id = folderId, Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId };
            var user = new ApplicationUser { Id = userId };
            var command = new ArchiveFolderCommand { Id = folder.Id, CurrentUserId = userId };

            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, Status = EntityStatus.Active };
            var folderLessonMaterial = new FolderLessonMaterial
            {
                FolderId = folderId,
                LessonMaterialId = lessonMaterialId,
                LessonMaterial = lessonMaterial
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.Student) });
            _folderRepoMock.Setup(r => r.Update(It.IsAny<Folder>()));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            // Setup folder-lesson material repo
            var folderLessonMaterialRepoMock = new Mock<IGenericRepository<FolderLessonMaterial, int>>();
            folderLessonMaterialRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<FolderLessonMaterial> { folderLessonMaterial });
            _unitOfWorkMock.Setup(u => u.GetRepository<FolderLessonMaterial, int>())
                .Returns(folderLessonMaterialRepoMock.Object);

            // Setup lesson material repo
            var lessonMaterialRepoMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            lessonMaterialRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<LessonMaterial> { lessonMaterial });
            lessonMaterialRepoMock.Setup(r => r.Update(It.IsAny<LessonMaterial>()));
            _unitOfWorkMock.Setup(u => u.GetRepository<LessonMaterial, Guid>())
                .Returns(lessonMaterialRepoMock.Object);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.That(folder.Status, Is.EqualTo(EntityStatus.Archived));
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folder.Id && f.Status == EntityStatus.Archived)), Times.Once);
            lessonMaterialRepoMock.Verify(r => r.Update(It.Is<LessonMaterial>(lm => lm.Status == EntityStatus.Deleted)), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
            Assert.That(result, Is.EqualTo(Unit.Value));
        }

        [Test]
        public void Handle_Should_Archive_When_Teacher_Of_Class_Folder()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var folder = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active, OwnerType = OwnerType.Class, ClassId = classId };
            var user = new ApplicationUser { Id = userId };
            var classroom = new Classroom { Id = classId, TeacherId = userId, SchoolId = 1 };
            var command = new ArchiveFolderCommand { Id = folder.Id, CurrentUserId = userId };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folder.Id)).ReturnsAsync(folder);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.Teacher) });
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.FolderMustBePersonal));
        }

        [Test]
        public void Handle_Should_Archive_When_SchoolAdmin_Of_Same_School()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var folder = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active, OwnerType = OwnerType.Class, ClassId = classId };
            var user = new ApplicationUser { Id = userId, SchoolId = 5 };
            var classroom = new Classroom { Id = classId, TeacherId = Guid.NewGuid(), SchoolId = 5 };
            var command = new ArchiveFolderCommand { Id = folder.Id, CurrentUserId = userId };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folder.Id)).ReturnsAsync(folder);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.SchoolAdmin) });
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.FolderMustBePersonal));
        }

        [Test]
        public void Handle_Should_Throw_When_Class_Not_Found_For_Class_Folder()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var folder = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active, OwnerType = OwnerType.Class, ClassId = classId };
            var user = new ApplicationUser { Id = userId, SchoolId = 5 };
            var command = new ArchiveFolderCommand { Id = folder.Id, CurrentUserId = userId };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folder.Id)).ReturnsAsync(folder);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.SchoolAdmin) });
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync((Classroom?)null);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.FolderMustBePersonal));
        }

        [Test]
        public void Handle_Should_Throw_When_Commit_Fails()
        {
            var userId = Guid.NewGuid();
            var folder = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId };
            var user = new ApplicationUser { Id = userId };
            var command = new ArchiveFolderCommand { Id = folder.Id, CurrentUserId = userId };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folder.Id)).ReturnsAsync(folder);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.SystemAdmin) });
            _folderRepoMock.Setup(r => r.Update(It.IsAny<Folder>()));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ThrowsAsync(new Exception("DB error"));

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.FolderArchiveFailed));
        }

        [Test]
        public async Task Handle_Should_ArchiveFolder_Even_When_LessonMaterial_Already_Deleted()
        {
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var folder = new Folder { Id = folderId, Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId };
            var user = new ApplicationUser { Id = userId };
            var command = new ArchiveFolderCommand { Id = folder.Id, CurrentUserId = userId };

            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, Status = EntityStatus.Deleted };
            var folderLessonMaterial = new FolderLessonMaterial
            {
                FolderId = folderId,
                LessonMaterialId = lessonMaterialId,
                LessonMaterial = lessonMaterial
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.SystemAdmin) });
            _folderRepoMock.Setup(r => r.Update(It.IsAny<Folder>()));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var folderLessonMaterialRepoMock = new Mock<IGenericRepository<FolderLessonMaterial, int>>();
            folderLessonMaterialRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<FolderLessonMaterial> { folderLessonMaterial });
            _unitOfWorkMock.Setup(u => u.GetRepository<FolderLessonMaterial, int>())
                .Returns(folderLessonMaterialRepoMock.Object);

            var lessonMaterialRepoMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            lessonMaterialRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<LessonMaterial> { lessonMaterial });
            lessonMaterialRepoMock.Setup(r => r.Update(It.IsAny<LessonMaterial>()));
            _unitOfWorkMock.Setup(u => u.GetRepository<LessonMaterial, Guid>())
                .Returns(lessonMaterialRepoMock.Object);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.That(folder.Status, Is.EqualTo(EntityStatus.Archived));
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folder.Id && f.Status == EntityStatus.Archived)), Times.Once);
            lessonMaterialRepoMock.Verify(r => r.Update(It.IsAny<LessonMaterial>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
            Assert.That(result, Is.EqualTo(Unit.Value));
        }

        [Test]
        public void Handle_Should_Throw_When_Not_Owner_Of_Personal_Folder()
        {
            var folder = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = Guid.NewGuid() };
            var command = new ArchiveFolderCommand { Id = folder.Id, CurrentUserId = Guid.NewGuid() };
            var user = new ApplicationUser { Id = command.CurrentUserId };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folder.Id)).ReturnsAsync(folder);
            _userRepoMock.Setup(r => r.GetByIdAsync(command.CurrentUserId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.Student) });

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.Forbidden));
        }

        [Test]
        public async Task HasPermissionToUpdateFolder_Should_ReturnFalse_When_Classroom_NotFound()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var folder = new Folder { Id = Guid.NewGuid(), OwnerType = OwnerType.Class, ClassId = classId };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.Teacher) });
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync((Classroom?)null);

            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>()).Returns(_userRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>()).Returns(_classroomRepoMock.Object);

            var handler = new ArchiveFolderHandler(_unitOfWorkMock.Object, _loggerMock.Object, _userManagerMock.Object);
            var task = (Task<bool>)handler.GetType()
                .GetMethod("HasPermissionToUpdateFolder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(handler, new object[] { folder, userId })!;
            Assert.That(await task, Is.False);
        }

        [TestCase(nameof(Role.Teacher))]
        [TestCase(nameof(Role.ContentModerator))]
        public async Task HasPermissionToUpdateFolder_Should_ReturnTrue_When_TeacherOrContentModerator_And_IsClassTeacher(string role)
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var folder = new Folder { Id = Guid.NewGuid(), OwnerType = OwnerType.Class, ClassId = classId };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var classroom = new Classroom { Id = classId, TeacherId = userId, SchoolId = 1 };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { role });
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);

            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>()).Returns(_userRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>()).Returns(_classroomRepoMock.Object);

            var handler = new ArchiveFolderHandler(_unitOfWorkMock.Object, _loggerMock.Object, _userManagerMock.Object);
            var task = (Task<bool>)handler.GetType()
                .GetMethod("HasPermissionToUpdateFolder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(handler, new object[] { folder, userId })!;
            Assert.That(await task, Is.True);
        }

        [Test]
        public async Task HasPermissionToUpdateFolder_Should_ReturnTrue_When_SchoolAdmin_And_SameSchool()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var folder = new Folder { Id = Guid.NewGuid(), OwnerType = OwnerType.Class, ClassId = classId };
            var user = new ApplicationUser { Id = userId, SchoolId = 5 };
            var classroom = new Classroom { Id = classId, TeacherId = Guid.NewGuid(), SchoolId = 5 };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.SchoolAdmin) });
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);

            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>()).Returns(_userRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>()).Returns(_classroomRepoMock.Object);

            var handler = new ArchiveFolderHandler(_unitOfWorkMock.Object, _loggerMock.Object, _userManagerMock.Object);
            var task = (Task<bool>)handler.GetType()
                .GetMethod("HasPermissionToUpdateFolder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(handler, new object[] { folder, userId })!;
            Assert.That(await task, Is.True);
        }

        [Test]
        public async Task HasPermissionToUpdateFolder_Should_ReturnFalse_When_NotTeacher_NotSchoolAdmin()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var folder = new Folder { Id = Guid.NewGuid(), OwnerType = OwnerType.Class, ClassId = classId };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var classroom = new Classroom { Id = classId, TeacherId = Guid.NewGuid(), SchoolId = 2 };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.Student) });
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);

            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>()).Returns(_userRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>()).Returns(_classroomRepoMock.Object);

            var handler = new ArchiveFolderHandler(_unitOfWorkMock.Object, _loggerMock.Object, _userManagerMock.Object);
            var task = (Task<bool>)handler.GetType()
                .GetMethod("HasPermissionToUpdateFolder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(handler, new object[] { folder, userId })!;
            Assert.That(await task, Is.False);
        }
    }
}
