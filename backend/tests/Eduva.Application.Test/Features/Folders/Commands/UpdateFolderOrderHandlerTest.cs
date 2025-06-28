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
        private UpdateFolderOrderHandler _handler = default!;

        #region UpdateFolderOrderHandler Setup
        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _folderRepoMock = new Mock<IGenericRepository<Folder, Guid>>();
            _loggerMock = new Mock<ILogger<UpdateFolderOrderHandler>>();
            _unitOfWorkMock.Setup(u => u.GetRepository<Folder, Guid>()).Returns(_folderRepoMock.Object);
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

        #endregion
    }
}
