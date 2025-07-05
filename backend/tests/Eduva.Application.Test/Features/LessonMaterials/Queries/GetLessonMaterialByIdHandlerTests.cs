using Eduva.Application.Common.Exceptions;
using Eduva.Application.Exceptions.LessonMaterial;
using Eduva.Application.Features.LessonMaterials.Queries;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Moq;

namespace Eduva.Application.Test.Features.LessonMaterials.Queries
{
    [TestFixture]
    public class GetLessonMaterialByIdHandlerTests
    {
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<ILessonMaterialRepository> _mockLessonMaterialRepository;
        private Mock<IUserRepository> _mockUserRepository;
        private Mock<IStorageService> _mockStorageService;
        private GetLessonMaterialByIdHandler _handler;

        [SetUp]
        public void Setup()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLessonMaterialRepository = new Mock<ILessonMaterialRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockStorageService = new Mock<IStorageService>();

            _mockUnitOfWork.Setup(x => x.GetCustomRepository<ILessonMaterialRepository>())
                .Returns(_mockLessonMaterialRepository.Object);
            _mockUnitOfWork.Setup(x => x.GetCustomRepository<IUserRepository>())
                .Returns(_mockUserRepository.Object);

            _handler = new GetLessonMaterialByIdHandler(_mockUnitOfWork.Object, _mockStorageService.Object);
        }

        [Test]
        public async Task Handle_ShouldReturnLessonMaterial_WhenSuccessful()
        {
            // Arrange
            var materialId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var schoolId = 1;

            var query = new GetLessonMaterialByIdQuery
            {
                Id = materialId,
                UserId = userId,
                SchoolId = schoolId
            };

            var lessonMaterial = new LessonMaterial
            {
                Id = materialId,
                Title = "Test Material",
                Description = "Test Description",
                ContentType = ContentType.PDF,
                SchoolId = schoolId,
                CreatedByUserId = Guid.NewGuid(),
                LessonStatus = LessonMaterialStatus.Approved,
                Visibility = LessonMaterialVisibility.School,
                FileSize = 1024,
                SourceUrl = "test.pdf"
            };

            var user = new ApplicationUser
            {
                Id = userId,
                SchoolId = schoolId
            };

            var readableUrl = "https://storage.blob.core.windows.net/test.pdf?readable";

            _mockLessonMaterialRepository.Setup(r => r.GetByIdWithDetailsAsync(materialId, default))
                .ReturnsAsync(lessonMaterial);
            _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _mockStorageService.Setup(s => s.GetReadableUrl("test.pdf"))
                .Returns(readableUrl);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(materialId));
            Assert.That(result.Title, Is.EqualTo("Test Material"));
            Assert.That(result.SourceUrl, Is.EqualTo(readableUrl));

