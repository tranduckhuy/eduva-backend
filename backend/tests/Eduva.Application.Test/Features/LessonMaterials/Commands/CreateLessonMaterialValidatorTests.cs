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
                BlobNames = new List<string> { "test.pdf" },
                LessonMaterials = new List<LessonMaterialRequest>
                {
                    new LessonMaterialRequest { Title = "Test", ContentType = ContentType.PDF, FileSize = 1024, SourceUrl = "http://test.com" }
                }
            };

            // Act & Assert
            var result = await _validator.TestValidateAsync(command);
            result.ShouldNotHaveAnyValidationErrors();
        }        [Test]
        public async Task Should_Have_Error_When_SchoolId_Does_Not_Exist()
        {
            // Arrange
            var mockSchoolRepository = new Mock<IGenericRepository<School, int>>();
            mockSchoolRepository.Setup(x => x.ExistsAsync(It.IsAny<int>()))
                .ReturnsAsync(false);
            
            var mockFolderRepository = new Mock<IGenericRepository<Folder, int>>();
            mockFolderRepository.Setup(x => x.ExistsAsync(It.IsAny<int>()))
                .ReturnsAsync(true);

            var mockApplicationUserRepository = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            mockApplicationUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 999 });

            _mockUnitOfWork.Setup(x => x.GetRepository<School, int>())
                .Returns(mockSchoolRepository.Object);
            _mockUnitOfWork.Setup(x => x.GetRepository<Folder, int>())
                .Returns(mockFolderRepository.Object);
            _mockUnitOfWork.Setup(x => x.GetRepository<ApplicationUser, Guid>())
                .Returns(mockApplicationUserRepository.Object);

            var command = new CreateLessonMaterialCommand
            {
                SchoolId = 999,
                FolderId = 1,
                BlobNames = new List<string> { "test.pdf" },
                LessonMaterials = new List<LessonMaterialRequest>
                {
                    new LessonMaterialRequest { Title = "Test", ContentType = ContentType.PDF, FileSize = 1024, SourceUrl = "http://test.com" }
                }
            };

            // Act & Assert
            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(x => x.SchoolId);
        }

        [Test]
        public async Task Should_Have_Error_When_User_Does_Not_Belong_To_School()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var schoolId = 1;

            var mockSchoolRepository = new Mock<IGenericRepository<School, int>>();
            mockSchoolRepository.Setup(x => x.ExistsAsync(schoolId))
                .ReturnsAsync(true);

            var mockUserRepository = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            mockUserRepository.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(new ApplicationUser { Id = userId, SchoolId = 999 }); // Different school

            var mockFolderRepository = new Mock<IGenericRepository<Folder, int>>();
            mockFolderRepository.Setup(x => x.ExistsAsync(It.IsAny<int>()))
                .ReturnsAsync(true);

            _mockUnitOfWork.Setup(x => x.GetRepository<School, int>())
                .Returns(mockSchoolRepository.Object);
            _mockUnitOfWork.Setup(x => x.GetRepository<ApplicationUser, Guid>())
                .Returns(mockUserRepository.Object);
            _mockUnitOfWork.Setup(x => x.GetRepository<Folder, int>())
                .Returns(mockFolderRepository.Object);

            var command = new CreateLessonMaterialCommand
            {
                CreatedBy = userId,
                SchoolId = schoolId,
                FolderId = 1,
                BlobNames = new List<string> { "test.pdf" },
                LessonMaterials = new List<LessonMaterialRequest>
                {
                    new LessonMaterialRequest { Title = "Test", ContentType = ContentType.PDF, FileSize = 1024, SourceUrl = "http://test.com" }
                }
            };

            // Act & Assert
            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(x => x);
        }        [Test]
        public async Task Should_Have_Error_When_BlobNames_Is_Null()
        {
            // Arrange
            var mockSchoolRepository = new Mock<IGenericRepository<School, int>>();
            mockSchoolRepository.Setup(x => x.ExistsAsync(It.IsAny<int>()))
                .ReturnsAsync(true);
            
            var mockFolderRepository = new Mock<IGenericRepository<Folder, int>>();
            mockFolderRepository.Setup(x => x.ExistsAsync(It.IsAny<int>()))
                .ReturnsAsync(true);

            var mockApplicationUserRepository = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            mockApplicationUserRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new ApplicationUser { Id = Guid.NewGuid(), SchoolId = 1 });

            _mockUnitOfWork.Setup(x => x.GetRepository<School, int>())
                .Returns(mockSchoolRepository.Object);
            _mockUnitOfWork.Setup(x => x.GetRepository<Folder, int>())
                .Returns(mockFolderRepository.Object);
            _mockUnitOfWork.Setup(x => x.GetRepository<ApplicationUser, Guid>())
                .Returns(mockApplicationUserRepository.Object);
                
            var command = new CreateLessonMaterialCommand
            {
                FolderId = 1,
                BlobNames = null!,
                LessonMaterials = new List<LessonMaterialRequest>
                {
                    new LessonMaterialRequest { Title = "Test", ContentType = ContentType.PDF, FileSize = 1024, SourceUrl = "http://test.com" }
                }
            };

            // Act & Assert
            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(x => x.BlobNames);
        }

        [Test]
        public async Task Should_Have_Error_When_Too_Many_BlobNames()
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
                BlobNames = Enumerable.Range(1, 11).Select(i => $"test{i}.pdf").ToList(),
                LessonMaterials = new List<LessonMaterialRequest>
                {
                    new LessonMaterialRequest { Title = "Test", ContentType = ContentType.PDF, FileSize = 1024, SourceUrl = "http://test.com" }
                }
            };

            // Act & Assert
            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(x => x.BlobNames);
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
        }        [Test]
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

        [Test]
        public void Should_Have_Error_When_Description_Too_Long()
        {
            // Arrange
            var request = new LessonMaterialRequest
            {
                Title = "Test Material",
                Description = new string('a', 1001), // 1001 characters, exceeds 1000 limit
                ContentType = ContentType.PDF,
                FileSize = 1024,
                SourceUrl = "http://test.com"
            };

            // Act & Assert
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Description);
        }

        [Test]
        public void Should_Have_Error_When_Tag_Too_Long()
        {
            // Arrange
            var request = new LessonMaterialRequest
            {
                Title = "Test Material",
                Tag = new string('a', 101), // 101 characters, exceeds 100 limit
                ContentType = ContentType.PDF,
                FileSize = 1024,
                SourceUrl = "http://test.com"
            };

            // Act & Assert
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Tag);
        }

        [Test]
        public void Should_Have_Error_When_FileSize_Too_Large()
        {
            // Arrange
            var request = new LessonMaterialRequest
            {
                Title = "Test Material",
                ContentType = ContentType.PDF,
                FileSize = 1073741825, // 1GB + 1 byte, exceeds 1GB limit
                SourceUrl = "http://test.com"
            };

            // Act & Assert
            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.FileSize);
        }

        [Test]
        public void Should_Not_Have_Error_When_Description_Is_Null()
        {
            // Arrange
            var request = new LessonMaterialRequest
            {
                Title = "Test Material",
                Description = null,
                ContentType = ContentType.PDF,
                FileSize = 1024,
                SourceUrl = "http://test.com"
            };

            // Act & Assert
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        [Test]
        public void Should_Not_Have_Error_When_Tag_Is_Null()
        {
            // Arrange
            var request = new LessonMaterialRequest
            {
                Title = "Test Material",
                Tag = null,
                ContentType = ContentType.PDF,
                FileSize = 1024,
                SourceUrl = "http://test.com"
            };

            // Act & Assert
            var result = _validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.Tag);
        }
    }
}
