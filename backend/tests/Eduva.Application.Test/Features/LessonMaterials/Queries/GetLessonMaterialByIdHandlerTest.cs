using Eduva.Application.Common.Exceptions;
using Eduva.Application.Exceptions.LessonMaterial;
using Eduva.Application.Features.LessonMaterials.Queries.GetLessonMaterialById;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Moq;

namespace Eduva.Application.Test.Features.LessonMaterials.Queries
{
    [TestFixture]
    public class GetLessonMaterialByIdHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<ILessonMaterialRepository> _lessonMaterialRepoMock = null!;
        private Mock<IUserRepository> _userRepoMock = null!;
        private Mock<IStorageService> _storageServiceMock = null!;
        private GetLessonMaterialByIdHandler _handler = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _lessonMaterialRepoMock = new Mock<ILessonMaterialRepository>();
            _userRepoMock = new Mock<IUserRepository>();
            _storageServiceMock = new Mock<IStorageService>();

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<ILessonMaterialRepository>())
                .Returns(_lessonMaterialRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IUserRepository>())
                .Returns(_userRepoMock.Object);

            _handler = new GetLessonMaterialByIdHandler(_unitOfWorkMock.Object, _storageServiceMock.Object);
        }

        [Test]
        public void Handle_Should_Throw_LessonMaterialNotFoundException_If_NotFound()
        {
            var query = new GetLessonMaterialByIdQuery
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid()
            };

            _lessonMaterialRepoMock.Setup(r => r.GetByIdWithDetailsAsync(query.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((LessonMaterial?)null);

            Assert.ThrowsAsync<LessonMaterialNotFoundException>(() => _handler.Handle(query, CancellationToken.None));
        }

        [Test]
        public void Handle_Should_Throw_Forbidden_If_User_NotFound()
        {
            var lessonMaterialId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, SchoolId = 1 };

            _lessonMaterialRepoMock.Setup(r => r.GetByIdWithDetailsAsync(lessonMaterialId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(lessonMaterial);

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null);

            var query = new GetLessonMaterialByIdQuery
            {
                Id = lessonMaterialId,
                UserId = userId
            };

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(query, CancellationToken.None));
        }

        [Test]
        public void Handle_Should_Throw_Forbidden_If_SchoolId_Not_Match()
        {
            var lessonMaterialId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, SchoolId = 2 };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };

            _lessonMaterialRepoMock.Setup(r => r.GetByIdWithDetailsAsync(lessonMaterialId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(lessonMaterial);

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            var query = new GetLessonMaterialByIdQuery
            {
                Id = lessonMaterialId,
                UserId = userId
            };

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(query, CancellationToken.None));
        }

        [Test]
        public void Handle_Should_Throw_Forbidden_If_SchoolId_Null()
        {
            var lessonMaterialId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var lessonMaterial = new LessonMaterial { Id = lessonMaterialId, SchoolId = null };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };

            _lessonMaterialRepoMock.Setup(r => r.GetByIdWithDetailsAsync(lessonMaterialId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(lessonMaterial);

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            var query = new GetLessonMaterialByIdQuery
            {
                Id = lessonMaterialId,
                UserId = userId
            };

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(query, CancellationToken.None));
        }

        [Test]
        public async Task Handle_Should_Return_Response_If_Access_Granted()
        {
            var lessonMaterialId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var lessonMaterial = new LessonMaterial
            {
                Id = lessonMaterialId,
                SchoolId = 1,
                SourceUrl = "source-url"
            };
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };

            _lessonMaterialRepoMock.Setup(r => r.GetByIdWithDetailsAsync(lessonMaterialId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(lessonMaterial);

            _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

            _storageServiceMock.Setup(s => s.GetReadableUrl(lessonMaterial.SourceUrl)).Returns("readable-url");

            var query = new GetLessonMaterialByIdQuery
            {
                Id = lessonMaterialId,
                UserId = userId
            };

            var result = await _handler.Handle(query, CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.SourceUrl, Is.EqualTo("readable-url"));
                Assert.That(result.Id, Is.EqualTo(lessonMaterialId));
            });
        }
    }
}