            _mockLessonMaterialRepository.Verify(r => r.GetByIdWithDetailsAsync(materialId, default), Times.Once);
            _mockUserRepository.Verify(r => r.GetByIdAsync(userId), Times.Once);
            _mockStorageService.Verify(s => s.GetReadableUrl("test.pdf"), Times.Once);
        }

        [Test]
        public void Handle_ShouldThrowLessonMaterialNotFoundException_WhenMaterialNotFound()
        {
            // Arrange
            var materialId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var query = new GetLessonMaterialByIdQuery
            {
                Id = materialId,
                UserId = userId,
                SchoolId = 1
            };

            _mockLessonMaterialRepository.Setup(r => r.GetByIdWithDetailsAsync(materialId, default))
                .ReturnsAsync((LessonMaterial?)null);

            // Act & Assert
            Assert.ThrowsAsync<LessonMaterialNotFountException>(async () =>
                await _handler.Handle(query, CancellationToken.None));

            _mockLessonMaterialRepository.Verify(r => r.GetByIdWithDetailsAsync(materialId, default), Times.Once);
            _mockUserRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Test]
        public void Handle_ShouldThrowForbiddenException_WhenUserNotFound()
        {
            // Arrange
            var materialId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var query = new GetLessonMaterialByIdQuery
            {
                Id = materialId,
                UserId = userId,
                SchoolId = 1
            };

            var lessonMaterial = new LessonMaterial
            {
                Id = materialId,
                SchoolId = 1,
                SourceUrl = "test.pdf"
            };

            _mockLessonMaterialRepository.Setup(r => r.GetByIdWithDetailsAsync(materialId, default))
                .ReturnsAsync(lessonMaterial);
            _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync((ApplicationUser?)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.Forbidden));
        }

        [Test]
        public void Handle_ShouldThrowForbiddenException_WhenUserDoesNotHaveAccess()
        {
            // Arrange
            var materialId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var query = new GetLessonMaterialByIdQuery
            {
                Id = materialId,
                UserId = userId,
                SchoolId = 1
            };

            var lessonMaterial = new LessonMaterial
            {
                Id = materialId,
                SchoolId = 2, // Different school
                SourceUrl = "test.pdf"
            };

            var user = new ApplicationUser
            {
                Id = userId,
                SchoolId = 1
            };

            _mockLessonMaterialRepository.Setup(r => r.GetByIdWithDetailsAsync(materialId, default))
                .ReturnsAsync(lessonMaterial);
            _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.Forbidden));
        }

        [Test]
        public void Handle_ShouldThrowForbiddenException_WhenMaterialHasNullSchoolId()
        {
            // Arrange
            var materialId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var query = new GetLessonMaterialByIdQuery
            {
                Id = materialId,
                UserId = userId,
                SchoolId = 1
            };

            var lessonMaterial = new LessonMaterial
            {
                Id = materialId,
                SchoolId = null, // Null school ID - should deny access
                Title = "Public Material",
                SourceUrl = "test.pdf"
            };

            var user = new ApplicationUser
            {
                Id = userId,
                SchoolId = 1
            };

            _mockLessonMaterialRepository.Setup(r => r.GetByIdWithDetailsAsync(materialId, default))
                .ReturnsAsync(lessonMaterial);
            _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.Forbidden));
        }

        [Test]
        public void Handle_ShouldThrowForbiddenException_WhenUserHasNullSchoolId()
        {
            // Arrange
            var materialId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var query = new GetLessonMaterialByIdQuery
            {
                Id = materialId,
                UserId = userId,
                SchoolId = 1
            };

            var lessonMaterial = new LessonMaterial
            {
                Id = materialId,
                SchoolId = 1,
                Title = "School Material",
                SourceUrl = "test.pdf"
            };

            var user = new ApplicationUser
            {
                Id = userId,
                SchoolId = null // User with null school ID - should deny access
            };

            _mockLessonMaterialRepository.Setup(r => r.GetByIdWithDetailsAsync(materialId, default))
                .ReturnsAsync(lessonMaterial);
            _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(exception.StatusCode, Is.EqualTo(CustomCode.Forbidden));
        }

        [Test]
        public async Task Handle_ShouldReturnCorrectSourceUrl_WhenStorageServiceCalled()
        {
            // Arrange
            var materialId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var originalUrl = "original.pdf";
            var readableUrl = "https://storage.blob.core.windows.net/readable.pdf?token";

            var query = new GetLessonMaterialByIdQuery
            {
                Id = materialId,
                UserId = userId,
                SchoolId = 1
            };

            var lessonMaterial = new LessonMaterial
            {
                Id = materialId,
                SchoolId = 1,
                SourceUrl = originalUrl
            };

            var user = new ApplicationUser
            {
                Id = userId,
                SchoolId = 1
            };

            _mockLessonMaterialRepository.Setup(r => r.GetByIdWithDetailsAsync(materialId, default))
                .ReturnsAsync(lessonMaterial);
            _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _mockStorageService.Setup(s => s.GetReadableUrl(originalUrl))
                .Returns(readableUrl);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result.SourceUrl, Is.EqualTo(readableUrl));
            _mockStorageService.Verify(s => s.GetReadableUrl(originalUrl), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldHandleCancellationToken()
        {
            // Arrange
            var materialId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var cancellationToken = new CancellationToken();

            var query = new GetLessonMaterialByIdQuery
            {
                Id = materialId,
                UserId = userId,
                SchoolId = 1
            };

            var lessonMaterial = new LessonMaterial
            {
                Id = materialId,
                SchoolId = 1,
                SourceUrl = "test.pdf"
            };

            var user = new ApplicationUser
            {
                Id = userId,
                SchoolId = 1
            };

            _mockLessonMaterialRepository.Setup(r => r.GetByIdWithDetailsAsync(materialId, cancellationToken))
                .ReturnsAsync(lessonMaterial);
            _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _mockStorageService.Setup(s => s.GetReadableUrl("test.pdf"))
                .Returns("readable-url");

            // Act
            await _handler.Handle(query, cancellationToken);

            // Assert
            _mockLessonMaterialRepository.Verify(r => r.GetByIdWithDetailsAsync(materialId, cancellationToken), Times.Once);
        }

        [Test]
        public void Handle_ShouldThrowException_WhenRepositoryThrowsException()
        {
            // Arrange
            var materialId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var query = new GetLessonMaterialByIdQuery
            {
                Id = materialId,
                UserId = userId,
                SchoolId = 1
            };

            _mockLessonMaterialRepository.Setup(r => r.GetByIdWithDetailsAsync(materialId, default))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert
            Assert.ThrowsAsync<Exception>(async () =>
                await _handler.Handle(query, CancellationToken.None));
        }
    }
}