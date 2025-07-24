using AutoMapper;
using Eduva.Application.Features.Folders.Queries;
using Eduva.Application.Features.Folders.Responses;
using Eduva.Application.Features.Folders.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Moq;

namespace Eduva.Application.Test.Features.Folders.Queries
{
    [TestFixture]
    public class GetAllUserFoldersHandlerTest
    {
        private Mock<IFolderRepository> _folderRepoMock = null!;
        private Mock<IMapper> _mapperMock = null!;
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<ILessonMaterialRepository> _lessonMaterialRepoMock = null!;
        private GetAllUserFoldersHandler _handler = null!;

        [SetUp]
        public void Setup()
        {
            _folderRepoMock = new Mock<IFolderRepository>();
            _mapperMock = new Mock<IMapper>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _lessonMaterialRepoMock = new Mock<ILessonMaterialRepository>();

            _handler = new GetAllUserFoldersHandler(
                _folderRepoMock.Object,
                _mapperMock.Object,
                _unitOfWorkMock.Object
            );
        }

        [Test]
        public async Task Handle_Should_Return_FolderResponses_With_Counts()
        {
            var userId = Guid.NewGuid();
            var param = new FolderSpecParam { UserId = userId, Status = EntityStatus.Active };
            var query = new GetAllUserFoldersQuery(param);

            var folders = new List<Folder>
            {
                new() { Id = Guid.NewGuid(), OwnerType = OwnerType.Personal, UserId = userId, Status = EntityStatus.Active }
            };

            var folderResponses = new List<FolderResponse>
            {
                new() { Id = folders[0].Id }
            };

            var counts = new Dictionary<Guid, int>
            {
                { folders[0].Id, 5 }
            };

            _folderRepoMock.Setup(r => r.ListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(folders);

            _mapperMock.Setup(m => m.Map<List<FolderResponse>>(folders)).Returns(folderResponses);

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<ILessonMaterialRepository>())
                .Returns(_lessonMaterialRepoMock.Object);

            _lessonMaterialRepoMock.Setup(r => r.GetApprovedMaterialCountsByFolderAsync(
                It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(counts);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].CountLessonMaterial, Is.EqualTo(5));
        }

        [Test]
        public async Task Handle_Should_Set_CountLessonMaterial_To_Zero_If_NotInCounts()
        {
            var userId = Guid.NewGuid();
            var param = new FolderSpecParam { UserId = userId, Status = EntityStatus.Active };
            var query = new GetAllUserFoldersQuery(param);

            var folders = new List<Folder>
            {
                new() { Id = Guid.NewGuid(), OwnerType = OwnerType.Personal, UserId = userId, Status = EntityStatus.Active }
            };

            var folderResponses = new List<FolderResponse>
            {
                new() { Id = folders[0].Id }
            };

            var counts = new Dictionary<Guid, int>();

            _folderRepoMock.Setup(r => r.ListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(folders);

            _mapperMock.Setup(m => m.Map<List<FolderResponse>>(folders)).Returns(folderResponses);

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<ILessonMaterialRepository>())
                .Returns(_lessonMaterialRepoMock.Object);

            _lessonMaterialRepoMock.Setup(r => r.GetApprovedMaterialCountsByFolderAsync(
                It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(counts);

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].CountLessonMaterial, Is.EqualTo(0));
        }

        [Test]
        public async Task Handle_Should_Return_EmptyList_If_NoFolders()
        {
            var userId = Guid.NewGuid();
            var param = new FolderSpecParam { UserId = userId };
            var query = new GetAllUserFoldersQuery(param);

            _folderRepoMock.Setup(r => r.ListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Folder>());

            _mapperMock.Setup(m => m.Map<List<FolderResponse>>(It.IsAny<List<Folder>>()))
                .Returns(new List<FolderResponse>());

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task Handle_Should_Return_EmptyList_If_DataIsNull()
        {
            var userId = Guid.NewGuid();
            var param = new FolderSpecParam { UserId = userId };
            var query = new GetAllUserFoldersQuery(param);
            _folderRepoMock.Setup(r => r.ListAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<Folder>?)null!);

            _mapperMock.Setup(m => m.Map<List<FolderResponse>>(It.IsAny<List<Folder>>()))
                .Returns(new List<FolderResponse>());

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task Handle_Should_Not_Call_LessonMaterialRepo_If_NoFolders()
        {
            var userId = Guid.NewGuid();
            var param = new FolderSpecParam { UserId = userId };
            var query = new GetAllUserFoldersQuery(param);

            _folderRepoMock.Setup(r => r.ListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Folder>());

            _mapperMock.Setup(m => m.Map<List<FolderResponse>>(It.IsAny<List<Folder>>()))
                .Returns(new List<FolderResponse>());

            var result = await _handler.Handle(query, CancellationToken.None);

            _unitOfWorkMock.Verify(u => u.GetCustomRepository<ILessonMaterialRepository>(), Times.Never);
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task Handle_Should_Not_Throw_If_LessonMaterialRepo_IsNull()
        {
            var userId = Guid.NewGuid();
            var param = new FolderSpecParam { UserId = userId, Status = EntityStatus.Active };
            var query = new GetAllUserFoldersQuery(param);

            var folders = new List<Folder>
            {
                new() { Id = Guid.NewGuid(), OwnerType = OwnerType.Personal, UserId = userId, Status = EntityStatus.Active }
            };

            var folderResponses = new List<FolderResponse>
            {
                new() { Id = folders[0].Id }
            };

            _folderRepoMock.Setup(r => r.ListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Folder, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(folders);

            _mapperMock.Setup(m => m.Map<List<FolderResponse>>(folders)).Returns(folderResponses);

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<ILessonMaterialRepository>())
                .Returns((ILessonMaterialRepository?)null!);
            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(1));
        }
    }
}