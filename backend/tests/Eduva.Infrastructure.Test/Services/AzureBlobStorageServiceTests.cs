using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Eduva.Application.Exceptions.FileStorage;
using Eduva.Application.Interfaces.Services;
using Eduva.Infrastructure.Configurations;
using Eduva.Infrastructure.Services;
using Moq;

namespace Eduva.Infrastructure.Test.Services
{
    [TestFixture]
    public class AzureBlobStorageServiceTests
    {
        private Mock<BlobContainerClient> _containerClientMock;
        private Mock<BlobClient> _blobClientMock;
        private Mock<IStorageQuotaService> _storageQuotaServiceMock;
        private AzureBlobStorageOptions _options;
        private AzureBlobStorageService _service;

        [SetUp]
        public void Setup()
        {
            _options = new AzureBlobStorageOptions
            {
                ConnectionString = "UseDevelopmentStorage=true",
                ContainerName = "test-container",
                TemporaryContainerName = "test-temp-container",
                StorageAccountName = "test-account",
                StorageAccountKey = "test-key"
            };

            _containerClientMock = new Mock<BlobContainerClient>();
            _blobClientMock = new Mock<BlobClient>();
            _storageQuotaServiceMock = new Mock<IStorageQuotaService>();

            // Setup the container client to return our mocked blob client
            _containerClientMock.Setup(c => c.GetBlobClient(It.IsAny<string>()))
                .Returns(_blobClientMock.Object);

            // Create service with mocked dependencies
            _service = new TestableAzureBlobStorageService(_options, _containerClientMock.Object, _storageQuotaServiceMock.Object);
        }

        #region Constructor Tests

        [Test]
        public void Constructor_ShouldInitializeService_WhenValidOptionsProvided()
        {
            // Arrange
            var mockStorageQuotaService = new Mock<IStorageQuotaService>();

            // Act & Assert
            Assert.DoesNotThrow(() => new AzureBlobStorageService(_options, mockStorageQuotaService.Object));
        }

        [Test]
        public void Constructor_ShouldThrowException_WhenNullOptionsProvided()
        {
            // Arrange
            var mockStorageQuotaService = new Mock<IStorageQuotaService>();

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => new AzureBlobStorageService(null!, mockStorageQuotaService.Object));
        }

        #endregion

        #region GenerateUploadSasTokens Tests

