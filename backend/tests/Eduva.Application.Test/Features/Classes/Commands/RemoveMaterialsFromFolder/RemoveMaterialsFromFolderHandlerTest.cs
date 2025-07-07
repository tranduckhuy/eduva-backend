using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Classes.Commands.RemoveMaterialsFromFolder;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.Classes.Commands.RemoveMaterialsFromFolder
{
    [TestFixture]
    public class RemoveMaterialsFromFolderHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private Mock<IGenericRepository<Folder, Guid>> _folderRepoMock = null!;
        private Mock<IGenericRepository<FolderLessonMaterial, int>> _folderLessonMaterialRepoMock = null!;
        private Mock<IGenericRepository<Classroom, Guid>> _classroomRepoMock = null!;
        private RemoveMaterialsFromFolderHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
            _folderRepoMock = new Mock<IGenericRepository<Folder, Guid>>();
            _folderLessonMaterialRepoMock = new Mock<IGenericRepository<FolderLessonMaterial, int>>();
            _classroomRepoMock = new Mock<IGenericRepository<Classroom, Guid>>();

            _unitOfWorkMock.Setup(u => u.GetRepository<Folder, Guid>())
                .Returns(_folderRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<FolderLessonMaterial, int>())
                .Returns(_folderLessonMaterialRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>())
                .Returns(_classroomRepoMock.Object);

            _handler = new RemoveMaterialsFromFolderHandler(_unitOfWorkMock.Object, _userManagerMock.Object);
        }

        [Test]
        public async Task Handle_Should_Remove_Materials_When_Valid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var materialId = Guid.NewGuid();

            var folder = new Folder { Id = folderId, ClassId = classId, OwnerType = OwnerType.Class };
            var classroom = new Classroom { Id = classId, TeacherId = userId, SchoolId = 1 };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var roles = new List<string> { nameof(Role.SystemAdmin) };

            var folderMaterial = new FolderLessonMaterial { Id = Guid.NewGuid(), FolderId = folderId, LessonMaterialId = materialId };

            var command = new RemoveMaterialsFromFolderCommand
            {
                FolderId = folderId,
                ClassId = classId,
                MaterialIds = new List<Guid> { materialId },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(roles);
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _folderLessonMaterialRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<FolderLessonMaterial> { folderMaterial });
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.True);
            _folderLessonMaterialRepoMock.Verify(r => r.Remove(It.Is<FolderLessonMaterial>(f => f.Id == folderMaterial.Id)), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Test]
        public void Handle_Should_Throw_When_Folder_Not_Found()
        {
            var command = new RemoveMaterialsFromFolderCommand
            {
                FolderId = Guid.NewGuid(),
                ClassId = Guid.NewGuid(),
                MaterialIds = new List<Guid> { Guid.NewGuid() },
                CurrentUserId = Guid.NewGuid()
            };
            _folderRepoMock.Setup(r => r.GetByIdAsync(command.FolderId)).ReturnsAsync((Folder?)null);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.FolderNotFound));
        }

        [Test]
        public void Handle_Should_Throw_When_Folder_Not_Belongs_To_Class()
        {
            var folder = new Folder { Id = Guid.NewGuid(), ClassId = Guid.NewGuid(), OwnerType = OwnerType.Personal };
            var command = new RemoveMaterialsFromFolderCommand
            {
                FolderId = folder.Id,
                ClassId = Guid.NewGuid(),
                MaterialIds = new List<Guid> { Guid.NewGuid() },
                CurrentUserId = Guid.NewGuid()
            };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folder.Id)).ReturnsAsync(folder);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.Unauthorized));
        }

        [Test]
        public void Handle_Should_Throw_When_User_Not_Exists()
        {
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var folder = new Folder { Id = folderId, ClassId = classId, OwnerType = OwnerType.Class };
            var command = new RemoveMaterialsFromFolderCommand
            {
                FolderId = folderId,
                ClassId = classId,
                MaterialIds = new List<Guid> { Guid.NewGuid() },
                CurrentUserId = Guid.NewGuid()
            };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userManagerMock.Setup(u => u.FindByIdAsync(command.CurrentUserId.ToString())).ReturnsAsync((ApplicationUser?)null);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.UserNotExists));
        }

        [Test]
        public void Handle_Should_Throw_When_Material_Not_Found_In_Folder()
        {
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var materialId = Guid.NewGuid();

            var folder = new Folder { Id = folderId, ClassId = classId, OwnerType = OwnerType.Class };
            var classroom = new Classroom { Id = classId, TeacherId = userId, SchoolId = 1 };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var roles = new List<string> { nameof(Role.SystemAdmin) };

            var command = new RemoveMaterialsFromFolderCommand
            {
                FolderId = folderId,
                ClassId = classId,
                MaterialIds = new List<Guid> { materialId },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(roles);
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _folderLessonMaterialRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<FolderLessonMaterial>()); // Không có material
            // Không cần setup CommitAsync vì sẽ throw trước

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.LessonMaterialNotFoundInFolder));
        }

        [Test]
        public void Handle_Should_Throw_When_User_Not_Authorized()
        {
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var folder = new Folder { Id = folderId, ClassId = classId, OwnerType = OwnerType.Class };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var classroom = new Classroom { Id = classId, TeacherId = Guid.NewGuid(), SchoolId = 2 }; // khác teacherId và schoolId
            var command = new RemoveMaterialsFromFolderCommand
            {
                FolderId = folderId,
                ClassId = classId,
                MaterialIds = new List<Guid> { Guid.NewGuid() },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { nameof(Role.Student) });
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _folderLessonMaterialRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<FolderLessonMaterial>());

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.Unauthorized));
        }


        [Test]
        public void Handle_Should_Throw_When_Class_Not_Found()
        {
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var materialId = Guid.NewGuid();

            var folder = new Folder { Id = folderId, ClassId = classId, OwnerType = OwnerType.Class };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var roles = new List<string> { nameof(Role.Teacher) };

            var command = new RemoveMaterialsFromFolderCommand
            {
                FolderId = folderId,
                ClassId = classId,
                MaterialIds = new List<Guid> { materialId },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(roles);
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync((Classroom?)null);

            var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ClassNotFound));
        }

        [Test]
        public async Task Handle_Should_Allow_When_Teacher_Of_Class()
        {
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var materialId = Guid.NewGuid();

            var folder = new Folder { Id = folderId, ClassId = classId, OwnerType = OwnerType.Class };
            var classroom = new Classroom { Id = classId, TeacherId = userId, SchoolId = 1 };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var roles = new List<string> { nameof(Role.Teacher) };
            var folderMaterial = new FolderLessonMaterial { Id = Guid.NewGuid(), FolderId = folderId, LessonMaterialId = materialId };

            var command = new RemoveMaterialsFromFolderCommand
            {
                FolderId = folderId,
                ClassId = classId,
                MaterialIds = new List<Guid> { materialId },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(roles);
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _folderLessonMaterialRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<FolderLessonMaterial> { folderMaterial });
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task Handle_Should_Allow_When_SchoolAdmin_Of_Same_School()
        {
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var materialId = Guid.NewGuid();

            var folder = new Folder { Id = folderId, ClassId = classId, OwnerType = OwnerType.Class };
            var classroom = new Classroom { Id = classId, TeacherId = Guid.NewGuid(), SchoolId = 1 };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var roles = new List<string> { nameof(Role.SchoolAdmin) };
            var folderMaterial = new FolderLessonMaterial { Id = Guid.NewGuid(), FolderId = folderId, LessonMaterialId = materialId };

            var command = new RemoveMaterialsFromFolderCommand
            {
                FolderId = folderId,
                ClassId = classId,
                MaterialIds = new List<Guid> { materialId },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(roles);
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _folderLessonMaterialRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<FolderLessonMaterial> { folderMaterial });
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.That(result, Is.True);
        }
    }
}