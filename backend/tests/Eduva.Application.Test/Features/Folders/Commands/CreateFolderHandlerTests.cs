using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Folders.Commands;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Shared.Enums;
using Moq;
using System.Linq.Expressions;

namespace Eduva.Application.Test.Features.Folders.Commands
{
    [TestFixture]
    public class CreateFolderHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = default!;
        private Mock<IFolderRepository> _folderRepoMock = default!;
        private Mock<IGenericRepository<Classroom, Guid>> _classRepoMock = default!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepoMock = default!;
        private CreateFolderHandler _handler = default!;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _folderRepoMock = new Mock<IFolderRepository>();
            _classRepoMock = new Mock<IGenericRepository<Classroom, Guid>>();
            _userRepoMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IFolderRepository>())
                .Returns(_folderRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<Classroom, Guid>())
                .Returns(_classRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>())
                .Returns(_userRepoMock.Object);

            _handler = new CreateFolderHandler(_unitOfWorkMock.Object);
        }

        #region CreateFolderHandler Tests

        [Test]
        public void Constructor_ShouldInitialize()
        {
            var handler = new CreateFolderHandler(_unitOfWorkMock.Object);
            Assert.That(handler, Is.Not.Null);
        }

        [Test]
        public async Task Handle_ShouldThrowUserIdNotFound_WhenUserNotExist()
        {
            var cmd = new CreateFolderCommand { CurrentUserId = Guid.NewGuid(), Name = "Test" };
            _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ApplicationUser?)null);
            _folderRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Folder, bool>>>())).ReturnsAsync(false);

            var ex = await TestDelegateWithException<AppException>(() => _handler.Handle(cmd, default));

            Assert.That(ex, Is.Not.Null);
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task Handle_ShouldThrowUserIdNotFound_WhenUserNotExist_WithClassId()
        {
            var cmd = new CreateFolderCommand { CurrentUserId = Guid.NewGuid(), Name = "Test", ClassId = Guid.NewGuid() };
            _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ApplicationUser?)null);
            _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Classroom());
            var ex = await TestDelegateWithException<AppException>(() => _handler.Handle(cmd, default));
            Assert.That(ex, Is.Not.Null, "Expected exception was not thrown.");
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task Handle_ShouldThrowFolderNameAlreadyExists_WhenFolderNameExists()
        {
            var cmd = new CreateFolderCommand { CurrentUserId = Guid.NewGuid(), Name = "Dup" };
            _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new ApplicationUser());
            _folderRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>())).ReturnsAsync(true);
            var ex = await TestDelegateWithException<AppException>(() => _handler.Handle(cmd, default));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.FolderNameAlreadyExists));
        }

        [Test]
        public async Task Handle_ShouldCreatePersonalFolder_WhenValidPersonal()
        {
            var userId = Guid.NewGuid();
            var cmd = new CreateFolderCommand { Name = "Test", CurrentUserId = userId };
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId });
            _folderRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>())).ReturnsAsync(false);
            _folderRepoMock.Setup(r => r.GetMaxOrderAsync(userId, null)).ReturnsAsync(0);
            _folderRepoMock.Setup(r => r.AddAsync(It.IsAny<Folder>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var result = await _handler.Handle(cmd, CancellationToken.None);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo(cmd.Name));
        }

        [Test]
        public async Task Handle_ShouldCreateClassFolder_WhenValidClassAndUserIsTeacher()
        {
            var userId = Guid.NewGuid();
            var classId = Guid.NewGuid();
            var cmd = new CreateFolderCommand { Name = "ClassFolder", CurrentUserId = userId, ClassId = classId };
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId });
            _classRepoMock.Setup(r => r.GetByIdAsync(classId)).ReturnsAsync(new Classroom { Id = classId, TeacherId = userId });
            _folderRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>())).ReturnsAsync(false);
            _folderRepoMock.Setup(r => r.GetMaxOrderAsync(null, classId)).ReturnsAsync(0);
            _folderRepoMock.Setup(r => r.AddAsync(It.IsAny<Folder>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            var result = await _handler.Handle(cmd, CancellationToken.None);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo(cmd.Name));
        }

        [Test]
        public async Task Handle_ShouldThrowFolderCreateFailed_WhenExceptionDuringSave()
        {
            var userId = Guid.NewGuid();
            var cmd = new CreateFolderCommand { Name = "Test", CurrentUserId = userId };
            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new ApplicationUser { Id = userId });
            _folderRepoMock.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>())).ReturnsAsync(false);
            _folderRepoMock.Setup(r => r.GetMaxOrderAsync(userId, null)).ReturnsAsync(0);
            _folderRepoMock.Setup(r => r.AddAsync(It.IsAny<Folder>())).Throws(new Exception());
            _unitOfWorkMock.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);
            var ex = await TestDelegateWithException<AppException>(() => _handler.Handle(cmd, default));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.FolderCreateFailed));
        }

        #endregion

        #region Helper Methods

        private static async Task<TException?> TestDelegateWithException<TException>(Func<Task> testDelegate)
            where TException : Exception
        {
            try
            {
                await testDelegate();
                Assert.Fail("Expected exception was not thrown.");
            }
            catch (TException ex)
            {
                return ex;
            }
            return null;
        }

        #endregion
    }
}
