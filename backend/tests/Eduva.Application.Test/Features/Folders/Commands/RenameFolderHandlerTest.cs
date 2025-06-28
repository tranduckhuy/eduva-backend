using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Folders.Commands;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Moq;

namespace Eduva.Application.Test.Features.Folders.Commands
{
    [TestFixture]
    public class RenameFolderHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = default!;
        private Mock<IGenericRepository<Folder, Guid>> _folderRepoMock = default!;
        private Mock<IGenericRepository<Classroom, Guid>> _classRepoMock = default!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepoMock = default!;
        private RenameFolderHandler _handler = default!;

        #region RenameFolderHandler Setup
        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _folderRepoMock = new Mock<IGenericRepository<Folder, Guid>>();
            _classRepoMock = new Mock<IGenericRepository<Classroom, Guid>>();
            _userRepoMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();

            _unitOfWorkMock.Setup(u => u.GetRepository<Folder, Guid>()).Returns(_folderRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>()).Returns(_classRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>()).Returns(_userRepoMock.Object);

            _handler = new RenameFolderHandler(_unitOfWorkMock.Object, null!);
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

        #endregion
    }
}
