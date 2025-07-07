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

        [Test]
        public async Task Handle_Only_Updates_Folders_With_Different_Id_And_Affected_Order()
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

            // folder2 and folder3 should be updated, folder1 is the target
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folder2.Id)), Times.Once);
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folder3.Id)), Times.Once);
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folder1.Id)), Times.Once);
        }

        [Test]
        public async Task Handle_Shifts_Folders_Down_Only_Those_In_Range()
        {
            var userId = Guid.NewGuid();
            var folder1 = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId, Order = 1 };
            var folder2 = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId, Order = 2 };
            var folder3 = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId, Order = 3 };
            var folder4 = new Folder { Id = Guid.NewGuid(), Status = EntityStatus.Active, OwnerType = OwnerType.Personal, UserId = userId, Order = 4 };
            var cmd = new UpdateFolderOrderCommand { Id = folder1.Id, Order = 3, CurrentUserId = userId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folder1.Id)).ReturnsAsync(folder1);
            _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { folder1, folder2, folder3, folder4 });
            _folderRepoMock.Setup(r => r.Update(It.IsAny<Folder>()));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            await _handler.Handle(cmd, CancellationToken.None);

            // Only folder2 and folder3 should be updated (shifted up), folder4 is out of range
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folder2.Id && f.Order == 1)), Times.Once);
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folder3.Id && f.Order == 2)), Times.Once);
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folder4.Id)), Times.Never);
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folder1.Id && f.Order == 3)), Times.Once);
        }

        [Test]
        public void Handle_Throws_When_ClassFolder_Has_No_ClassId()
        {
            var teacherId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var folder = new Folder
            {
                Id = folderId,
                Status = EntityStatus.Active,
                OwnerType = OwnerType.Class,
                UserId = teacherId,
                Order = 1,
                ClassId = null // No ClassId
            };
            var cmd = new UpdateFolderOrderCommand { Id = folderId, Order = 2, CurrentUserId = teacherId };
            _folderRepoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);

            Assert.ThrowsAsync<Common.Exceptions.AppException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public async Task Handle_Filters_Only_Active_Folders_With_SameScope()
        {
            var userId = Guid.NewGuid();
            var targetFolder = new Folder
            {
                Id = Guid.NewGuid(),
                Status = EntityStatus.Active,
                OwnerType = OwnerType.Personal,
                UserId = userId,
                Order = 1
            };

            var unrelatedFolder = new Folder
            {
                Id = Guid.NewGuid(),
                Status = EntityStatus.Active,
                OwnerType = OwnerType.Personal,
                UserId = Guid.NewGuid(), // khác user
                Order = 2
            };

            var inactiveFolder = new Folder
            {
                Id = Guid.NewGuid(),
                Status = EntityStatus.Inactive,
                OwnerType = OwnerType.Personal,
                UserId = userId,
                Order = 2
            };

            var sameScopeFolder = new Folder
            {
                Id = Guid.NewGuid(),
                Status = EntityStatus.Active,
                OwnerType = OwnerType.Personal,
                UserId = userId,
                Order = 2
            };

            var cmd = new UpdateFolderOrderCommand
            {
                Id = targetFolder.Id,
                Order = 2,
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(targetFolder.Id)).ReturnsAsync(targetFolder);
            _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { targetFolder, unrelatedFolder, inactiveFolder, sameScopeFolder });
            _folderRepoMock.Setup(r => r.Update(It.IsAny<Folder>()));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            await _handler.Handle(cmd, CancellationToken.None);

            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == sameScopeFolder.Id && f.Order == 1)), Times.Once);
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == unrelatedFolder.Id)), Times.Never);
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == inactiveFolder.Id)), Times.Never);
        }


        [Test]
        public async Task Handle_Shifts_Folders_Down_Correctly_When_Moving_Down()
        {
            var userId = Guid.NewGuid();

            var folder1 = new Folder { Id = Guid.NewGuid(), Order = 1, Status = EntityStatus.Active, UserId = userId, OwnerType = OwnerType.Personal };
            var folder2 = new Folder { Id = Guid.NewGuid(), Order = 2, Status = EntityStatus.Active, UserId = userId, OwnerType = OwnerType.Personal };
            var folder3 = new Folder { Id = Guid.NewGuid(), Order = 3, Status = EntityStatus.Active, UserId = userId, OwnerType = OwnerType.Personal };

            var cmd = new UpdateFolderOrderCommand
            {
                Id = folder1.Id,
                Order = 3,
                CurrentUserId = userId
            };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folder1.Id)).ReturnsAsync(folder1);
            _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { folder1, folder2, folder3 });
            _folderRepoMock.Setup(r => r.Update(It.IsAny<Folder>()));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            await _handler.Handle(cmd, CancellationToken.None);

            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folder2.Id && f.Order == 1)), Times.Once);
            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folder3.Id && f.Order == 2)), Times.Once);
        }

        [Test]
        public async Task Handle_PersonalFolder_Allows_Owner_To_Update()
        {
            var userId = Guid.NewGuid();
            var folder = new Folder
            {
                Id = Guid.NewGuid(),
                OwnerType = OwnerType.Personal,
                UserId = userId,
                Status = EntityStatus.Active,
                Order = 1
            };

            var cmd = new UpdateFolderOrderCommand { Id = folder.Id, Order = 2, CurrentUserId = userId };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folder.Id)).ReturnsAsync(folder);
            _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { folder });
            _folderRepoMock.Setup(r => r.Update(It.IsAny<Folder>()));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            await _handler.Handle(cmd, CancellationToken.None);

            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folder.Id && f.Order == 2)), Times.Once);
        }

        [Test]
        public async Task Handle_ClassFolder_With_Valid_ClassId_Calls_ClassRepo()
        {
            var teacherId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var folder = new Folder
            {
                Id = Guid.NewGuid(),
                OwnerType = OwnerType.Class,
                UserId = teacherId,
                ClassId = classId,
                Status = EntityStatus.Active,
                Order = 1
            };

            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = teacherId
            };

            var cmd = new UpdateFolderOrderCommand { Id = folder.Id, Order = 2, CurrentUserId = teacherId };

            _folderRepoMock.Setup(r => r.GetByIdAsync(folder.Id)).ReturnsAsync(folder);
            _classroomRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);
            _folderRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new[] { folder });
            _folderRepoMock.Setup(r => r.Update(It.IsAny<Folder>()));
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            await _handler.Handle(cmd, CancellationToken.None);

            _folderRepoMock.Verify(r => r.Update(It.Is<Folder>(f => f.Id == folder.Id && f.Order == 2)), Times.Once);
            _classroomRepoMock.Verify(r => r.GetByIdAsync(classId), Times.Once);
        }

        #endregion

        [Test]
        public async Task Handle_Should_Decrement_Order_Of_Folders_Between_Original_And_New_Order()
        {
            var userId = Guid.NewGuid();
            var folderId = Guid.NewGuid();

            var folder = new Folder
            {
                Id = folderId,
                Status = EntityStatus.Active,
                OwnerType = OwnerType.Personal,
                UserId = userId,
                Order = 1
            };

            var folder2 = new Folder
            {
                Id = Guid.NewGuid(),
                Status = EntityStatus.Active,
                OwnerType = OwnerType.Personal,
                UserId = userId,
                Order = 2
            };

            var folder3 = new Folder
            {
                Id = Guid.NewGuid(),
                Status = EntityStatus.Active,
                OwnerType = OwnerType.Personal,
                UserId = userId,
                Order = 3
            };

            var repoMock = new Mock<IGenericRepository<Folder, Guid>>();
            repoMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
            repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Folder> { folder, folder2, folder3 });
            repoMock.Setup(r => r.Update(It.IsAny<Folder>()));

            var unitOfWorkMock = new Mock<IUnitOfWork>();
            unitOfWorkMock.Setup(u => u.GetRepository<Folder, Guid>()).Returns(repoMock.Object);
            unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var loggerMock = new Mock<ILogger<UpdateFolderOrderHandler>>();
            var handler = new UpdateFolderOrderHandler(unitOfWorkMock.Object, loggerMock.Object);

            var command = new UpdateFolderOrderCommand
            {
                Id = folderId,
                CurrentUserId = userId,
                Order = 3
            };

            await handler.Handle(command, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(folder2.Order, Is.EqualTo(1));
                Assert.That(folder3.Order, Is.EqualTo(2));
                Assert.That(folder.Order, Is.EqualTo(3));
            });
        }


        [Test]
        public async Task HasPermissionToUpdateFolder_Should_Return_True_For_Personal_Owner()
        {
            var userId = Guid.NewGuid();
            var folder = new Folder
            {
                Id = Guid.NewGuid(),
                OwnerType = OwnerType.Personal,
                UserId = userId
            };

            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var loggerMock = new Mock<ILogger<UpdateFolderOrderHandler>>();
            var handler = new UpdateFolderOrderHandler(unitOfWorkMock.Object, loggerMock.Object);

            var result = handler.GetType()
            .GetMethod("HasPermissionToUpdateFolder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(handler, new object[] { folder, userId });

            Assert.That(await (Task<bool>)result!, Is.True);

        }

        [Test]
        public async Task HasPermissionToUpdateFolder_Should_Return_True_For_ClassTeacher()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var folder = new Folder
            {
                Id = Guid.NewGuid(),
                OwnerType = OwnerType.Class,
                ClassId = classId
            };

            var classroom = new Classroom
            {
                Id = classId,
                TeacherId = userId,
                SchoolId = 1
            };

            var classRepoMock = new Mock<IGenericRepository<Classroom, Guid>>();
            classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(classroom);

            var unitOfWorkMock = new Mock<IUnitOfWork>();
            unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>()).Returns(classRepoMock.Object);

            var loggerMock = new Mock<ILogger<UpdateFolderOrderHandler>>();
            var handler = new UpdateFolderOrderHandler(unitOfWorkMock.Object, loggerMock.Object);

            var result = handler.GetType()
            .GetMethod("HasPermissionToUpdateFolder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(handler, new object[] { folder, userId });

            Assert.That(await (Task<bool>)result!, Is.True);
        }
    }
}
