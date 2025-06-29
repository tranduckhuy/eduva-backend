using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Folders.Commands;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Moq;
using Microsoft.Extensions.Logging;

namespace Eduva.Application.Test.Features.Folders.Commands
{
    [TestFixture]
    public class RenameFolderHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = default!;
        private Mock<IGenericRepository<Folder, Guid>> _folderRepoMock = default!;
        private Mock<IGenericRepository<Classroom, Guid>> _classRepoMock = default!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepoMock = default!;
        private Mock<ILogger<RenameFolderHandler>> _loggerMock = default!;
        private RenameFolderHandler _handler = default!;

        #region RenameFolderHandler Setup
        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _folderRepoMock = new Mock<IGenericRepository<Folder, Guid>>();
            _classRepoMock = new Mock<IGenericRepository<Classroom, Guid>>();
            _userRepoMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            _loggerMock = new Mock<ILogger<RenameFolderHandler>>();

            _unitOfWorkMock.Setup(u => u.GetRepository<Folder, Guid>()).Returns(_folderRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>()).Returns(_classRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>()).Returns(_userRepoMock.Object);

            _handler = new RenameFolderHandler(_unitOfWorkMock.Object, _loggerMock.Object);
        }
        #endregion

        #region RenameFolderHandler Tests

        [Test]
        public async Task Handle_Rename_Success()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var cmd = new RenameFolderCommand { Id = folderId, Name = "NewName", CurrentUserId = userId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(new Folder { Id = folderId, Name = "OldName", Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId });
            _folderRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>())).ReturnsAsync(false);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            await _handler.Handle(cmd, CancellationToken.None);
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Name == "NewName")), Times.Once);
        }

        [Test]
        public void Handle_Throws_When_Folder_Not_Found()
        {
            var cmd = new RenameFolderCommand { Id = Guid.NewGuid(), Name = "Name", CurrentUserId = Guid.NewGuid() };
            _folderRepoMock.Setup(r => r.GetByIdAsync(cmd.Id)).ReturnsAsync((Folder)null!);
            Assert.ThrowsAsync<AppException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public void Handle_Throws_When_Duplicate_Folder_Name_Exists()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var cmd = new RenameFolderCommand { Id = folderId, Name = "DupName", CurrentUserId = userId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(new Folder { Id = folderId, Name = "OldName", Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId });
            _folderRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>())).ReturnsAsync(true);
            Assert.ThrowsAsync<AppException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public void Handle_Throws_When_User_Does_Not_Own_Folder()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var cmd = new RenameFolderCommand { Id = folderId, Name = "NewName", CurrentUserId = userId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(new Folder { Id = folderId, Name = "OldName", Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = otherUserId });
            Assert.Throws<AppException>(() => _handler.Handle(cmd, CancellationToken.None).GetAwaiter().GetResult());
        }

        [Test]
        public void Handle_Throws_When_User_Is_Not_Teacher_For_Class_Folder()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var cmd = new RenameFolderCommand { Id = folderId, Name = "NewName", CurrentUserId = userId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(new Folder { Id = folderId, Name = "OldName", Status = EntityStatus.Active, OwnerType = OwnerType.Class, ClassId = classId });
            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(new Classroom { Id = classId, TeacherId = Guid.NewGuid() });
            Assert.ThrowsAsync<AppException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public void Handle_Throws_When_Classroom_Not_Found_For_Class_Folder()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var cmd = new RenameFolderCommand { Id = folderId, Name = "NewName", CurrentUserId = userId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(new Folder { Id = folderId, Name = "OldName", Status = EntityStatus.Active, OwnerType = OwnerType.Class, ClassId = classId });
            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync((Classroom)null!);
            Assert.Throws<AppException>(() => _handler.Handle(cmd, CancellationToken.None).GetAwaiter().GetResult());
        }

        [Test]
        public void Handle_Throws_When_Folder_Status_Is_Not_Active()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var cmd = new RenameFolderCommand { Id = folderId, Name = "NewName", CurrentUserId = userId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(new Folder { Id = folderId, Name = "OldName", Status = EntityStatus.Deleted, OwnerType = OwnerType.Personal, UserId = userId });
            Assert.ThrowsAsync<AppException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public void Handle_Throws_FolderUpdateFailed_When_Exception_Occurs()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var cmd = new RenameFolderCommand { Id = folderId, Name = "NewName", CurrentUserId = userId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(new Folder { Id = folderId, Name = "OldName", Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId });
            _folderRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>())).ReturnsAsync(false);
            _folderRepoMock.Setup(r => r.Update(It.IsAny<Folder>())).Throws(new Exception("DB error"));
            _unitOfWorkMock.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);
            var ex = Assert.Throws<AppException>(() => _handler.Handle(cmd, CancellationToken.None).GetAwaiter().GetResult());
            Assert.That(ex?.StatusCode, Is.EqualTo(CustomCode.FolderUpdateFailed));
        }

        [Test]
        public void Handle_When_Exception_Occurs_Performs_Rollback_And_Throws_AppException()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var cmd = new RenameFolderCommand { Id = folderId, Name = "NewName", CurrentUserId = userId };
            var folder = new Folder { Id = folderId, Name = "OldName", Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _folderRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>())).ReturnsAsync(false);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ThrowsAsync(new Exception("DB error"));
            _unitOfWorkMock.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);

            var ex = Assert.Throws<AppException>(() => _handler.Handle(cmd, CancellationToken.None).GetAwaiter().GetResult());
            Assert.That(ex?.StatusCode, Is.EqualTo(CustomCode.FolderUpdateFailed));
            _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
            #pragma warning disable CS8602 
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("Failed to rename folder")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.Once
            );
            #pragma warning restore CS8602
        }

        [Test]
        public async Task Handle_Rename_Succeeds_When_User_Is_Teacher_For_Class_Folder()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var cmd = new RenameFolderCommand { Id = folderId, Name = "NewClassName", CurrentUserId = userId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(new Folder { Id = folderId, Name = "OldClassName", Status = EntityStatus.Active, OwnerType = OwnerType.Class, ClassId = classId });
            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(new Classroom { Id = classId, TeacherId = userId });
            _folderRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>())).ReturnsAsync(false);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            await _handler.Handle(cmd, CancellationToken.None);
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Name == "NewClassName")), Times.Once);
        }

        [Test]
        public async Task Handle_Rename_Succeeds_When_User_Is_Admin_For_Class_Folder()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var schoolId = 123;
            var cmd = new RenameFolderCommand { Id = folderId, Name = "AdminClassName", CurrentUserId = userId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(new Folder { Id = folderId, Name = "OldClassName", Status = EntityStatus.Active, OwnerType = OwnerType.Class, ClassId = classId });
            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(new Classroom { Id = classId, TeacherId = Guid.NewGuid(), SchoolId = schoolId });
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId, SchoolId = schoolId });
            _folderRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>())).ReturnsAsync(false);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            await _handler.Handle(cmd, CancellationToken.None);
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Name == "AdminClassName")), Times.Once);
        }

        [Test]
        public async Task Handle_Rename_Succeeds_When_User_Is_Teacher_For_Class_Folder_Branch()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var cmd = new RenameFolderCommand { Id = folderId, Name = "TeacherClassName", CurrentUserId = userId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(new Folder { Id = folderId, Name = "OldClassName", Status = EntityStatus.Active, OwnerType = OwnerType.Class, ClassId = classId });
            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(new Classroom { Id = classId, TeacherId = userId });
            _folderRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>())).ReturnsAsync(false);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            await _handler.Handle(cmd, CancellationToken.None);
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Name == "TeacherClassName")), Times.Once);
        }

        [Test]
        public void Handle_Throws_When_User_SchoolId_Does_Not_Match_Classroom_SchoolId()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var cmd = new RenameFolderCommand { Id = folderId, Name = "NoAdmin", CurrentUserId = userId };
            var classroom = new Classroom { Id = classId, TeacherId = Guid.NewGuid(), SchoolId = 999 };
            var user = new ApplicationUser { Id = userId, SchoolId = 123 };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(new Folder { Id = folderId, Name = "OldClassName", Status = EntityStatus.Active, OwnerType = OwnerType.Class, ClassId = classId });
            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
            Assert.Throws<AppException>(() => _handler.Handle(cmd, CancellationToken.None).GetAwaiter().GetResult());
        }

        #endregion
    }
}
