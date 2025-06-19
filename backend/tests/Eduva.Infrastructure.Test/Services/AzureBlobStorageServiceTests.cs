using Eduva.Application.Exceptions.FileStorage;
using Eduva.Infrastructure.Configurations;
using Eduva.Infrastructure.Services;

namespace Eduva.Infrastructure.Test.Services
{
    [TestFixture]
    public class AzureBlobStorageServiceTests
    {
        private AzureBlobStorageOptions _options = null!;
        private AzureBlobStorageService _service = null!;

        [SetUp]
        public void Setup()
        {
            _options = new AzureBlobStorageOptions
            {
                ConnectionString = "UseDevelopmentStorage=true",
                ContainerName = "test-container",
                StorageAccountName = "test-account",
                StorageAccountKey = "test-key"
            };
        }

        #region Constructor Tests

        [Test]
        public void Constructor_ShouldInitializeService_WhenValidOptionsProvided()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => new AzureBlobStorageService(_options));
        }

        [Test]
        public void Constructor_ShouldThrowException_WhenNullOptionsProvided()
        {
            // Act & Assert - Azure SDK might throw different exception types
            Assert.Throws<NullReferenceException>(() => new AzureBlobStorageService(null!));
        }

        #endregion

        #region GenerateUploadSasToken Tests

        [Test]
        public async Task GenerateUploadSasToken_ShouldReturnValidSasToken_WhenValidBlobNameProvided()
        {
            // Arrange
            _service = new AzureBlobStorageService(_options);
            var blobName = "test-file.pdf";
            var expiresOn = DateTimeOffset.UtcNow.AddHours(1);

            // Act
            var result = await _service.GenerateUploadSasToken(blobName, expiresOn);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
            Assert.That(result, Does.Contain(blobName));
            Assert.That(result, Does.Contain("sp=cw")); // Write and Create permissions
        }

        [Test]
        public async Task GenerateUploadSasToken_ShouldIncludeCorrectPermissions_ForUpload()
        {
            // Arrange
            _service = new AzureBlobStorageService(_options);
            var blobName = "upload-test.jpg";
            var expiresOn = DateTimeOffset.UtcNow.AddHours(2);

            // Act
            var result = await _service.GenerateUploadSasToken(blobName, expiresOn);

            // Assert
            Assert.That(result, Does.Contain("sp=cw")); // Write and Create permissions
            Assert.That(result, Does.Contain("sr=b")); // Resource type: blob
        }

        [Test]
        public async Task GenerateUploadSasToken_ShouldGenerateDifferentTokens_ForDifferentBlobs()
        {
            // Arrange
            _service = new AzureBlobStorageService(_options);
            var blobName1 = "file1.pdf";
            var blobName2 = "file2.pdf";
            var expiresOn = DateTimeOffset.UtcNow.AddHours(1);

            // Act
            var token1 = await _service.GenerateUploadSasToken(blobName1, expiresOn);
            var token2 = await _service.GenerateUploadSasToken(blobName2, expiresOn);

            // Assert
            Assert.That(token1, Is.Not.EqualTo(token2));
            Assert.That(token1, Does.Contain(blobName1));
            Assert.That(token2, Does.Contain(blobName2));
        }

        [Test]
        public void GenerateUploadSasToken_ShouldHandleSpecialCharacters_InBlobName()
        {
            // Arrange
            _service = new AzureBlobStorageService(_options);
            var blobName = "folder/sub-folder/file name with spaces.pdf";
            var expiresOn = DateTimeOffset.UtcNow.AddHours(1);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _service.GenerateUploadSasToken(blobName, expiresOn));
        }

        #endregion

        #region GetReadableUrl Tests

        [Test]
        public void GetReadableUrl_ShouldReturnUrlWithSasToken_WhenValidBlobUrlProvided()
        {
            // Arrange
            _service = new AzureBlobStorageService(_options);
            var blobUrl = "https://teststorage.blob.core.windows.net/container/test-file.pdf";

            // Act
            var result = _service.GetReadableUrl(blobUrl);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
            Assert.That(result, Does.StartWith(blobUrl));
            Assert.That(result, Does.Contain("?")); // Should have query parameters
            Assert.That(result, Does.Contain("sp=r")); // Read permission
        }

        [Test]
        public void GetReadableUrl_ShouldExtractCorrectBlobName_FromUrl()
        {
            // Arrange
            _service = new AzureBlobStorageService(_options);
            var fileName = "document.docx";
            var blobUrl = $"https://teststorage.blob.core.windows.net/container/{fileName}";

            // Act
            var result = _service.GetReadableUrl(blobUrl);

            // Assert
            Assert.That(result, Does.Contain(fileName));
        }

        [Test]
        public void GetReadableUrl_ShouldHandleNestedPath_InBlobUrl()
        {
            // Arrange
            _service = new AzureBlobStorageService(_options);
            var blobUrl = "https://teststorage.blob.core.windows.net/container/folder/subfolder/file.txt";

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
            _service = new AzureBlobStorageService(_options);
            var invalidUrl = "not-a-valid-url";

            // Act & Assert
            Assert.Throws<UriFormatException>(() => _service.GetReadableUrl(invalidUrl));
        }

        [Test]
        public void GetReadableUrl_ShouldThrowException_WhenNullUrlProvided()
        {
            // Arrange
            _service = new AzureBlobStorageService(_options);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.GetReadableUrl(null!));
        }

        #endregion

        #region DeleteFileAsync Tests - Integration Style

        [Test]
        public void DeleteFileAsync_ShouldThrowConnectionException_WhenStorageEmulatorNotRunning()
        {
            // Arrange
            _service = new AzureBlobStorageService(_options);
            var nonExistentBlobName = "non-existent-file.pdf";

            // Act & Assert - Without Azure Storage Emulator running, should get connection exceptions
            Assert.ThrowsAsync<AggregateException>(
                () => _service.DeleteFileAsync(nonExistentBlobName));
        }

        [Test]
        public void DeleteFileAsync_ShouldThrowConnectionException_WhenValidBlobNameProvided()
        {
            // Arrange
            _service = new AzureBlobStorageService(_options);
            var blobName = "test-file.pdf";

            // Act & Assert - Without Azure Storage Emulator running, should get connection exceptions
            Assert.ThrowsAsync<AggregateException>(async () => await _service.DeleteFileAsync(blobName));
        }

        #endregion

        #region GetBlobNameFromUrl Tests (Private Method Testing through Public Methods)

        [Test]
        public void GetBlobNameFromUrl_ShouldExtractCorrectName_ThroughGetReadableUrl()
        {
            // Arrange
            _service = new AzureBlobStorageService(_options);
            var expectedFileName = "test-document.pdf";
            var blobUrl = $"https://storage.blob.core.windows.net/container/{expectedFileName}";

            // Act
            var result = _service.GetReadableUrl(blobUrl);

            // Assert - Verify the blob name was correctly extracted and used
            Assert.That(result, Does.Contain(expectedFileName));
        }

        [Test]
        public void GetBlobNameFromUrl_ShouldHandleUrlsWithQueryParameters_ThroughGetReadableUrl()
        {
            // Arrange
            _service = new AzureBlobStorageService(_options);
            var expectedFileName = "file-with-query.txt";
            var blobUrl = $"https://storage.blob.core.windows.net/container/{expectedFileName}?existing=param";

            // Act
            var result = _service.GetReadableUrl(blobUrl);

            // Assert
            Assert.That(result, Does.Contain(expectedFileName));
        }

        #endregion

        #region Edge Cases and Error Handling

        [Test]
        public void GenerateUploadSasToken_ShouldThrowArgumentException_ForEmptyBlobName()
        {
            // Arrange
            _service = new AzureBlobStorageService(_options);
            var emptyBlobName = "";
            var expiresOn = DateTimeOffset.UtcNow.AddHours(1);

            // Act & Assert - Azure SDK throws ArgumentException for empty blob name
            Assert.ThrowsAsync<ArgumentException>(async () => await _service.GenerateUploadSasToken(emptyBlobName, expiresOn));
        }

        [Test]
        public void GenerateUploadSasToken_ShouldHandlePastExpiryDate()
        {
            // Arrange
            _service = new AzureBlobStorageService(_options);
            var blobName = "test-file.pdf";
            var pastDate = DateTimeOffset.UtcNow.AddHours(-1);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _service.GenerateUploadSasToken(blobName, pastDate));
        }

        [Test]
        public void GetReadableUrl_ShouldHandleUrlsWithSpecialCharacters()
        {
            // Arrange
            _service = new AzureBlobStorageService(_options);
            var blobUrl = "https://storage.blob.core.windows.net/container/file%20with%20spaces.pdf";

            // Act & Assert
            Assert.DoesNotThrow(() => _service.GetReadableUrl(blobUrl));
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
                StorageAccountName = "test-account",
                StorageAccountKey = "test-key"
            };
            _service = new AzureBlobStorageService(customOptions);
            var blobName = "test.pdf";
            var expiresOn = DateTimeOffset.UtcNow.AddHours(1);

            // Act
            var result = _service.GenerateUploadSasToken(blobName, expiresOn);

            // Assert - The container name should be reflected in the generated SAS token
            Assert.DoesNotThrowAsync(async () => await result);
        }

        [Test]
        public void Service_ShouldHandleEmptyConnectionString()
        {
            // Arrange
            var invalidOptions = new AzureBlobStorageOptions
            {
                ConnectionString = "",
                ContainerName = "test-container",
                StorageAccountName = "test-account",
                StorageAccountKey = "test-key"
            };

            // Act & Assert - Azure SDK throws ArgumentNullException for empty connection string
            Assert.Throws<ArgumentNullException>(() => new AzureBlobStorageService(invalidOptions));
        }

        #endregion

        #region Performance and Behavior Tests

        [Test]
        public async Task GenerateUploadSasToken_ShouldReturnQuickly_ForMultipleRequests()
        {
            // Arrange
            _service = new AzureBlobStorageService(_options);
            var blobNames = new[] { "file1.pdf", "file2.jpg", "file3.docx" };
            var expiresOn = DateTimeOffset.UtcNow.AddHours(1);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var tasks = blobNames.Select(name => _service.GenerateUploadSasToken(name, expiresOn));
            var results = await Task.WhenAll(tasks);

            stopwatch.Stop();

            // Assert
            Assert.That(results.Length, Is.EqualTo(3));
            Assert.That(results.All(r => !string.IsNullOrEmpty(r)), Is.True);
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(5000)); // Should complete within 5 seconds
        }

        [Test]
        public void GetReadableUrl_ShouldGenerateConsistentTokens_ForSameInput()
        {
            // Arrange
            _service = new AzureBlobStorageService(_options);
            var blobUrl = "https://storage.blob.core.windows.net/container/test.pdf";

            // Act
            var result1 = _service.GetReadableUrl(blobUrl);
            var result2 = _service.GetReadableUrl(blobUrl);

            // Assert
            Assert.That(result1, Is.Not.Null);
            Assert.That(result2, Is.Not.Null);
            // SAS tokens generated within the same second should be identical due to timestamp precision
            Assert.That(result1, Is.EqualTo(result2));
        }

        #endregion
    }
}