        [Test]
        public async Task GenerateUploadSasTokens_ShouldReturnTokens_WhenValidBlobNamesProvided()
        {
            // Arrange
            var blobNames = new List<string> { "file1.pdf", "file2.jpg" };
            var expectedSasUri = new Uri("https://test.blob.core.windows.net/container/test.pdf?sv=2021-12-02&sp=cw&sr=b");

            _blobClientMock.Setup(b => b.GenerateSasUri(It.IsAny<BlobSasBuilder>()))
                .Returns(expectedSasUri);

            // Act
            var result = await _service.GenerateUploadSasTokens(blobNames);

            // Assert
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result.All(token => !string.IsNullOrEmpty(token)), Is.True);
        }

        [Test]
        public async Task GenerateUploadSasTokens_ShouldReturnEmpty_WhenEmptyListProvided()
        {
            // Arrange
            var blobNames = new List<string>();

            // Act
            var result = await _service.GenerateUploadSasTokens(blobNames);

            // Assert
            Assert.That(result, Is.Empty);
        }

        #endregion

        #region GetReadableUrl Tests

        [Test]
        public void GetReadableUrl_ShouldReturnUrlWithSasToken_WhenValidBlobUrlProvided()
        {
            // Arrange
            var blobUrl = "https://teststorage.blob.core.windows.net/container/test-file.pdf";
            var expectedQuery = "?sv=2021-12-02&sp=r&sr=b&sig=signature";
            var expectedSasUri = new Uri($"{blobUrl}{expectedQuery}");

            _blobClientMock.Setup(b => b.GenerateSasUri(It.IsAny<BlobSasBuilder>()))
                .Returns(expectedSasUri);

            // Act
            var result = _service.GetReadableUrl(blobUrl);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith(blobUrl));
            Assert.That(result, Does.Contain("?"));
            _blobClientMock.Verify(b => b.GenerateSasUri(It.Is<BlobSasBuilder>(builder =>
                builder.Resource == "b")), Times.Once);
        }

        [Test]
        public void GetReadableUrl_ShouldExtractCorrectBlobName_FromUrl()
        {
            // Arrange
            var fileName = "document.docx";
            var blobUrl = $"https://teststorage.blob.core.windows.net/container/{fileName}";
            var expectedSasUri = new Uri($"{blobUrl}?sv=2021-12-02&sp=r&sr=b");

            _blobClientMock.Setup(b => b.GenerateSasUri(It.IsAny<BlobSasBuilder>()))
                .Returns(expectedSasUri);

            // Act
            var result = _service.GetReadableUrl(blobUrl);

            // Assert
            Assert.That(result, Does.Contain(fileName));
            _containerClientMock.Verify(c => c.GetBlobClient(fileName), Times.Once);
        }

        [Test]
        public void GetReadableUrl_ShouldHandleNestedPath_InBlobUrl()
        {
            // Arrange
            var blobUrl = "https://teststorage.blob.core.windows.net/container/folder/subfolder/file.txt";
            var expectedSasUri = new Uri($"{blobUrl}?sv=2021-12-02&sp=r&sr=b");

            _blobClientMock.Setup(b => b.GenerateSasUri(It.IsAny<BlobSasBuilder>()))
                .Returns(expectedSasUri);

            // Act
            var result = _service.GetReadableUrl(blobUrl);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.StartWith(blobUrl));
            Assert.That(result, Does.Contain("file.txt"));
        }

        [Test]
        public void GetReadableUrl_ShouldThrowException_WhenInvalidUrlProvided()
        {
            // Arrange
            var invalidUrl = "not-a-valid-url";

            // Act & Assert
            Assert.Throws<UriFormatException>(() => _service.GetReadableUrl(invalidUrl));
        }

        [Test]
        public void GetReadableUrl_ShouldThrowException_WhenNullUrlProvided()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.GetReadableUrl(null!));
        }

        #endregion

        #region DeleteFileAsync Tests

        [Test]
        public async Task DeleteFileAsync_ShouldDeleteSuccessfully_WhenBlobExists()
        {
            // Arrange
            var blobName = "existing-file.pdf";
            var mockResponse = Response.FromValue(true, Mock.Of<Response>());

            _blobClientMock.Setup(b => b.DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            await _service.DeleteFileAsync(blobName);

            // Assert
            _blobClientMock.Verify(b => b.DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void DeleteFileAsync_ShouldThrowBlobNotFoundException_WhenBlobDoesNotExist()
        {
            // Arrange
            var nonExistentBlobName = "non-existent-file.pdf";
            var mockResponse = Response.FromValue(false, Mock.Of<Response>());

            _blobClientMock.Setup(b => b.DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act & Assert
            var ex = Assert.ThrowsAsync<BlobNotFoundException>(async () => await _service.DeleteFileAsync(nonExistentBlobName));
            Assert.That(ex, Is.Not.Null);
        }

        [Test]
        public async Task DeleteFileAsync_ShouldCallCorrectBlobClient_WhenValidBlobNameProvided()
        {
            // Arrange
            var blobName = "test-file.pdf";
            var mockResponse = Response.FromValue(true, Mock.Of<Response>());

            _blobClientMock.Setup(b => b.DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            await _service.DeleteFileAsync(blobName);

            // Assert
            _containerClientMock.Verify(c => c.GetBlobClient(blobName), Times.Once);
            _blobClientMock.Verify(b => b.DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region DeleteRangeFileAsync Tests
        [Test]
        public async Task DeleteRangeFileAsync_ShouldDeleteAllFiles_WhenAllBlobsExist()
        {
            // Arrange
            var blobNames = new List<string> { "file1.pdf", "file2.jpg" };
            var mockResponse = Response.FromValue(true, Mock.Of<Response>());

            _blobClientMock.Setup(b => b.DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse);

            // Act
            await _service.DeleteRangeFileAsync(blobNames);

            // Assert
            _containerClientMock.Verify(c => c.GetBlobClient(It.IsAny<string>()), Times.Exactly(2));
            _blobClientMock.Verify(b => b.DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test]
        public void DeleteRangeFileAsync_ShouldThrowBlobNotFoundException_WhenAnyBlobDoesNotExist()
        {
            // Arrange
            var blobNames = new List<string> { "existing-file.pdf", "non-existent-file.jpg" };
            var successResponse = Response.FromValue(true, Mock.Of<Response>());
            var failureResponse = Response.FromValue(false, Mock.Of<Response>());

            _blobClientMock.SetupSequence(b => b.DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(successResponse)
                .ReturnsAsync(failureResponse);

            // Act & Assert
            Assert.ThrowsAsync<BlobNotFoundException>(async () => await _service.DeleteRangeFileAsync(blobNames));
        }

        #endregion

        #region Edge Cases and Error Handling

        [Test]
        public void GetReadableUrl_ShouldHandleUrlsWithSpecialCharacters()
        {
            // Arrange
            var blobUrl = "https://storage.blob.core.windows.net/container/file%20with%20spaces.pdf";
            var expectedSasUri = new Uri($"{blobUrl}?sv=2021-12-02&sp=r&sr=b");

            _blobClientMock.Setup(b => b.GenerateSasUri(It.IsAny<BlobSasBuilder>()))
                .Returns(expectedSasUri);

            // Act & Assert
            Assert.DoesNotThrow(() => _service.GetReadableUrl(blobUrl));
        }

        [Test]
        public void GetReadableUrl_ShouldHandleUrlsWithQueryParameters()
        {
            // Arrange
            var expectedFileName = "file-with-query.txt";
            var blobUrl = $"https://storage.blob.core.windows.net/container/{expectedFileName}?existing=param";
            var expectedSasUri = new Uri($"https://storage.blob.core.windows.net/container/{expectedFileName}?sv=2021-12-02&sp=r&sr=b");

            _blobClientMock.Setup(b => b.GenerateSasUri(It.IsAny<BlobSasBuilder>()))
                .Returns(expectedSasUri);

            // Act
            var result = _service.GetReadableUrl(blobUrl);

            // Assert
            Assert.That(result, Does.Contain(expectedFileName));
        }

        #endregion

        #region Configuration Tests

        [Test]
        public void Service_ShouldUseCorrectContainerName_FromOptions()
        {
            // Arrange
            var customOptions = new AzureBlobStorageOptions
            {
                ConnectionString = "UseDevelopmentStorage=true",
                ContainerName = "custom-container-name",
                TemporaryContainerName = "test-temp-container",
                StorageAccountName = "test-account",
                StorageAccountKey = "test-key"
            };

            // Arrange
            var mockStorageQuotaService = new Mock<IStorageQuotaService>();

            // Act & Assert
            Assert.DoesNotThrow(() => new AzureBlobStorageService(customOptions, mockStorageQuotaService.Object));
        }

        [Test]
        public void Service_ShouldThrowException_WhenConnectionStringIsNull()
        {
            // Arrange
            var invalidOptions = new AzureBlobStorageOptions
            {
                ConnectionString = null!,
                ContainerName = "test-container",
                TemporaryContainerName = "test-temp-container",
                StorageAccountName = "test-account",
                StorageAccountKey = "test-key"
            };

            // Arrange
            var mockStorageQuotaService = new Mock<IStorageQuotaService>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AzureBlobStorageService(invalidOptions, mockStorageQuotaService.Object));
        }

        #endregion

        #region Performance and Behavior Tests

        [Test]
        public async Task GenerateUploadSasTokens_ShouldHandleMultipleBlobNames()
        {
            // Arrange
            var blobNames = new List<string> { "file1.pdf", "file2.jpg", "file3.docx" };
            var expectedSasUri = new Uri("https://test.blob.core.windows.net/container/test.pdf?sv=2021-12-02&sp=cw&sr=b");

            _blobClientMock.Setup(b => b.GenerateSasUri(It.IsAny<BlobSasBuilder>()))
                .Returns(expectedSasUri);

            // Act
            var result = await _service.GenerateUploadSasTokens(blobNames);

            // Assert
            Assert.That(result, Has.Count.EqualTo(3));
            Assert.That(result.All(r => !string.IsNullOrEmpty(r)), Is.True);
        }

        [Test]
        public void GetReadableUrl_ShouldReturnConsistentResults()
        {
            // Arrange
            var blobUrl = "https://storage.blob.core.windows.net/container/test.pdf";
            var expectedSasUri = new Uri($"{blobUrl}?sv=2021-12-02&sp=r&sr=b");

            _blobClientMock.Setup(b => b.GenerateSasUri(It.IsAny<BlobSasBuilder>()))
                .Returns(expectedSasUri);            // Act
            var result1 = _service.GetReadableUrl(blobUrl);
            var result2 = _service.GetReadableUrl(blobUrl);

            // Assert
            Assert.That(result1, Is.EqualTo(result2));
        }

        #endregion

    }

    // Testable version of AzureBlobStorageService that allows dependency injection
    public class TestableAzureBlobStorageService : AzureBlobStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public TestableAzureBlobStorageService(AzureBlobStorageOptions options, BlobContainerClient containerClient, IStorageQuotaService storageQuotaService)
            : base(options, storageQuotaService)
        {
            _containerClient = containerClient;

            // Use reflection to set the private field
            var field = typeof(AzureBlobStorageService).GetField("_containerClient",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(this, containerClient);
        }
    }
}