using AutoMapper;
using Eduva.Application.Common.Models;
using Eduva.Application.Common.Specifications;
using Eduva.Application.Features.Folders.Queries;
using Eduva.Application.Features.Folders.Responses;
using Eduva.Application.Features.Folders.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.Folders.Queries
{
    [TestFixture]
    public class GetFoldersHandlerTest
    {
        #region Setup
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<IFolderRepository> _folderRepoMock;
        private Mock<IMapper> _mapperMock;
        private Mock<UserManager<ApplicationUser>> _userManagerMock;
        private GetFoldersHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _folderRepoMock = new Mock<IFolderRepository>();
            _mapperMock = new Mock<IMapper>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object,
                null!, null!, null!, null!, null!, null!, null!, null!
            );
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IFolderRepository>()).Returns(_folderRepoMock.Object);
            _handler = new GetFoldersHandler(_folderRepoMock.Object, _mapperMock.Object, _unitOfWorkMock.Object, _userManagerMock.Object);
        }
        #endregion

        #region Tests
        [Test]
        public async Task Handle_Returns_Folders()
        {
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10 };
            var query = new GetFoldersQuery(param);
            var folders = new List<Folder> { new Folder { Name = "Test Folder" } };
            var pagination = new Pagination<Folder>(1, 10, 1, folders);
            _folderRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ISpecification<Folder>>()))
                .ReturnsAsync(pagination);
            var folderResponses = new List<FolderResponse> { new FolderResponse { Name = "Test Folder" } };
            _mapperMock.Setup(m => m.Map<IReadOnlyCollection<FolderResponse>>(folders))
                .Returns(folderResponses);
            var result = await _handler.Handle(query, CancellationToken.None);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data.Count, Is.EqualTo(1));
            Assert.That(result.Data.First().Name, Is.EqualTo("Test Folder"));
        }

        [Test]
        public async Task Handle_Returns_Empty_When_NoFolders()
        {
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10 };
            var query = new GetFoldersQuery(param);
            var folders = new List<Folder>();
            var pagination = new Pagination<Folder>(1, 10, 0, folders);
            _folderRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ISpecification<Folder>>()))
                .ReturnsAsync(pagination);
            _mapperMock.Setup(m => m.Map<IReadOnlyCollection<FolderResponse>>(folders))
                .Returns(new List<FolderResponse>());
            var result = await _handler.Handle(query, CancellationToken.None);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data, Is.Empty);
        }

        [Test]
        public async Task Handle_Returns_MultipleFolders_WithPagination()
        {
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 2 };
            var query = new GetFoldersQuery(param);
            var folders = new List<Folder> { new Folder { Name = "A" }, new Folder { Name = "B" } };
            var pagination = new Pagination<Folder>(1, 2, 2, folders);
            _folderRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ISpecification<Folder>>()))
                .ReturnsAsync(pagination);
            var folderResponses = new List<FolderResponse> { new FolderResponse { Name = "A" }, new FolderResponse { Name = "B" } };
            _mapperMock.Setup(m => m.Map<IReadOnlyCollection<FolderResponse>>(folders))
                .Returns(folderResponses);
            var result = await _handler.Handle(query, CancellationToken.None);
            Assert.That(result.Data.Count, Is.EqualTo(2));
            Assert.That(result.Data.Select(f => f.Name), Is.EquivalentTo(new[] { "A", "B" }));
        }

        [Test]
        public async Task Handle_Throws_When_MapperFails()
        {
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 1 };
            var query = new GetFoldersQuery(param);
            var folders = new List<Folder> { new Folder { Name = "X" } };
            var pagination = new Pagination<Folder>(1, 1, 1, folders);
            _folderRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ISpecification<Folder>>()))
                .ReturnsAsync(pagination);
            _mapperMock.Setup(m => m.Map<IReadOnlyCollection<FolderResponse>>(folders))
                .Throws(new Exception("Mapping failed"));
            Assert.ThrowsAsync<Exception>(async () => await _handler.Handle(query, CancellationToken.None));
        }

        [Test]
        public async Task Handle_Throws_When_RepositoryThrows()
        {
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 1 };
            var query = new GetFoldersQuery(param);
            _folderRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ISpecification<Folder>>()))
                .ThrowsAsync(new Exception("Repo error"));
            Assert.ThrowsAsync<Exception>(async () => await _handler.Handle(query, CancellationToken.None));
        }

        [Test]
        public async Task Handle_Returns_Empty_When_PaginationIsNull()
        {
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10 };
            var query = new GetFoldersQuery(param);
            _folderRepoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<ISpecification<Folder>>()))
                .ReturnsAsync((Pagination<Folder>?)null);
            var result = await _handler.Handle(query, CancellationToken.None);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data, Is.Empty);
        }
        #endregion
    }
}