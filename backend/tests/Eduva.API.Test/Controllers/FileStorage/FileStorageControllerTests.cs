using Eduva.API.Controllers.FileStorage;
using Eduva.API.Models.FileStorage;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Exceptions.Storage;
using Eduva.Application.Features.StorageQuota;
using Eduva.Application.Interfaces.Services;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Eduva.API.Test.Controllers.FileStorage
{
    [TestFixture]
    public class FileStorageControllerTests
    {
        private Mock<IStorageService> _storageServiceMock;
        private Mock<IStorageQuotaService> _storageQuotaServiceMock;
        private Mock<ILogger<FileStorageController>> _loggerMock;
        private FileStorageController _controller;

        [SetUp]
        public void Setup()
        {
            _storageServiceMock = new Mock<IStorageService>();
            _storageQuotaServiceMock = new Mock<IStorageQuotaService>();
            _loggerMock = new Mock<ILogger<FileStorageController>>();
            _controller = new FileStorageController(_loggerMock.Object, _storageServiceMock.Object, _storageQuotaServiceMock.Object);

            // Setup HttpContext for User claims
            SetupUserContext();
        }

        private void SetupUserContext(string userId = "123e4567-e89b-12d3-a456-426614174000", string schoolId = "1")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim("SchoolId", schoolId)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };
        }

        #region GenerateUploadSasTokenWithQuota Tests

        [Test]
        public async Task GenerateUploadSasTokenWithQuota_ShouldReturn200_WhenSuccessful()
        {
            // Arrange
            var request = new GenerateUploadTokensRequest
            {
                Files = new List<FileUploadInfo>
                {
                    new FileUploadInfo { BlobName = "test.pdf", FileSize = 1024 }
                }
            };
            var expectedTokens = new List<string> { "https://storage.blob.core.windows.net/test.pdf?token" };

            _storageServiceMock.Setup(s => s.GenerateUploadSasTokensWithQuotaCheck(
                It.IsAny<List<string>>(), It.IsAny<List<long>>(), It.IsAny<int>()))
                .ReturnsAsync(expectedTokens);

            // Act
            var result = await _controller.GenerateUploadSasTokenWithQuota(request);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task GenerateUploadSasTokenWithQuota_ShouldReturn401_WhenUserIdNotFound()
        {
            // Arrange
            SetupUserContext("", "1");
            var request = new GenerateUploadTokensRequest();

            // Act
            var result = await _controller.GenerateUploadSasTokenWithQuota(request);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(401));
        }

        [Test]
        public async Task GenerateUploadSasTokenWithQuota_ShouldReturn404_WhenSchoolIdNotFound()
        {
            // Arrange
            SetupUserContext("123e4567-e89b-12d3-a456-426614174000", "");
            var request = new GenerateUploadTokensRequest();

            // Act
            var result = await _controller.GenerateUploadSasTokenWithQuota(request);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public async Task GenerateUploadSasTokenWithQuota_ShouldReturn413_WhenQuotaExceeded()
        {
            // Arrange
            var request = new GenerateUploadTokensRequest
            {
                Files = new List<FileUploadInfo>
                {
                    new FileUploadInfo { BlobName = "large-file.pdf", FileSize = 1000000000 }
                }
            };

            _storageServiceMock.Setup(s => s.GenerateUploadSasTokensWithQuotaCheck(
                It.IsAny<List<string>>(), It.IsAny<List<long>>(), It.IsAny<int>()))
                .ThrowsAsync(new StorageQuotaExceededException(100, 50, 75, 0));

            // Act
            var result = await _controller.GenerateUploadSasTokenWithQuota(request);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(413));
        }

        #endregion

        #region GetReadableUrl Tests

        [Test]
        public void GetReadableUrl_ShouldReturn200_WhenSuccessful()
        {
            // Arrange
            var blobName = "test-file.pdf";
            var expectedUrl = "https://storage.blob.core.windows.net/test-file.pdf?readable";

            _storageServiceMock.Setup(s => s.GetReadableUrl(blobName))
                .Returns(expectedUrl);

            // Act
            var result = _controller.GetReadableUrl(blobName);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(200));
            _storageServiceMock.Verify(s => s.GetReadableUrl(blobName), Times.Once);
        }

        [Test]
        public void GetReadableUrl_ShouldReturn400_WhenBlobNameIsNull()
        {
            // Act
            var result = _controller.GetReadableUrl("");

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(400));
        }

        [Test]
        public void GetReadableUrl_ShouldReturn400_WhenBlobNameIsEmpty()
        {
            // Act
            var result = _controller.GetReadableUrl("");

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(400));
        }

        [Test]
        public void GetReadableUrl_ShouldReturn400_WhenBlobNameIsWhitespace()
        {
            // Act
            var result = _controller.GetReadableUrl("   ");

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(400));
        }

        #endregion

        #region DeleteFileAsync Tests

        [Test]
        public async Task DeleteFileAsync_ShouldReturn200_WhenSuccessful()
        {
            // Arrange
            var blobName = "test-file.pdf";
            _storageServiceMock.Setup(s => s.DeleteFileAsync(blobName, false))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteFileAsync(blobName);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(200));
            _storageServiceMock.Verify(s => s.DeleteFileAsync(blobName, false), Times.Once);
        }

        [Test]
        public async Task DeleteFileAsync_ShouldReturn404_WhenFileNotFound()
        {
            // Arrange
            var blobName = "non-existent-file.pdf";
            _storageServiceMock.Setup(s => s.DeleteFileAsync(blobName, false))
                .ThrowsAsync(new AppException(CustomCode.BlobNotFound));

            // Act
            var result = await _controller.DeleteFileAsync(blobName);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public async Task DeleteFileAsync_ShouldReturn500_WhenServiceThrowsException()
        {
            // Arrange
            var blobName = "test-file.pdf";
            _storageServiceMock.Setup(s => s.DeleteFileAsync(blobName, false))
                .ThrowsAsync(new Exception("Storage service error"));

            // Act
            var result = await _controller.DeleteFileAsync(blobName);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(500));
        }

        #endregion

        #region GetStorageQuota Tests

        [Test]
        public async Task GetStorageQuota_ShouldReturn200_WhenSuccessful()
        {
            // Arrange
            var quotaDetails = new StorageQuotaDetailsResponse
            {
                UsedBytes = 1024,
                LimitBytes = 10240,
                RemainingBytes = 9216,
                UsedGB = 0.001,
                LimitGB = 0.01,
                RemainingGB = 0.009,
                UsagePercentage = 10.0
            };
            _storageQuotaServiceMock.Setup(s => s.GetStorageQuotaDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(quotaDetails);

            // Act
            var result = await _controller.GetStorageQuota();

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(200));
            _storageQuotaServiceMock.Verify(s => s.GetStorageQuotaDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetStorageQuota_ShouldReturn404_WhenSchoolIdNotFound()
        {
            // Arrange
            SetupUserContext("123e4567-e89b-12d3-a456-426614174000", "");

            // Act
            var result = await _controller.GetStorageQuota();

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public async Task GetStorageQuota_ShouldReturn404_WhenSchoolIdInvalid()
        {
            // Arrange
            SetupUserContext("123e4567-e89b-12d3-a456-426614174000", "invalid");

            // Act
            var result = await _controller.GetStorageQuota();

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public async Task GetStorageQuota_ShouldReturn500_WhenServiceThrowsException()
        {
            // Arrange
            _storageQuotaServiceMock.Setup(s => s.GetStorageQuotaDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.GetStorageQuota();

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(500));
        }

        #endregion
    }
}
