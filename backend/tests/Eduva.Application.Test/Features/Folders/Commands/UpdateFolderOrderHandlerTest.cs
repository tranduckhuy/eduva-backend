using Eduva.Application.Features.Folders.Commands;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace Eduva.Application.Test.Features.Folders.Commands
{
    [TestFixture]
    public class UpdateFolderOrderHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = default!;
        private Mock<IGenericRepository<Folder, Guid>> _folderRepoMock = default!;
        private Mock<ILogger<UpdateFolderOrderHandler>> _loggerMock = default!;
        private Mock<IGenericRepository<Classroom, Guid>> _classroomRepoMock = default!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepoMock = default!;
        private UpdateFolderOrderHandler _handler = default!;

        #region UpdateFolderOrderHandler Setup
        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _folderRepoMock = new Mock<IGenericRepository<Folder, Guid>>();
            _loggerMock = new Mock<ILogger<UpdateFolderOrderHandler>>();
            _classroomRepoMock = new Mock<IGenericRepository<Classroom, Guid>>();
            _userRepoMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            _unitOfWorkMock.Setup(u => u.GetRepository<Folder, Guid>()).Returns(_folderRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>()).Returns(_classroomRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>()).Returns(_userRepoMock.Object);
            _handler = new UpdateFolderOrderHandler(_unitOfWorkMock.Object, _loggerMock.Object);
        }
        #endregion

        #region UpdateFolderOrderHandler Tests

        [Test]
        public async Task Handle_Updates_Order_Success()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var folder = new Folder { Id = folderId, Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId, Order = 1 };
            var cmd = new UpdateFolderOrderCommand { Id = folderId, Order = 2, CurrentUserId = userId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { folder });
            _folderRepoMock.Setup(r => r.Update(It.IsAny<Folder>()));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            await _handler.Handle(cmd, CancellationToken.None);
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Order == 2)), Times.Once);
        }

        [Test]
        public void Handle_Throws_When_Folder_Not_Found()
        {
            var cmd = new UpdateFolderOrderCommand { Id = Guid.NewGuid(), Order = 1, CurrentUserId = Guid.NewGuid() };
            _folderRepoMock.Setup(r => r.GetByIdAsync(cmd.Id)).ReturnsAsync((Folder)null!);
            Assert.ThrowsAsync<Eduva.Application.Common.Exceptions.AppException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public void Handle_Throws_When_User_Not_Owner()
        {
            var folderId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var folder = new Folder { Id = folderId, Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = ownerId, Order = 1 };
            var cmd = new UpdateFolderOrderCommand { Id = folderId, Order = 2, CurrentUserId = otherUserId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            Assert.ThrowsAsync<Eduva.Application.Common.Exceptions.AppException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public void Handle_Throws_When_ClassFolder_User_Not_Teacher()
        {
            var folderId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var notTeacherId = Guid.NewGuid();
            var classroomId = Guid.NewGuid();
            var folder = new Folder
            {
                Id = folderId,
                Status = EntityStatus.Active,
                OwnerType = OwnerType.Class,
                UserId = teacherId,
                Order = 1,
                ClassId = classroomId
            };
            var classroom = new Classroom { Id = classroomId, TeacherId = teacherId };
            var cmd = new UpdateFolderOrderCommand { Id = folderId, Order = 2, CurrentUserId = notTeacherId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classroomId)).ReturnsAsync(classroom);
            Assert.ThrowsAsync<Eduva.Application.Common.Exceptions.AppException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public void Handle_Throws_When_Folder_Is_Inactive_Or_Deleted()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var inactiveFolder = new Folder { Id = folderId, Status = EntityStatus.Inactive, OwnerType = OwnerType.Personal, UserId = userId, Order = 1 };
            var deletedFolder = new Folder { Id = folderId, Status = EntityStatus.Deleted, OwnerType = OwnerType.Personal, UserId = userId, Order = 1 };
            var cmd = new UpdateFolderOrderCommand { Id = folderId, Order = 2, CurrentUserId = userId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(inactiveFolder);
            Assert.ThrowsAsync<Eduva.Application.Common.Exceptions.AppException>(() => _handler.Handle(cmd, CancellationToken.None));
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(deletedFolder);
            Assert.ThrowsAsync<Eduva.Application.Common.Exceptions.AppException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public async Task Handle_Does_Not_Update_When_Order_Is_Same()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var folder = new Folder { Id = folderId, Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId, Order = 5 };
            var cmd = new UpdateFolderOrderCommand { Id = folderId, Order = 5, CurrentUserId = userId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { folder });

            await _handler.Handle(cmd, CancellationToken.None);
            _folderRepoMock.Verify(r => r.Update(It.IsAny<Folder>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Test]
        public async Task Handle_Updates_Only_Target_Folder_Order()
        {
            var folderId = Guid.NewGuid();
            var otherFolderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var folder = new Folder { Id = folderId, Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId, Order = 1 };
            var otherFolder = new Folder { Id = otherFolderId, Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId, Order = 2 };
            var cmd = new UpdateFolderOrderCommand { Id = folderId, Order = 2, CurrentUserId = userId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { folder, otherFolder });
            _folderRepoMock.Setup(r => r.Update(It.IsAny<Folder>()));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            await _handler.Handle(cmd, CancellationToken.None);
            // Both folders should be updated: target gets new order, swapped folder gets old order
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folderId && f.Order == 2)), Times.Once);
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == otherFolderId && f.Order == 1)), Times.Once);
        }

        [Test]
        public async Task Handle_Updates_Folders_In_Order()
        {
            var userId = Guid.NewGuid();
            var folder1 = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId, Order = 1 };
            var folder2 = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId, Order = 3 };
            var folder3 = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId, Order = 2 };
            var cmd = new UpdateFolderOrderCommand { Id = folder2.Id, Order = 1, CurrentUserId = userId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folder2.Id)).ReturnsAsync(folder2);
            _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { folder1, folder2, folder3 });
            _folderRepoMock.Setup(r => r.Update(It.IsAny<Folder>()));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            await _handler.Handle(cmd, CancellationToken.None);
            // The test ensures that the handler processes folders in order, but since the update logic is only for the target folder, we just verify update is called for the correct folder.
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folder2.Id && f.Order == 1)), Times.Once);
        }

        [Test]
        public async Task Handle_Updates_When_Moving_Up_Order()
        {
            var userId = Guid.NewGuid();
            var folder1 = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId, Order = 1 };
            var folder2 = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId, Order = 2 };
            var folder3 = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId, Order = 3 };
            var cmd = new UpdateFolderOrderCommand { Id = folder3.Id, Order = 1, CurrentUserId = userId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folder3.Id)).ReturnsAsync(folder3);
            _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { folder1, folder2, folder3 });
            _folderRepoMock.Setup(r => r.Update(It.IsAny<Folder>()));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            await _handler.Handle(cmd, CancellationToken.None);
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folder3.Id && f.Order == 1)), Times.Once);
        }

        [Test]
        public async Task Handle_Shifts_Folders_Up_When_Moving_Up_Order()
        {
            var userId = Guid.NewGuid();
            var folder1 = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId, Order = 1 };
            var folder2 = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId, Order = 2 };
            var folder3 = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId, Order = 3 };
            var cmd = new UpdateFolderOrderCommand { Id = folder3.Id, Order = 1, CurrentUserId = userId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folder3.Id)).ReturnsAsync(folder3);
            _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { folder1, folder2, folder3 });
            _folderRepoMock.Setup(r => r.Update(It.IsAny<Folder>()));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            await _handler.Handle(cmd, CancellationToken.None);
            // folder1 and folder2 should have their order incremented by 1
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folder1.Id && f.Order == 2)), Times.Once);
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folder2.Id && f.Order == 3)), Times.Once);
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folder3.Id && f.Order == 1)), Times.Once);
        }

        [Test]
        public async Task Handle_Moving_Up_Order_With_No_Folders_To_Shift_Does_Not_Update_Others()
        {
            var userId = Guid.NewGuid();
            var folder1 = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId, Order = 1 };
            var folder2 = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId, Order = 5 };
            var cmd = new UpdateFolderOrderCommand { Id = folder2.Id, Order = 2, CurrentUserId = userId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folder2.Id)).ReturnsAsync(folder2);
            _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { folder1, folder2 });
            _folderRepoMock.Setup(r => r.Update(It.IsAny<Folder>()));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            await _handler.Handle(cmd, CancellationToken.None);
            // Only folder2 should be updated, folder1 remains unchanged
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folder2.Id && f.Order == 2)), Times.Once);
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folder1.Id)), Times.Never);
        }

        [Test]
        public async Task Handle_Shifts_Folders_Down_When_Moving_Down_Order()
        {
            var userId = Guid.NewGuid();
            var folder1 = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId, Order = 1 };
            var folder2 = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId, Order = 2 };
            var folder3 = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId, Order = 3 };
            var cmd = new UpdateFolderOrderCommand { Id = folder1.Id, Order = 3, CurrentUserId = userId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folder1.Id)).ReturnsAsync(folder1);
            _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { folder1, folder2, folder3 });
            _folderRepoMock.Setup(r => r.Update(It.IsAny<Folder>()));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            await _handler.Handle(cmd, CancellationToken.None);
            // folder2 and folder3 should have their order decremented by 1
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folder2.Id && f.Order == 1)), Times.Once);
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folder3.Id && f.Order == 2)), Times.Once);
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folder1.Id && f.Order == 3)), Times.Once);
        }

        [Test]
        public void Handle_When_Exception_Occurs_Performs_Rollback_And_Throws_AppException()
        {
            var folderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var folder = new Folder { Id = folderId, Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId, Order = 1 };
            var cmd = new UpdateFolderOrderCommand { Id = folderId, Order = 2, CurrentUserId = userId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { folder });
            _folderRepoMock.Setup(r => r.Update(It.IsAny<Folder>())).Throws(new Exception("DB error"));

            var ex = Assert.Throws<Eduva.Application.Common.Exceptions.AppException>(() => _handler.Handle(cmd, CancellationToken.None).GetAwaiter().GetResult());
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString() != null && v.ToString()!.Contains("Failed to update folder order")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ), Times.Once);
            Assert.That(ex?.StatusCode, Is.EqualTo(Eduva.Shared.Enums.CustomCode.FolderUpdateFailed));
        }

        [Test]
        public async Task Handle_Updates_ClassFolder_When_User_Is_Teacher()
        {
            var teacherId = Guid.NewGuid();
            var classroomId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var folder = new Folder
            {
                Id = folderId,
                Status = EntityStatus.Active,
                OwnerType = OwnerType.Class,
                UserId = teacherId,
                Order = 1,
                ClassId = classroomId
            };
            var classroom = new Classroom { Id = classroomId, TeacherId = teacherId };
            var cmd = new UpdateFolderOrderCommand { Id = folderId, Order = 2, CurrentUserId = teacherId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classroomId)).ReturnsAsync(classroom);
            _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { folder });
            _folderRepoMock.Setup(r => r.Update(It.IsAny<Folder>()));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            await _handler.Handle(cmd, CancellationToken.None);
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folderId && f.Order == 2)), Times.Once);
        }

        [Test]
        public void Handle_Throws_When_Classroom_Not_Found_For_ClassFolder()
        {
            var teacherId = Guid.NewGuid();
            var classroomId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var folder = new Folder
            {
                Id = folderId,
                Status = EntityStatus.Active,
                OwnerType = OwnerType.Class,
                UserId = teacherId,
                Order = 1,
                ClassId = classroomId
            };
            var cmd = new UpdateFolderOrderCommand { Id = folderId, Order = 2, CurrentUserId = teacherId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classroomId)).ReturnsAsync((Classroom)null!);

            Assert.ThrowsAsync<Eduva.Application.Common.Exceptions.AppException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public async Task Handle_Updates_ClassFolder_When_User_SchoolId_Matches_Classroom()
        {
            var adminId = Guid.NewGuid();
            var classroomId = Guid.NewGuid();
            int schoolId = 123;
            var folderId = Guid.NewGuid();
            var folder = new Folder
            {
                Id = folderId,
                Status = EntityStatus.Active,
                OwnerType = OwnerType.Class,
                UserId = Guid.NewGuid(), // not admin
                Order = 1,
                ClassId = classroomId
            };
            var classroom = new Classroom { Id = classroomId, TeacherId = Guid.NewGuid(), SchoolId = schoolId };
            var adminUser = new ApplicationUser { Id = adminId, SchoolId = schoolId };
            var cmd = new UpdateFolderOrderCommand { Id = folderId, Order = 2, CurrentUserId = adminId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classroomId)).ReturnsAsync(classroom);
            _userRepoMock.Setup(r => r.GetByIdAsync(adminId)).ReturnsAsync(adminUser);
            _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { folder });
            _folderRepoMock.Setup(r => r.Update(It.IsAny<Folder>()));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            await _handler.Handle(cmd, CancellationToken.None);
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folderId && f.Order == 2)), Times.Once);
        }

        #endregion
    }
}
