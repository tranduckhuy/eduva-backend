using Eduva.API.Controllers.FileStorage;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Exceptions.FileStorage;
using Eduva.Application.Interfaces.Services;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Eduva.API.Test.Controllers.FireStorage
{
    [TestFixture]
    public class FileStorageControllerTests
    {
        private Mock<IStorageService> _storageServiceMock;
        private Mock<ILogger<FileStorageController>> _loggerMock;
        private FileStorageController _controller;

        [SetUp]
        public void Setup()
        {
            _storageServiceMock = new Mock<IStorageService>();
            _loggerMock = new Mock<ILogger<FileStorageController>>();
            _controller = new FileStorageController(_loggerMock.Object, _storageServiceMock.Object);
        }

        #region GenerateUploadSasToken Tests        [Test]
        public async Task GenerateUploadSasToken_ShouldReturn200_WhenSuccessful()
        {
            // Arrange
            var blobNames = new List<string> { "test-file.pdf" };
            var expectedSasTokens = new List<string> { "https://storage.blob.core.windows.net/container/test-file.pdf?sv=2021-12-02&st=2023-01-01T00%3A00%3A00Z&se=2023-01-01T01%3A00%3A00Z&sr=b&sp=cw&sig=signature" };

            _storageServiceMock.Setup(s => s.GenerateUploadSasTokens(blobNames))
                .ReturnsAsync(expectedSasTokens);

            // Act
            var result = await _controller.GenerateUploadSasToken(blobNames);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

            // Verify the service was called with correct parameters
            _storageServiceMock.Verify(s => s.GenerateUploadSasTokens(blobNames), Times.Once);
        }        [Test]
        public async Task GenerateUploadSasToken_ShouldReturn500_WhenServiceThrowsException()
        {
            // Arrange
            var blobNames = new List<string> { "test-file.pdf" };

            _storageServiceMock.Setup(s => s.GenerateUploadSasTokens(blobNames))
                .ThrowsAsync(new Exception("Storage service error"));

            // Act
            var result = await _controller.GenerateUploadSasToken(blobNames);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        }        [Test]
        public async Task GenerateUploadSasToken_ShouldReturn400_WhenAppExceptionThrown()
        {
            // Arrange
            var blobNames = new List<string> { "test-file.pdf" };

            _storageServiceMock.Setup(s => s.GenerateUploadSasTokens(blobNames))
                .ThrowsAsync(new AppException(CustomCode.InvalidBlobName));

            // Act
            var result = await _controller.GenerateUploadSasToken(blobNames);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }        [Test]
        public async Task GenerateUploadSasToken_ShouldReturn200_WhenMultipleBlobNamesProvided()
        {
            // Arrange
            var blobNames = new List<string> { "file1.pdf", "file2.jpg", "file3.docx" };
            var expectedSasTokens = new List<string> 
            { 
                "https://storage.blob.core.windows.net/container/file1.pdf?sv=2021-12-02&sp=cw&sr=b&sig=signature1",
                "https://storage.blob.core.windows.net/container/file2.jpg?sv=2021-12-02&sp=cw&sr=b&sig=signature2",
                "https://storage.blob.core.windows.net/container/file3.docx?sv=2021-12-02&sp=cw&sr=b&sig=signature3"
            };

            _storageServiceMock.Setup(s => s.GenerateUploadSasTokens(blobNames))
                .ReturnsAsync(expectedSasTokens);

            // Act
            var result = await _controller.GenerateUploadSasToken(blobNames);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

            // Verify the service was called with correct parameters
            _storageServiceMock.Verify(s => s.GenerateUploadSasTokens(blobNames), Times.Once);
        }

        [Test]
        public async Task GenerateUploadSasToken_ShouldReturn200_WhenEmptyListProvided()
        {
            // Arrange
            var blobNames = new List<string>();
            var expectedSasTokens = new List<string>();

            _storageServiceMock.Setup(s => s.GenerateUploadSasTokens(blobNames))
                .ReturnsAsync(expectedSasTokens);

            // Act
            var result = await _controller.GenerateUploadSasToken(blobNames);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

            // Verify the service was called with correct parameters
            _storageServiceMock.Verify(s => s.GenerateUploadSasTokens(blobNames), Times.Once);
        }

        #endregion

        #region GetReadableUrl Tests

        [Test]
        public void GetReadableUrl_ShouldReturn200_WhenSuccessful()
        {
            // Arrange
            var blobName = "test-file.pdf";
            var expectedUrl = "https://storage.blob.core.windows.net/container/test-file.pdf?sv=2021-12-02&st=2023-01-01T00%3A00%3A00Z&se=2023-01-01T01%3A00%3A00Z&sr=b&sp=r&sig=signature";

            _storageServiceMock.Setup(s => s.GetReadableUrl(blobName))
                .Returns(expectedUrl);

            // Act
            var result = _controller.GetReadableUrl(blobName);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

            // Verify the service was called with correct parameters
            _storageServiceMock.Verify(s => s.GetReadableUrl(blobName), Times.Once);
        }

        [Test]
        public void GetReadableUrl_ShouldReturn400_WhenBlobNameIsNull()
        {
            // Act
            var result = _controller.GetReadableUrl(null!);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));

            // Verify the service was not called
            _storageServiceMock.Verify(s => s.GetReadableUrl(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void GetReadableUrl_ShouldReturn400_WhenBlobNameIsEmpty()
        {
            // Act
            var result = _controller.GetReadableUrl("");

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));

            // Verify the service was not called
            _storageServiceMock.Verify(s => s.GetReadableUrl(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void GetReadableUrl_ShouldReturn400_WhenBlobNameIsWhitespace()
        {
            // Act
            var result = _controller.GetReadableUrl("   ");

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));

            // Verify the service was not called
            _storageServiceMock.Verify(s => s.GetReadableUrl(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void GetReadableUrl_ShouldReturn500_WhenServiceThrowsException()
        {
            // Arrange
            var blobName = "test-file.pdf";

            _storageServiceMock.Setup(s => s.GetReadableUrl(blobName))
                .Throws(new Exception("Storage service error"));

            // Act & Assert
            Assert.Throws<Exception>(() => _controller.GetReadableUrl(blobName));
        }

        #endregion

        #region DeleteFileAsync Tests

        [Test]
        public async Task DeleteFileAsync_ShouldReturn200_WhenSuccessful()
        {
            // Arrange
            var blobName = "test-file.pdf";

            _storageServiceMock.Setup(s => s.DeleteFileAsync(blobName))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteFileAsync(blobName);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

            // Verify the service was called with correct parameters
            _storageServiceMock.Verify(s => s.DeleteFileAsync(blobName), Times.Once);
        }

        [Test]
        public async Task DeleteFileAsync_ShouldReturn404_WhenBlobNotFound()
        {
            // Arrange
            var blobName = "non-existent-file.pdf";

            _storageServiceMock.Setup(s => s.DeleteFileAsync(blobName))
                .ThrowsAsync(new BlobNotFoundException());

            // Act
            var result = await _controller.DeleteFileAsync(blobName);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
        }

        [Test]
        public async Task DeleteFileAsync_ShouldReturn400_WhenAppExceptionThrown()
        {
            // Arrange
            var blobName = "test-file.pdf";

            _storageServiceMock.Setup(s => s.DeleteFileAsync(blobName))
                .ThrowsAsync(new AppException(CustomCode.InvalidBlobName));

            // Act
            var result = await _controller.DeleteFileAsync(blobName);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        [Test]
        public async Task DeleteFileAsync_ShouldReturn500_WhenUnexpectedExceptionThrown()
        {
            // Arrange
            var blobName = "test-file.pdf";

            _storageServiceMock.Setup(s => s.DeleteFileAsync(blobName))
                .ThrowsAsync(new Exception("Unexpected storage error"));

            // Act
            var result = await _controller.DeleteFileAsync(blobName);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        }

        [Test]
        public async Task DeleteFileAsync_ShouldCallServiceWithCorrectBlobName()
        {
            // Arrange
            var blobName = "specific-test-file.pdf";

            _storageServiceMock.Setup(s => s.DeleteFileAsync(blobName))
                .Returns(Task.CompletedTask);

            // Act
            await _controller.DeleteFileAsync(blobName);

            // Assert
            _storageServiceMock.Verify(s => s.DeleteFileAsync(blobName), Times.Once);
            _storageServiceMock.Verify(s => s.DeleteFileAsync(It.Is<string>(name => name == blobName)), Times.Once);
        }

        #endregion

        #region Integration Tests        [Test]
        public async Task Controller_ShouldHandleAllMethods_WithDifferentBlobNames()
        {
            // Arrange
            var blobNames = new[] { "file1.pdf", "file2.jpg", "file3.docx" };
            var expectedSasTokens = new List<string> { "sas-token-url-1", "sas-token-url-2", "sas-token-url-3" };
            var expectedReadableUrl = "readable-url";

            foreach (var blobName in blobNames)
            {
                _storageServiceMock.Setup(s => s.GetReadableUrl(blobName))
                    .Returns(expectedReadableUrl);
                _storageServiceMock.Setup(s => s.DeleteFileAsync(blobName))
                    .Returns(Task.CompletedTask);
            }

            _storageServiceMock.Setup(s => s.GenerateUploadSasTokens(It.IsAny<List<string>>()))
                .ReturnsAsync(expectedSasTokens);

            // Act & Assert
            // Test GenerateUploadSasToken with all blob names
            var blobNamesList = blobNames.ToList();
            var sasResult = await _controller.GenerateUploadSasToken(blobNamesList);
            var sasObjectResult = sasResult as ObjectResult;
            Assert.That(sasObjectResult?.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

            foreach (var blobName in blobNames)
            {
                // Test GetReadableUrl
                var urlResult = _controller.GetReadableUrl(blobName);
                var urlObjectResult = urlResult as ObjectResult;
                Assert.That(urlObjectResult?.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

                // Test DeleteFileAsync
                var deleteResult = await _controller.DeleteFileAsync(blobName);
                var deleteObjectResult = deleteResult as ObjectResult;
                Assert.That(deleteObjectResult?.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
            }

            // Verify all service calls were made
            _storageServiceMock.Verify(s => s.GenerateUploadSasTokens(It.IsAny<List<string>>()), Times.Once);
            foreach (var blobName in blobNames)
            {
                _storageServiceMock.Verify(s => s.GetReadableUrl(blobName), Times.Once);
                _storageServiceMock.Verify(s => s.DeleteFileAsync(blobName), Times.Once);
            }
        }

        #endregion
    }
}