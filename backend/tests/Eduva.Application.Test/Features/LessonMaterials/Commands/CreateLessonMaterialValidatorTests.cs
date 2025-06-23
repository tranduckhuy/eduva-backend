using Eduva.Application.Features.LessonMaterials;
using Eduva.Application.Features.LessonMaterials.Commands;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using FluentValidation.TestHelper;
using Moq;

namespace Eduva.Application.Test.Features.LessonMaterials.Commands
{
    [TestFixture]
    public class CreateLessonMaterialValidatorTests
    {
        private Mock<IUnitOfWork> _mockUnitOfWork;
        private CreateLessonMaterialValidator _validator;

        [SetUp]
        public void Setup()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _validator = new CreateLessonMaterialValidator(_mockUnitOfWork.Object);
        }        [Test]
        public async Task Should_Have_Error_When_FolderId_Is_Zero()
        {
            // Arrange
            var command = new CreateLessonMaterialCommand
            {
                FolderId = 0,
                LessonMaterials = new List<LessonMaterialRequest>
                {
                    new LessonMaterialRequest { Title = "Test", ContentType = ContentType.PDF, FileSize = 1024, SourceUrl = "http://test.com" }
                }
            };

            // Act & Assert
            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(x => x.FolderId);
        }

        [Test]
        public async Task Should_Have_Error_When_LessonMaterials_Is_Empty()
        {
            // Arrange
            var mockFolderRepository = new Mock<IGenericRepository<Folder, int>>();
            mockFolderRepository.Setup(x => x.ExistsAsync(It.IsAny<int>()))
                .ReturnsAsync(true);
            
            _mockUnitOfWork.Setup(x => x.GetRepository<Folder, int>())
                .Returns(mockFolderRepository.Object);

            var command = new CreateLessonMaterialCommand
            {
                FolderId = 1,
                LessonMaterials = new List<LessonMaterialRequest>()
            };

            // Act & Assert
            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(x => x.LessonMaterials);
        }

        [Test]
        public async Task Should_Have_Error_When_Too_Many_LessonMaterials()
        {
            // Arrange
            var mockFolderRepository = new Mock<IGenericRepository<Folder, int>>();
            mockFolderRepository.Setup(x => x.ExistsAsync(It.IsAny<int>()))
                .ReturnsAsync(true);
            
            _mockUnitOfWork.Setup(x => x.GetRepository<Folder, int>())
                .Returns(mockFolderRepository.Object);

            var command = new CreateLessonMaterialCommand
            {
                FolderId = 1,
                LessonMaterials = Enumerable.Range(1, 11).Select(i => new LessonMaterialRequest
                {
                    Title = $"Test {i}",
                    ContentType = ContentType.PDF,
                    FileSize = 1024,
                    SourceUrl = "http://test.com"
                }).ToList()
            };

            // Act & Assert
            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(x => x.LessonMaterials);
        }

        [Test]
        public async Task Should_Have_Error_When_Folder_Does_Not_Exist()
        {
            // Arrange
            var mockFolderRepository = new Mock<IGenericRepository<Folder, int>>();
            mockFolderRepository.Setup(x => x.ExistsAsync(It.IsAny<int>()))
                .ReturnsAsync(false);

            _mockUnitOfWork.Setup(x => x.GetRepository<Folder, int>())
                .Returns(mockFolderRepository.Object);

            var command = new CreateLessonMaterialCommand
            {
                FolderId = 999,
                LessonMaterials = new List<LessonMaterialRequest>
                {
                    new LessonMaterialRequest { Title = "Test", ContentType = ContentType.PDF, FileSize = 1024, SourceUrl = "http://test.com" }
                }
            };

            // Act & Assert
            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(x => x.FolderId);
        }

        [Test]
        public async Task Should_Not_Have_Error_When_Valid_Command()
        {
            // Arrange
            var mockFolderRepository = new Mock<IGenericRepository<Folder, int>>();
            mockFolderRepository.Setup(x => x.ExistsAsync(It.IsAny<int>()))
                .ReturnsAsync(true);

            _mockUnitOfWork.Setup(x => x.GetRepository<Folder, int>())
                .Returns(mockFolderRepository.Object);

            var command = new CreateLessonMaterialCommand
            {
                FolderId = 1,
                LessonMaterials = new List<LessonMaterialRequest>
                {
                    new LessonMaterialRequest { Title = "Test", ContentType = ContentType.PDF, FileSize = 1024, SourceUrl = "http://test.com" }
                }
            };

            // Act & Assert
            var result = await _validator.TestValidateAsync(command);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }

    [TestFixture]
    public class LessonMaterialRequestValidatorTests
    {
        private LessonMaterialRequestValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new LessonMaterialRequestValidator();
        }

        [Test]
        public void Should_Have_Error_When_Title_Is_Empty()
        {
            // Arrange
            var request = new LessonMaterialRequest
            {
                Title = "",
                ContentType = ContentType.PDF,
                FileSize = 1024,
                SourceUrl = "http://test.com"
            };

            // Act & Assert
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Title);
        }

        [Test]
        public void Should_Have_Error_When_Title_Too_Long()
        {
            // Arrange
            var request = new LessonMaterialRequest
            {
                Title = new string('a', 256), // 256 characters, exceeds 255 limit
                ContentType = ContentType.PDF,
                FileSize = 1024,
                SourceUrl = "http://test.com"
            };

            // Act & Assert
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Title);
        }

        [Test]
        public void Should_Have_Error_When_FileSize_Is_Zero()
        {
            // Arrange
            var request = new LessonMaterialRequest
            {
                Title = "Test",
                ContentType = ContentType.PDF,
                FileSize = 0,
                SourceUrl = "http://test.com"
            };

            // Act & Assert
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.FileSize);
        }

        [Test]
        public void Should_Have_Error_When_SourceUrl_Is_Empty()
        {
            // Arrange
            var request = new LessonMaterialRequest
            {
                Title = "Test",
                ContentType = ContentType.PDF,
                FileSize = 1024,
                SourceUrl = ""
            };

            // Act & Assert
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.SourceUrl);
        }

        [Test]
        public void Should_Not_Have_Error_When_Valid_Request()
        {
            // Arrange
            var request = new LessonMaterialRequest
            {
                Title = "Test Material",
                Description = "Test Description",
                ContentType = ContentType.PDF,
                FileSize = 1024,
                SourceUrl = "http://test.com"
            };

            // Act & Assert
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
