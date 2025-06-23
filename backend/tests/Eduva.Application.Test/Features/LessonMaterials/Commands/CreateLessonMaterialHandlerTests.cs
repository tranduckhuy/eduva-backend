using Eduva.Application.Features.LessonMaterials.Commands;
using Eduva.Application.Features.LessonMaterials;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;
using Moq;
using NUnit.Framework;
using Microsoft.EntityFrameworkCore.Storage;

namespace Eduva.Application.Test.Features.LessonMaterials.Commands
{
    [TestFixture]
    public class CreateLessonMaterialHandlerTests
    {
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private Mock<ILessonMaterialRepository> _mockLessonMaterialRepository;
        private Mock<IGenericRepository<FolderLessonMaterial, int>> _mockFolderLessonMaterialRepository;
        private CreateLessonMaterialHandler _handler;

        [SetUp]
        public void Setup()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLessonMaterialRepository = new Mock<ILessonMaterialRepository>();
            _mockFolderLessonMaterialRepository = new Mock<IGenericRepository<FolderLessonMaterial, int>>();
            
            _mockUnitOfWork.Setup(x => x.GetCustomRepository<ILessonMaterialRepository>())
                .Returns(_mockLessonMaterialRepository.Object);
            _mockUnitOfWork.Setup(x => x.GetRepository<FolderLessonMaterial, int>())
                .Returns(_mockFolderLessonMaterialRepository.Object);

            _handler = new CreateLessonMaterialHandler(_mockUnitOfWork.Object);
        }

        [Test]
        public async Task Handle_ValidRequest_ReturnsUnit()
        {
            // Arrange
            var request = new CreateLessonMaterialCommand
            {
                CreatedBy = Guid.NewGuid(),
                SchoolId = 1,
                FolderId = 1,
                LessonMaterials = new List<LessonMaterialRequest>
                {
                    new LessonMaterialRequest
                    {
                        Title = "Test Material",
                        Description = "Test Description",
                        ContentType = ContentType.PDF,
                        FileSize = 1024,
                        SourceUrl = "http://test.com"
                    }
                }
            };

            var mockTransaction = new Mock<IDbContextTransaction>();
            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync()).ReturnsAsync(mockTransaction.Object);
            _mockUnitOfWork.Setup(x => x.CommitAsync()).ReturnsAsync(1);
            _mockLessonMaterialRepository.Setup(x => x.AddAsync(It.IsAny<LessonMaterial>())).Returns(Task.CompletedTask);
            _mockFolderLessonMaterialRepository.Setup(x => x.AddAsync(It.IsAny<FolderLessonMaterial>())).Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);            // Assert
            Assert.That(result, Is.EqualTo(Unit.Value));
            _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitAsync(), Times.Once);
            _mockLessonMaterialRepository.Verify(x => x.AddAsync(It.IsAny<LessonMaterial>()), Times.Once);
            _mockFolderLessonMaterialRepository.Verify(x => x.AddAsync(It.IsAny<FolderLessonMaterial>()), Times.Once);
        }        [Test]
        public void Handle_ExceptionThrown_CallsRollback()
        {
            // Arrange
            var request = new CreateLessonMaterialCommand
            {
                CreatedBy = Guid.NewGuid(),
                SchoolId = 1,
                FolderId = 1,
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

            var mockTransaction = new Mock<IDbContextTransaction>();
            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync()).ReturnsAsync(mockTransaction.Object);
            _mockLessonMaterialRepository.Setup(x => x.AddAsync(It.IsAny<LessonMaterial>())).ThrowsAsync(new Exception("Test exception"));
            _mockUnitOfWork.Setup(x => x.RollbackAsync()).Returns(Task.CompletedTask);

            // Act & Assert
            Assert.ThrowsAsync<Exception>(async () => await _handler.Handle(request, CancellationToken.None));
            _mockUnitOfWork.Verify(x => x.RollbackAsync(), Times.Once);
        }
    }
}
