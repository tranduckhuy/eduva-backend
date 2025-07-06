using Eduva.Application.Interfaces.Services;
using Eduva.Infrastructure.Services;
using Eduva.Shared.Constants;
using Moq;

namespace Eduva.Infrastructure.Test.Services
{
    [TestFixture]
    public class SystemConfigHelperTests
    {
        private Mock<ISystemConfigService> _systemConfigServiceMock = null!;
        private SystemConfigHelper _systemConfigHelper = null!;

        #region Setup

        [SetUp]
        public void Setup()
        {
            _systemConfigServiceMock = new Mock<ISystemConfigService>();
            _systemConfigHelper = new SystemConfigHelper(_systemConfigServiceMock.Object);
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_ShouldInitializeSuccessfully_WhenValidServiceProvided()
        {
            // Arrange & Act
            var helper = new SystemConfigHelper(_systemConfigServiceMock.Object);

            // Assert
            Assert.That(helper, Is.Not.Null);
        }

        #endregion

        #region GetValueAsync Tests

        [Test]
        public async Task GetValueAsync_ShouldReturnValue_WhenServiceReturnsValue()
        {
            // Arrange
            var key = "TEST_KEY";
            var expectedValue = "test-value";
            var defaultValue = "default-value";

            _systemConfigServiceMock
                .Setup(s => s.GetByKeyAsync(key))
                .ReturnsAsync(expectedValue);

            // Act
            var result = await _systemConfigHelper.GetValueAsync(key, defaultValue);

            // Assert
            Assert.That(result, Is.EqualTo(expectedValue));
            _systemConfigServiceMock.Verify(s => s.GetByKeyAsync(key), Times.Once);
        }

        [Test]
        public async Task GetValueAsync_ShouldReturnDefaultValue_WhenServiceReturnsNull()
        {
            // Arrange
            var key = "TEST_KEY";
            var defaultValue = "default-value";

            _systemConfigServiceMock
                .Setup(s => s.GetByKeyAsync(key))
                .ReturnsAsync((string?)null);

            // Act
            var result = await _systemConfigHelper.GetValueAsync(key, defaultValue);

            // Assert
            Assert.That(result, Is.EqualTo(defaultValue));
            _systemConfigServiceMock.Verify(s => s.GetByKeyAsync(key), Times.Once);
        }

        [Test]
        public async Task GetValueAsync_ShouldReturnEmptyString_WhenServiceReturnsNullAndNoDefaultProvided()
        {
            // Arrange
            var key = "TEST_KEY";

            _systemConfigServiceMock
                .Setup(s => s.GetByKeyAsync(key))
                .ReturnsAsync((string?)null);

            // Act
            var result = await _systemConfigHelper.GetValueAsync(key);

            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
            _systemConfigServiceMock.Verify(s => s.GetByKeyAsync(key), Times.Once);
        }

        [Test]
        public async Task GetValueAsync_ShouldReturnEmptyString_WhenServiceReturnsNullAndEmptyDefaultProvided()
        {
            // Arrange
            var key = "TEST_KEY";
            var defaultValue = "";

            _systemConfigServiceMock
                .Setup(s => s.GetByKeyAsync(key))
                .ReturnsAsync((string?)null);

            // Act
            var result = await _systemConfigHelper.GetValueAsync(key, defaultValue);

            // Assert
            Assert.That(result, Is.EqualTo(""));
            _systemConfigServiceMock.Verify(s => s.GetByKeyAsync(key), Times.Once);
        }

        [Test]
        public async Task GetValueAsync_ShouldReturnValue_WhenServiceReturnsEmptyString()
        {
            // Arrange
            var key = "TEST_KEY";
            var returnedValue = "";
            var defaultValue = "default-value";

            _systemConfigServiceMock
                .Setup(s => s.GetByKeyAsync(key))
                .ReturnsAsync(returnedValue);

            // Act
            var result = await _systemConfigHelper.GetValueAsync(key, defaultValue);

            // Assert
            Assert.That(result, Is.EqualTo(returnedValue));
            _systemConfigServiceMock.Verify(s => s.GetByKeyAsync(key), Times.Once);
        }

        #endregion

        #region GetDefaultAvatarUrlAsync Tests

        [Test]
        public async Task GetDefaultAvatarUrlAsync_ShouldReturnValue_WhenConfigExists()
        {
            // Arrange
            var expectedUrl = "https://custom-avatar.com/default.jpg";

            _systemConfigServiceMock
                .Setup(s => s.GetByKeyAsync(SystemConfigKeys.DEFAULT_AVATAR_URL))
                .ReturnsAsync(expectedUrl);

            // Act
            var result = await _systemConfigHelper.GetDefaultAvatarUrlAsync();

            // Assert
            Assert.That(result, Is.EqualTo(expectedUrl));
            _systemConfigServiceMock.Verify(s => s.GetByKeyAsync(SystemConfigKeys.DEFAULT_AVATAR_URL), Times.Once);
        }

        [Test]
        public async Task GetDefaultAvatarUrlAsync_ShouldReturnDefaultValue_WhenConfigNotExists()
        {
            // Arrange
            var expectedDefault = "https://via.placeholder.com/150x150/CCCCCC/FFFFFF?text=Avatar";

            _systemConfigServiceMock
                .Setup(s => s.GetByKeyAsync(SystemConfigKeys.DEFAULT_AVATAR_URL))
                .ReturnsAsync((string?)null);

            // Act
            var result = await _systemConfigHelper.GetDefaultAvatarUrlAsync();

            // Assert
            Assert.That(result, Is.EqualTo(expectedDefault));
            _systemConfigServiceMock.Verify(s => s.GetByKeyAsync(SystemConfigKeys.DEFAULT_AVATAR_URL), Times.Once);
        }

        #endregion

        #region GetImportUsersTemplateAsync Tests

        [Test]
        public async Task GetImportUsersTemplateAsync_ShouldReturnValue_WhenConfigExists()
        {
            // Arrange
            var expectedUrl = "https://custom-template.com/import-users.xlsx";

            _systemConfigServiceMock
                .Setup(s => s.GetByKeyAsync(SystemConfigKeys.IMPORT_USERS_TEMPLATE))
                .ReturnsAsync(expectedUrl);

            // Act
            var result = await _systemConfigHelper.GetImportUsersTemplateAsync();

            // Assert
            Assert.That(result, Is.EqualTo(expectedUrl));
            _systemConfigServiceMock.Verify(s => s.GetByKeyAsync(SystemConfigKeys.IMPORT_USERS_TEMPLATE), Times.Once);
        }

        [Test]
        public async Task GetImportUsersTemplateAsync_ShouldReturnDefaultValue_WhenConfigNotExists()
        {
            // Arrange
            var expectedDefault = "https://example.com/import-users-template";

            _systemConfigServiceMock
                .Setup(s => s.GetByKeyAsync(SystemConfigKeys.IMPORT_USERS_TEMPLATE))
                .ReturnsAsync((string?)null);

            // Act
            var result = await _systemConfigHelper.GetImportUsersTemplateAsync();

            // Assert
            Assert.That(result, Is.EqualTo(expectedDefault));
            _systemConfigServiceMock.Verify(s => s.GetByKeyAsync(SystemConfigKeys.IMPORT_USERS_TEMPLATE), Times.Once);
        }

        #endregion

        #region GetPayosReturnUrlPlanAsync Tests

        [Test]
        public async Task GetPayosReturnUrlPlanAsync_ShouldReturnValue_WhenConfigExists()
        {
            // Arrange
            var expectedUrl = "https://custom-payos.com/return-plan";

            _systemConfigServiceMock
                .Setup(s => s.GetByKeyAsync(SystemConfigKeys.PAYOS_RETURN_URL_PLAN))
                .ReturnsAsync(expectedUrl);

            // Act
            var result = await _systemConfigHelper.GetPayosReturnUrlPlanAsync();

            // Assert
            Assert.That(result, Is.EqualTo(expectedUrl));
            _systemConfigServiceMock.Verify(s => s.GetByKeyAsync(SystemConfigKeys.PAYOS_RETURN_URL_PLAN), Times.Once);
        }

        [Test]
        public async Task GetPayosReturnUrlPlanAsync_ShouldReturnDefaultValue_WhenConfigNotExists()
        {
            // Arrange
            var expectedDefault = "https://example.com/payos-return-plan";

            _systemConfigServiceMock
                .Setup(s => s.GetByKeyAsync(SystemConfigKeys.PAYOS_RETURN_URL_PLAN))
                .ReturnsAsync((string?)null);

            // Act
            var result = await _systemConfigHelper.GetPayosReturnUrlPlanAsync();

            // Assert
            Assert.That(result, Is.EqualTo(expectedDefault));
            _systemConfigServiceMock.Verify(s => s.GetByKeyAsync(SystemConfigKeys.PAYOS_RETURN_URL_PLAN), Times.Once);
        }

        #endregion

        #region GetPayosReturnUrlPackAsync Tests

        [Test]
        public async Task GetPayosReturnUrlPackAsync_ShouldReturnValue_WhenConfigExists()
        {
            // Arrange
            var expectedUrl = "https://custom-payos.com/return-pack";

            _systemConfigServiceMock
                .Setup(s => s.GetByKeyAsync(SystemConfigKeys.PAYOS_RETURN_URL_PACK))
                .ReturnsAsync(expectedUrl);

            // Act
            var result = await _systemConfigHelper.GetPayosReturnUrlPackAsync();

            // Assert
            Assert.That(result, Is.EqualTo(expectedUrl));
            _systemConfigServiceMock.Verify(s => s.GetByKeyAsync(SystemConfigKeys.PAYOS_RETURN_URL_PACK), Times.Once);
        }

        [Test]
        public async Task GetPayosReturnUrlPackAsync_ShouldReturnDefaultValue_WhenConfigNotExists()
        {
            // Arrange
            var expectedDefault = "https://example.com/payos-return-pack";

            _systemConfigServiceMock
                .Setup(s => s.GetByKeyAsync(SystemConfigKeys.PAYOS_RETURN_URL_PACK))
                .ReturnsAsync((string?)null);

            // Act
            var result = await _systemConfigHelper.GetPayosReturnUrlPackAsync();

            // Assert
            Assert.That(result, Is.EqualTo(expectedDefault));
            _systemConfigServiceMock.Verify(s => s.GetByKeyAsync(SystemConfigKeys.PAYOS_RETURN_URL_PACK), Times.Once);
        }

        #endregion

        #region Integration and Edge Case Tests

        [Test]
        public async Task MultipleMethodCalls_ShouldCallServiceWithCorrectKeys()
        {
            // Arrange
            _systemConfigServiceMock
                .Setup(s => s.GetByKeyAsync(It.IsAny<string>()))
                .ReturnsAsync((string?)null);

            // Act
            await _systemConfigHelper.GetDefaultAvatarUrlAsync();
            await _systemConfigHelper.GetImportUsersTemplateAsync();
            await _systemConfigHelper.GetPayosReturnUrlPlanAsync();
            await _systemConfigHelper.GetPayosReturnUrlPackAsync();

            // Assert
            _systemConfigServiceMock.Verify(s => s.GetByKeyAsync(SystemConfigKeys.DEFAULT_AVATAR_URL), Times.Once);
            _systemConfigServiceMock.Verify(s => s.GetByKeyAsync(SystemConfigKeys.IMPORT_USERS_TEMPLATE), Times.Once);
            _systemConfigServiceMock.Verify(s => s.GetByKeyAsync(SystemConfigKeys.PAYOS_RETURN_URL_PLAN), Times.Once);
            _systemConfigServiceMock.Verify(s => s.GetByKeyAsync(SystemConfigKeys.PAYOS_RETURN_URL_PACK), Times.Once);
        }

        [Test]
        public async Task GetValueAsync_ShouldNotTrimOrModifyReturnedValue()
        {
            // Arrange
            var key = "TEST_KEY";
            var valueWithSpaces = "  value with spaces  ";

            _systemConfigServiceMock
                .Setup(s => s.GetByKeyAsync(key))
                .ReturnsAsync(valueWithSpaces);

            // Act
            var result = await _systemConfigHelper.GetValueAsync(key);

            // Assert
            Assert.That(result, Is.EqualTo(valueWithSpaces));
        }

        [Test]
        public async Task GetValueAsync_ShouldHandleSpecialCharacters()
        {
            // Arrange
            var key = "SPECIAL_KEY";
            var specialValue = "https://example.com/path?param=value&other=123#anchor";

            _systemConfigServiceMock
                .Setup(s => s.GetByKeyAsync(key))
                .ReturnsAsync(specialValue);

            // Act
            var result = await _systemConfigHelper.GetValueAsync(key);

            // Assert
            Assert.That(result, Is.EqualTo(specialValue));
        }

        #endregion
    }
}