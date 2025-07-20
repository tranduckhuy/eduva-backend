using Eduva.Application.Features.LessonMaterials;
using Eduva.Application.Features.LessonMaterials.Commands.CreateLessonMaterial;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Eduva.Application.Test.Features.LessonMaterials.Commands
{
    [TestFixture]
    public class CreateLessonMaterialHandlerTests
    {
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<ILessonMaterialRepository> _mockLessonMaterialRepository;
        private Mock<IGenericRepository<FolderLessonMaterial, int>> _mockFolderLessonMaterialRepository;
        private Mock<ILogger<CreateLessonMaterialHandler>> _mockLogger;
        private Mock<IStorageService> _mockStorageService;
        private CreateLessonMaterialHandler _handler;

        [SetUp]
        public void Setup()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLessonMaterialRepository = new Mock<ILessonMaterialRepository>();
            _mockFolderLessonMaterialRepository = new Mock<IGenericRepository<FolderLessonMaterial, int>>();
            _mockLogger = new Mock<ILogger<CreateLessonMaterialHandler>>();
            _mockStorageService = new Mock<IStorageService>();

            _mockUnitOfWork.Setup(x => x.GetCustomRepository<ILessonMaterialRepository>())
                .Returns(_mockLessonMaterialRepository.Object);
            _mockUnitOfWork.Setup(x => x.GetRepository<FolderLessonMaterial, int>())
                .Returns(_mockFolderLessonMaterialRepository.Object);

            _handler = new CreateLessonMaterialHandler(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockStorageService.Object);
        }

        #region Success Tests

        [Test]
        public async Task Handle_ValidRequest_ReturnsUnit()
        {
            // Arrange
            var request = new CreateLessonMaterialCommand
            {
                CreatedBy = Guid.NewGuid(),
                SchoolId = 1,
                FolderId = Guid.NewGuid(),
                BlobNames = new List<string> { "test.pdf" },
                LessonMaterials = new List<LessonMaterialRequest>
                {
                    new LessonMaterialRequest
                    {
                        Title = "Test Material",
                        Description = "Test Description",
                        ContentType = ContentType.PDF,
                        FileSize = 1024,
                        SourceUrl = "http://test.com",
                        Duration = 120,
                        IsAIContent = false
                    }
                }
            };

            _mockUnitOfWork.Setup(x => x.CommitAsync()).ReturnsAsync(1);
            _mockLessonMaterialRepository.Setup(x => x.AddAsync(It.IsAny<LessonMaterial>()))
                .Returns(Task.CompletedTask);
            _mockFolderLessonMaterialRepository.Setup(x => x.AddAsync(It.IsAny<FolderLessonMaterial>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.That(result, Is.EqualTo(Unit.Value));
            _mockUnitOfWork.Verify(x => x.CommitAsync(), Times.Once);
            _mockLessonMaterialRepository.Verify(x => x.AddAsync(It.IsAny<LessonMaterial>()), Times.Once);
            _mockFolderLessonMaterialRepository.Verify(x => x.AddAsync(It.IsAny<FolderLessonMaterial>()), Times.Once);
        }

        [Test]
        public async Task Handle_MultipleLessonMaterials_CreatesAllMaterials()
        {
            // Arrange
            var request = new CreateLessonMaterialCommand
            {
                CreatedBy = Guid.NewGuid(),
                SchoolId = 1,
                FolderId = Guid.NewGuid(),
                BlobNames = new List<string> { "test1.pdf", "test2.pdf", "test3.pdf" },
                LessonMaterials = new List<LessonMaterialRequest>
                {
                    new LessonMaterialRequest
                    {
                        Title = "Test Material 1",
                        ContentType = ContentType.PDF,
                        FileSize = 1024,
                        SourceUrl = "http://test1.com"
                    },
                    new LessonMaterialRequest
                    {
                        Title = "Test Material 2",
                        ContentType = ContentType.Video,
                        FileSize = 2048,
                        SourceUrl = "http://test2.com"
                    },                    new LessonMaterialRequest
                    {
                        Title = "Test Material 3",
                        ContentType = ContentType.DOCX,
                        FileSize = 512,
                        SourceUrl = "http://test3.com"
                    }
                }
            };

            _mockUnitOfWork.Setup(x => x.CommitAsync()).ReturnsAsync(3);
            _mockLessonMaterialRepository.Setup(x => x.AddAsync(It.IsAny<LessonMaterial>()))
                .Returns(Task.CompletedTask);
            _mockFolderLessonMaterialRepository.Setup(x => x.AddAsync(It.IsAny<FolderLessonMaterial>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.That(result, Is.EqualTo(Unit.Value));
            _mockLessonMaterialRepository.Verify(x => x.AddAsync(It.IsAny<LessonMaterial>()), Times.Exactly(3));
            _mockFolderLessonMaterialRepository.Verify(x => x.AddAsync(It.IsAny<FolderLessonMaterial>()), Times.Exactly(3));
        }
        [Test]
        public async Task Handle_ValidRequest_SetsCorrectDefaultValues()
        {
            // Arrange
            var request = new CreateLessonMaterialCommand
            {
                CreatedBy = Guid.NewGuid(),
                SchoolId = 1,
                FolderId = Guid.NewGuid(),
                BlobNames = new List<string> { "test.pdf" },
                LessonMaterials = new List<LessonMaterialRequest>
                {
                    new LessonMaterialRequest
                    {
                        Title = "Test Material",
                        ContentType = ContentType.PDF,
                        FileSize = 1024,
                        SourceUrl = "http://test.com"
                    }
                }
            };

            LessonMaterial? capturedLessonMaterial = null;
            _mockLessonMaterialRepository.Setup(x => x.AddAsync(It.IsAny<LessonMaterial>()))
                .Callback<LessonMaterial>(lm => capturedLessonMaterial = lm)
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            // Act
            await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.That(capturedLessonMaterial, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(capturedLessonMaterial.Id, Is.Not.EqualTo(Guid.Empty));
                Assert.That(capturedLessonMaterial.LessonStatus, Is.EqualTo(LessonMaterialStatus.Pending));
                Assert.That(capturedLessonMaterial.Visibility, Is.EqualTo(LessonMaterialVisibility.Private));
                Assert.That(capturedLessonMaterial.CreatedByUserId, Is.EqualTo(request.CreatedBy));
                Assert.That(capturedLessonMaterial.SchoolId, Is.EqualTo(request.SchoolId));
            });
        }

        [Test]
        public async Task Handle_ValidRequest_CreatesFolderLessonMaterialRelationship()
        {
            // Arrange
            var request = new CreateLessonMaterialCommand
            {
                CreatedBy = Guid.NewGuid(),
                SchoolId = 1,
                FolderId = Guid.NewGuid(),
                BlobNames = new List<string> { "test.pdf" },
                LessonMaterials = new List<LessonMaterialRequest>
                {
                    new LessonMaterialRequest
                    {
                        Title = "Test Material",
                        ContentType = ContentType.PDF,
                        FileSize = 1024,
                        SourceUrl = "http://test.com"
                    }
                }
            };

            FolderLessonMaterial? capturedFolderLessonMaterial = null;
            _mockFolderLessonMaterialRepository.Setup(x => x.AddAsync(It.IsAny<FolderLessonMaterial>()))
                .Callback<FolderLessonMaterial>(flm => capturedFolderLessonMaterial = flm)
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            // Act
            await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.That(capturedFolderLessonMaterial, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(capturedFolderLessonMaterial.FolderId, Is.EqualTo(request.FolderId));
                Assert.That(capturedFolderLessonMaterial.LessonMaterialId, Is.Not.EqualTo(Guid.Empty));
            });
        }

        #endregion

        #region Exception Tests        
        [Test]
        public void Handle_ExceptionInAddLessonMaterial_CallsRollbackAndDeletesBlobs()
        {
            // Arrange
            var request = new CreateLessonMaterialCommand
            {
                CreatedBy = Guid.NewGuid(),
                SchoolId = 1,
                FolderId = Guid.NewGuid(),
                BlobNames = new List<string> { "test.pdf", "test2.pdf" },
                LessonMaterials = new List<LessonMaterialRequest>
                {
                    new LessonMaterialRequest
                    {
                        Title = "Test Material",
                        ContentType = ContentType.PDF,
                        FileSize = 1024,
                        SourceUrl = "http://test.com"
                    }
                }
            };

            var expectedException = new Exception("Database error");
            _mockLessonMaterialRepository.Setup(x => x.AddAsync(It.IsAny<LessonMaterial>()))
                .ThrowsAsync(expectedException);
            _mockStorageService.Setup(x => x.DeleteRangeFileAsync(It.IsAny<List<string>>()))
                .Returns(Task.CompletedTask);

            // Act & Assert
            var exception = Assert.ThrowsAsync<Exception>(
                async () => await _handler.Handle(request, CancellationToken.None));

            Assert.That(exception, Is.Not.Null);
            _mockStorageService.Verify(x => x.DeleteRangeFileAsync(request.BlobNames), Times.Once);
        }
        [Test]
        public void Handle_ExceptionInFolderRelationship_CallsRollbackAndDeletesBlobs()
        {
            // Arrange
            var request = new CreateLessonMaterialCommand
            {
                CreatedBy = Guid.NewGuid(),
                SchoolId = 1,
                FolderId = Guid.NewGuid(),
                BlobNames = new List<string> { "test.pdf" },
                LessonMaterials = new List<LessonMaterialRequest>
                {
                    new LessonMaterialRequest
                    {
                        Title = "Test Material",
                        ContentType = ContentType.PDF,
                        FileSize = 1024,
                        SourceUrl = "http://test.com"
                    }
                }
            };

            var expectedException = new Exception("Folder relationship error");
            _mockLessonMaterialRepository.Setup(x => x.AddAsync(It.IsAny<LessonMaterial>()))
                .Returns(Task.CompletedTask);
            _mockFolderLessonMaterialRepository.Setup(x => x.AddAsync(It.IsAny<FolderLessonMaterial>()))
                .ThrowsAsync(expectedException);
            _mockStorageService.Setup(x => x.DeleteRangeFileAsync(It.IsAny<List<string>>()))
                .Returns(Task.CompletedTask);

            // Act & Assert
            var exception = Assert.ThrowsAsync<Exception>(
                async () => await _handler.Handle(request, CancellationToken.None));

            Assert.That(exception, Is.Not.Null);
            _mockStorageService.Verify(x => x.DeleteRangeFileAsync(request.BlobNames), Times.Once);
        }
        [Test]
        public void Handle_ExceptionInCommit_CallsRollbackAndDeletesBlobs()
        {
            // Arrange
            var request = new CreateLessonMaterialCommand
            {
                CreatedBy = Guid.NewGuid(),
                SchoolId = 1,
                FolderId = Guid.NewGuid(),
                BlobNames = new List<string> { "test.pdf" },
                LessonMaterials = new List<LessonMaterialRequest>
                {
                    new LessonMaterialRequest
                    {
                        Title = "Test Material",
                        ContentType = ContentType.PDF,
                        FileSize = 1024,
                        SourceUrl = "http://test.com"
                    }
                }
            };

            var expectedException = new Exception("Commit error");
            _mockLessonMaterialRepository.Setup(x => x.AddAsync(It.IsAny<LessonMaterial>()))
                .Returns(Task.CompletedTask);
            _mockFolderLessonMaterialRepository.Setup(x => x.AddAsync(It.IsAny<FolderLessonMaterial>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CommitAsync()).ThrowsAsync(expectedException);
            _mockStorageService.Setup(x => x.DeleteRangeFileAsync(It.IsAny<List<string>>()))
                .Returns(Task.CompletedTask);

            // Act & Assert
            var exception = Assert.ThrowsAsync<Exception>(
                async () => await _handler.Handle(request, CancellationToken.None));

            Assert.That(exception, Is.Not.Null);
            _mockStorageService.Verify(x => x.DeleteRangeFileAsync(request.BlobNames), Times.Once);
        }

        [Test]
        public void Handle_ExceptionThrown_LogsError()
        {
            // Arrange
            var request = new CreateLessonMaterialCommand
            {
                CreatedBy = Guid.NewGuid(),
                SchoolId = 1,
                FolderId = Guid.NewGuid(),
                BlobNames = new List<string> { "test.pdf" },
                LessonMaterials = new List<LessonMaterialRequest>
                {
                    new LessonMaterialRequest
                    {
                        Title = "Test Material",
                        ContentType = ContentType.PDF,
                        FileSize = 1024,
                        SourceUrl = "http://test.com"
                    }
                }
            };

            var expectedException = new Exception("Test exception");
            _mockLessonMaterialRepository.Setup(x => x.AddAsync(It.IsAny<LessonMaterial>()))
                .ThrowsAsync(expectedException);
            _mockStorageService.Setup(x => x.DeleteRangeFileAsync(It.IsAny<List<string>>()))
                .Returns(Task.CompletedTask);

            // Act & Assert
            Assert.ThrowsAsync<Exception>(
                async () => await _handler.Handle(request, CancellationToken.None));
            // Verify that error was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error creating lesson materials")),
                    It.Is<Exception>(ex => ex.Message == "Test exception"),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Edge Cases

        [Test]
        public async Task Handle_EmptyLessonMaterialsList_DoesNotCreateAnyMaterials()
        {
            // Arrange
            var request = new CreateLessonMaterialCommand
            {
                CreatedBy = Guid.NewGuid(),
                SchoolId = 1,
                FolderId = Guid.NewGuid(),
                BlobNames = new List<string>(),
                LessonMaterials = new List<LessonMaterialRequest>()
            };

            _mockUnitOfWork.Setup(x => x.CommitAsync()).ReturnsAsync(0);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.That(result, Is.EqualTo(Unit.Value));
            _mockLessonMaterialRepository.Verify(x => x.AddAsync(It.IsAny<LessonMaterial>()), Times.Never);
            _mockFolderLessonMaterialRepository.Verify(x => x.AddAsync(It.IsAny<FolderLessonMaterial>()), Times.Never);
        }

        [Test]
        public async Task Handle_NullSchoolId_SetsSchoolIdToNull()
        {
            // Arrange
            var request = new CreateLessonMaterialCommand
            {
                CreatedBy = Guid.NewGuid(),
                SchoolId = null,
                FolderId = Guid.NewGuid(),
                BlobNames = new List<string> { "test.pdf" },
                LessonMaterials = new List<LessonMaterialRequest>
                {
                    new LessonMaterialRequest
                    {
                        Title = "Test Material",
                        ContentType = ContentType.PDF,
                        FileSize = 1024,
                        SourceUrl = "http://test.com"
                    }
                }
            };

            LessonMaterial? capturedLessonMaterial = null;
            _mockLessonMaterialRepository.Setup(x => x.AddAsync(It.IsAny<LessonMaterial>()))
                .Callback<LessonMaterial>(lm => capturedLessonMaterial = lm)
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            // Act
            await _handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.That(capturedLessonMaterial, Is.Not.Null);
            Assert.That(capturedLessonMaterial.SchoolId, Is.Null);
        }
        [Test]
        public void Handle_CancellationRequested_CompletesNormally()
        {
            // Arrange
            var request = new CreateLessonMaterialCommand
            {
                CreatedBy = Guid.NewGuid(),
                SchoolId = 1,
                FolderId = Guid.NewGuid(),
                BlobNames = new List<string> { "test.pdf" },
                LessonMaterials = new List<LessonMaterialRequest>
                {
                    new LessonMaterialRequest
                    {
                        Title = "Test Material",
                        ContentType = ContentType.PDF,
                        FileSize = 1024,
                        SourceUrl = "http://test.com"
                    }
                }
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _mockLessonMaterialRepository.Setup(x => x.AddAsync(It.IsAny<LessonMaterial>()))
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CommitAsync()).ReturnsAsync(1);

            // Act & Assert
            // Since the handler doesn't check cancellation tokens, it completes normally
            Assert.DoesNotThrowAsync(
                async () => await _handler.Handle(request, cancellationTokenSource.Token));
        }
        [Test]
        public void Handle_BeginTransactionThrowsException_CallsRollbackAndDeletesFiles()
        {
            // Arrange  
            var request = new CreateLessonMaterialCommand
            {
                CreatedBy = Guid.NewGuid(),
                SchoolId = 1,
                FolderId = Guid.NewGuid(),
                BlobNames = new List<string> { "test.pdf" },
                LessonMaterials = new List<LessonMaterialRequest>
                {
                    new LessonMaterialRequest
                    {
                        Title = "Test Material",
                        ContentType = ContentType.PDF,
                        FileSize = 1024,
                        SourceUrl = "http://test.com"
                    }
                }
            };

            var expectedException = new Exception("Repository setup error");
            _mockLessonMaterialRepository.Setup(x => x.AddAsync(It.IsAny<LessonMaterial>()))
                .ThrowsAsync(expectedException);
            _mockStorageService.Setup(x => x.DeleteRangeFileAsync(It.IsAny<List<string>>()))
                .Returns(Task.CompletedTask);

            // Act & Assert
            var exception = Assert.ThrowsAsync<Exception>(
                async () => await _handler.Handle(request, CancellationToken.None));

            Assert.That(exception, Is.Not.Null);
            _mockStorageService.Verify(x => x.DeleteRangeFileAsync(request.BlobNames), Times.Once);
        }

        #endregion
    }
}
