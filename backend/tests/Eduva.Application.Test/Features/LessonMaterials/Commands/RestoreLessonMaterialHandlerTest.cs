using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.LessonMaterials.Commands.RestoreLessonMaterial;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.LessonMaterials.Commands
{
    [TestFixture]
    public class RestoreLessonMaterialHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private Mock<IGenericRepository<Folder, Guid>> _folderRepoMock = null!;
        private Mock<IGenericRepository<LessonMaterial, Guid>> _lessonMaterialRepoMock = null!;
        private Mock<IGenericRepository<FolderLessonMaterial, int>> _folderLessonMaterialRepoMock = null!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepoMock = null!;
        private RestoreLessonMaterialHandler _handler = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

            _folderRepoMock = new Mock<IGenericRepository<Folder, Guid>>();
            _lessonMaterialRepoMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            _folderLessonMaterialRepoMock = new Mock<IGenericRepository<FolderLessonMaterial, int>>();
            _userRepoMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();

            _unitOfWorkMock.Setup(u => u.GetRepository<Folder, Guid>()).Returns(_folderRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<LessonMaterial, Guid>()).Returns(_lessonMaterialRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<FolderLessonMaterial, int>()).Returns(_folderLessonMaterialRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>()).Returns(_userRepoMock.Object);

            _handler = new RestoreLessonMaterialHandler(_unitOfWorkMock.Object, _userManagerMock.Object);
        }

        [Test]
        public void Handle_Should_Throw_When_User_Not_Found()
        {
            var cmd = new RestoreLessonMaterialCommand
            {
                PersonalFolderId = Guid.NewGuid(),
                CurrentUserId = Guid.NewGuid(),
                LessonMaterialIds = new List<Guid> { Guid.NewGuid() }
            };

            _userRepoMock.Setup(r => r.GetByIdAsync(cmd.CurrentUserId)).ReturnsAsync((ApplicationUser?)null);

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public void Handle_Should_Throw_When_Folder_Not_Found()
        {
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var cmd = new RestoreLessonMaterialCommand
            {
                PersonalFolderId = folderId,
                CurrentUserId = userId,
                LessonMaterialIds = new List<Guid> { Guid.NewGuid() }
            };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId });
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync((Folder?)null);

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public void Handle_Should_Throw_When_Folder_Is_Not_Personal_Or_Not_Active()
        {
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var cmd = new RestoreLessonMaterialCommand
            {
                PersonalFolderId = folderId,
                CurrentUserId = userId,
                LessonMaterialIds = new List<Guid> { Guid.NewGuid() }
            };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId });
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(new Folder { Id = folderId, OwnerType = OwnerType.Class, Status = EntityStatus.Active, UserId = userId });

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(cmd, CancellationToken.None));

            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(new Folder { Id = folderId, OwnerType = OwnerType.Personal, Status = EntityStatus.Deleted, UserId = userId });

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public void Handle_Should_Throw_When_User_Has_No_Permission_To_Use_Folder()
        {
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var cmd = new RestoreLessonMaterialCommand
            {
                PersonalFolderId = folderId,
                CurrentUserId = userId,
                LessonMaterialIds = new List<Guid> { Guid.NewGuid() }
            };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId });
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(new Folder { Id = folderId, OwnerType = OwnerType.Personal, Status = EntityStatus.Active, UserId = Guid.NewGuid() });

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public void Handle_Should_Throw_When_No_LessonMaterial_To_Restore()
        {
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var cmd = new RestoreLessonMaterialCommand
            {
                PersonalFolderId = folderId,
                CurrentUserId = userId,
                LessonMaterialIds = new List<Guid> { lessonMaterialId }
            };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId });
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(new Folder { Id = folderId, OwnerType = OwnerType.Personal, Status = EntityStatus.Active, UserId = userId });
            _lessonMaterialRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<LessonMaterial>());

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public void Handle_Should_Throw_When_User_Has_No_Permission_To_Restore_LessonMaterial()
        {
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var cmd = new RestoreLessonMaterialCommand
            {
                PersonalFolderId = folderId,
                CurrentUserId = userId,
                LessonMaterialIds = new List<Guid> { lessonMaterialId }
            };

            var user = new ApplicationUser { Id = userId };
            var folder = new Folder { Id = folderId, OwnerType = OwnerType.Personal, Status = EntityStatus.Active, UserId = userId };
            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, Status = EntityStatus.Deleted, CreatedByUserId = Guid.NewGuid() };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _lessonMaterialRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<LessonMaterial> { lessonMaterial });
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Teacher" });

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public async Task Handle_Should_Restore_LessonMaterial_And_Add_To_Folder_If_Not_Exists()
        {
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var cmd = new RestoreLessonMaterialCommand
            {
                PersonalFolderId = folderId,
                CurrentUserId = userId,
                LessonMaterialIds = new List<Guid> { lessonMaterialId }
            };

            var user = new ApplicationUser { Id = userId };
            var folder = new Folder { Id = folderId, OwnerType = OwnerType.Personal, Status = EntityStatus.Active, UserId = userId };
            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, Status = EntityStatus.Deleted, CreatedByUserId = userId };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _lessonMaterialRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<LessonMaterial> { lessonMaterial });
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Teacher" });

            _lessonMaterialRepoMock.Setup(r => r.Update(lessonMaterial));
            _folderLessonMaterialRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<FolderLessonMaterial>());
            _folderLessonMaterialRepoMock.Setup(r => r.AddAsync(It.IsAny<FolderLessonMaterial>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(0);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            _lessonMaterialRepoMock.Verify(r => r.Update(lessonMaterial), Times.Once);
            _folderLessonMaterialRepoMock.Verify(r => r.AddAsync(It.Is<FolderLessonMaterial>(flm => flm.FolderId == folderId && flm.LessonMaterialId == lessonMaterialId)), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(lessonMaterial.Status, Is.EqualTo(EntityStatus.Active));
            });
        }

        [Test]
        public async Task Handle_Should_Restore_LessonMaterial_And_Not_Add_To_Folder_If_Exists()
        {
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var cmd = new RestoreLessonMaterialCommand
            {
                PersonalFolderId = folderId,
                CurrentUserId = userId,
                LessonMaterialIds = new List<Guid> { lessonMaterialId }
            };

            var user = new ApplicationUser { Id = userId };
            var folder = new Folder { Id = folderId, OwnerType = OwnerType.Personal, Status = EntityStatus.Active, UserId = userId };
            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, Status = EntityStatus.Deleted, CreatedByUserId = userId };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _lessonMaterialRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<LessonMaterial> { lessonMaterial });
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Teacher" });

            _lessonMaterialRepoMock.Setup(r => r.Update(lessonMaterial));
            var existingRelation = new FolderLessonMaterial { FolderId = folderId, LessonMaterialId = lessonMaterialId };
            _folderLessonMaterialRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<FolderLessonMaterial> { existingRelation });
            _folderLessonMaterialRepoMock.Setup(r => r.Remove(existingRelation));
            _folderLessonMaterialRepoMock.Setup(r => r.AddAsync(It.IsAny<FolderLessonMaterial>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(0);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            _lessonMaterialRepoMock.Verify(r => r.Update(lessonMaterial), Times.Once);
            _folderLessonMaterialRepoMock.Verify(r => r.Remove(existingRelation), Times.Once);
            _folderLessonMaterialRepoMock.Verify(r => r.AddAsync(It.Is<FolderLessonMaterial>(flm => flm.FolderId == folderId && flm.LessonMaterialId == lessonMaterialId)), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(lessonMaterial.Status, Is.EqualTo(EntityStatus.Active));
            });
        }

        [Test]
        public void Handle_Should_Throw_AppException_On_Unexpected_Exception()
        {
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var lessonMaterialId = Guid.NewGuid();
            var cmd = new RestoreLessonMaterialCommand
            {
                PersonalFolderId = folderId,
                CurrentUserId = userId,
                LessonMaterialIds = new List<Guid> { lessonMaterialId }
            };

            var user = new ApplicationUser { Id = userId };
            var folder = new Folder { Id = folderId, OwnerType = OwnerType.Personal, Status = EntityStatus.Active, UserId = userId };
            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, Status = EntityStatus.Deleted, CreatedByUserId = userId };

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _lessonMaterialRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<LessonMaterial> { lessonMaterial });
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Teacher" });

            _lessonMaterialRepoMock.Setup(r => r.Update(lessonMaterial)).Throws(new Exception("Unexpected"));

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(cmd, CancellationToken.None));
        }
    }
}