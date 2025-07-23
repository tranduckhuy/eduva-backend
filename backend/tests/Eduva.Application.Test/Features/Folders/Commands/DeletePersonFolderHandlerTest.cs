using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Folders.Commands;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.Folders.Commands
{
    [TestFixture]
    public class DeletePersonFolderHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IFolderRepository> _folderRepoMock = null!;
        private Mock<ILessonMaterialRepository> _lessonMaterialRepoMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepoMock = null!;
        private Mock<IGenericRepository<FolderLessonMaterial, int>> _folderLessonMaterialRepoMock = null!;
        private DeletePersonFolderHandler _handler = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _folderRepoMock = new Mock<IFolderRepository>();
            _lessonMaterialRepoMock = new Mock<ILessonMaterialRepository>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
            _userRepoMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            _folderLessonMaterialRepoMock = new Mock<IGenericRepository<FolderLessonMaterial, int>>();

            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>()).Returns(_userRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<FolderLessonMaterial, int>()).Returns(_folderLessonMaterialRepoMock.Object);

            _handler = new DeletePersonFolderHandler(
                _unitOfWorkMock.Object,
                _folderRepoMock.Object,
                _lessonMaterialRepoMock.Object,
                _userManagerMock.Object
            );
        }

        [Test]
        public async Task Handle_Should_Delete_All_Archived_Personal_Folders_When_FolderIds_Empty()
        {
            var userId = Guid.NewGuid();
            var archivedFolder = new Folder
            {
                Id = Guid.NewGuid(),
                OwnerType = OwnerType.Personal,
                Status = EntityStatus.Archived,
                UserId = userId
            };
            var command = new DeletePersonFolderCommand
            {
                FolderIds = new List<Guid>(),
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.ListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Folder> { archivedFolder });

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId });
            _userManagerMock.Setup(m => m.IsInRoleAsync(It.IsAny<ApplicationUser>(), Role.SystemAdmin.ToString())).ReturnsAsync(false);

            _folderRepoMock.Setup(r => r.GetFolderWithMaterialsAsync(archivedFolder.Id)).ReturnsAsync(archivedFolder);
            _folderLessonMaterialRepoMock.Setup(r => r.RemoveRange(It.IsAny<IEnumerable<FolderLessonMaterial>>()));
            _folderRepoMock.Setup(r => r.Remove(archivedFolder));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.That(result, Is.True);
            _folderRepoMock.Verify(r => r.Remove(archivedFolder), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Test]
        public void Handle_Should_Throw_When_Folder_Is_Not_Personal()
        {
            var userId = Guid.NewGuid();
            var classFolder = new Folder
            {
                Id = Guid.NewGuid(),
                OwnerType = OwnerType.Class,
                Status = EntityStatus.Archived,
                UserId = userId
            };
            var command = new DeletePersonFolderCommand
            {
                FolderIds = new List<Guid> { classFolder.Id },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.ListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Folder> { classFolder });

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Test]
        public async Task Handle_Should_Skip_Deleted_Folder()
        {
            var userId = Guid.NewGuid();
            var deletedFolder = new Folder
            {
                Id = Guid.NewGuid(),
                OwnerType = OwnerType.Personal,
                Status = EntityStatus.Deleted,
                UserId = userId
            };
            var command = new DeletePersonFolderCommand
            {
                FolderIds = new List<Guid> { deletedFolder.Id },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.ListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Folder> { deletedFolder });

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId });
            _userManagerMock.Setup(m => m.IsInRoleAsync(It.IsAny<ApplicationUser>(), Role.SystemAdmin.ToString())).ReturnsAsync(false);

            _folderRepoMock.Setup(r => r.GetFolderWithMaterialsAsync(deletedFolder.Id)).ReturnsAsync(deletedFolder);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.That(result, Is.True);
            _folderRepoMock.Verify(r => r.Remove(It.IsAny<Folder>()), Times.Never);
        }

        [Test]
        public void Handle_Should_Throw_When_Folder_Not_Archived()
        {
            var userId = Guid.NewGuid();
            var activeFolder = new Folder
            {
                Id = Guid.NewGuid(),
                OwnerType = OwnerType.Personal,
                Status = EntityStatus.Active,
                UserId = userId
            };
            var command = new DeletePersonFolderCommand
            {
                FolderIds = new List<Guid> { activeFolder.Id },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.ListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Folder> { activeFolder });

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId });
            _userManagerMock.Setup(m => m.IsInRoleAsync(It.IsAny<ApplicationUser>(), Role.SystemAdmin.ToString())).ReturnsAsync(false);

            _folderRepoMock.Setup(r => r.GetFolderWithMaterialsAsync(activeFolder.Id)).ReturnsAsync(activeFolder);

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Test]
        public void Handle_Should_Throw_When_User_Has_No_Permission()
        {
            var userId = Guid.NewGuid();
            var folder = new Folder
            {
                Id = Guid.NewGuid(),
                OwnerType = OwnerType.Personal,
                Status = EntityStatus.Archived,
                UserId = Guid.NewGuid()
            };
            var command = new DeletePersonFolderCommand
            {
                FolderIds = new List<Guid> { folder.Id },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.ListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Folder> { folder });

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId });
            _userManagerMock.Setup(m => m.IsInRoleAsync(It.IsAny<ApplicationUser>(), Role.SystemAdmin.ToString())).ReturnsAsync(false);

            _folderRepoMock.Setup(r => r.GetFolderWithMaterialsAsync(folder.Id)).ReturnsAsync(folder);

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Test]
        public async Task Handle_Should_Delete_LessonMaterial_If_CreatedByUser_And_Not_Deleted()
        {
            var userId = Guid.NewGuid();
            var lessonMaterial = new LessonMaterial
            {
                Id = Guid.NewGuid(),
                CreatedByUserId = userId,
                Status = EntityStatus.Active
            };
            var folder = new Folder
            {
                Id = Guid.NewGuid(),
                OwnerType = OwnerType.Personal,
                Status = EntityStatus.Archived,
                UserId = userId,
                FolderLessonMaterials = new List<FolderLessonMaterial>
                {
                    new FolderLessonMaterial { LessonMaterial = lessonMaterial }
                }
            };
            var command = new DeletePersonFolderCommand
            {
                FolderIds = new List<Guid> { folder.Id },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.ListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Folder> { folder });

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId });
            _userManagerMock.Setup(m => m.IsInRoleAsync(It.IsAny<ApplicationUser>(), Role.SystemAdmin.ToString())).ReturnsAsync(false);

            _folderRepoMock.Setup(r => r.GetFolderWithMaterialsAsync(folder.Id)).ReturnsAsync(folder);
            _lessonMaterialRepoMock.Setup(r => r.Update(It.IsAny<LessonMaterial>()));
            _folderLessonMaterialRepoMock.Setup(r => r.RemoveRange(It.IsAny<IEnumerable<FolderLessonMaterial>>()));
            _folderRepoMock.Setup(r => r.Remove(folder));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.That(result, Is.True);
            _lessonMaterialRepoMock.Verify(r => r.Update(It.Is<LessonMaterial>(lm => lm.Status == EntityStatus.Deleted)), Times.Once);
        }

        [Test]
        public async Task Handle_Should_Not_Update_LessonMaterial_If_Not_CreatedByUser_Or_Already_Deleted()
        {
            var userId = Guid.NewGuid();
            var lessonMaterial = new LessonMaterial
            {
                Id = Guid.NewGuid(),
                CreatedByUserId = Guid.NewGuid(),
                Status = EntityStatus.Deleted
            };
            var folder = new Folder
            {
                Id = Guid.NewGuid(),
                OwnerType = OwnerType.Personal,
                Status = EntityStatus.Archived,
                UserId = userId,
                FolderLessonMaterials = new List<FolderLessonMaterial>
                {
                    new FolderLessonMaterial { LessonMaterial = lessonMaterial }
                }
            };
            var command = new DeletePersonFolderCommand
            {
                FolderIds = new List<Guid> { folder.Id },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.ListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Folder> { folder });

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId });
            _userManagerMock.Setup(m => m.IsInRoleAsync(It.IsAny<ApplicationUser>(), Role.SystemAdmin.ToString())).ReturnsAsync(false);

            _folderRepoMock.Setup(r => r.GetFolderWithMaterialsAsync(folder.Id)).ReturnsAsync(folder);
            _lessonMaterialRepoMock.Setup(r => r.Update(It.IsAny<LessonMaterial>()));
            _folderLessonMaterialRepoMock.Setup(r => r.RemoveRange(It.IsAny<IEnumerable<FolderLessonMaterial>>()));
            _folderRepoMock.Setup(r => r.Remove(folder));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.That(result, Is.True);
            _lessonMaterialRepoMock.Verify(r => r.Update(It.IsAny<LessonMaterial>()), Times.Never);
        }

        [Test]
        public async Task Handle_Should_Allow_SystemAdmin_To_Delete_Any_Personal_Folder()
        {
            var userId = Guid.NewGuid();
            var folder = new Folder
            {
                Id = Guid.NewGuid(),
                OwnerType = OwnerType.Personal,
                Status = EntityStatus.Archived,
                UserId = Guid.NewGuid()
            };
            var command = new DeletePersonFolderCommand
            {
                FolderIds = new List<Guid> { folder.Id },
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.ListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Folder> { folder });

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId });
            _userManagerMock.Setup(m => m.IsInRoleAsync(It.IsAny<ApplicationUser>(), Role.SystemAdmin.ToString())).ReturnsAsync(true);

            _folderRepoMock.Setup(r => r.GetFolderWithMaterialsAsync(folder.Id)).ReturnsAsync(folder);
            _folderLessonMaterialRepoMock.Setup(r => r.RemoveRange(It.IsAny<IEnumerable<FolderLessonMaterial>>()));
            _folderRepoMock.Setup(r => r.Remove(folder));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.That(result, Is.True);
            _folderRepoMock.Verify(r => r.Remove(folder), Times.Once);
        }

        [Test]
        public void Handle_Should_Throw_AppException_On_Repository_Exception()
        {
            var userId = Guid.NewGuid();
            var command = new DeletePersonFolderCommand
            {
                FolderIds = new List<Guid>(),
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.ListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("db error"));

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Test]
        public async Task Handle_Should_Return_True_When_No_Archived_Personal_Folder_Found()
        {
            var userId = Guid.NewGuid();
            var command = new DeletePersonFolderCommand
            {
                FolderIds = new List<Guid>(),
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.ListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Folder>());

            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.That(result, Is.True);
            _folderRepoMock.Verify(r => r.Remove(It.IsAny<Folder>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        }
    }
}