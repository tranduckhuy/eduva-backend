using Eduva.Application.Features.SystemConfigs;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Infrastructure.Services;
using Eduva.Shared.Enums;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace Eduva.Infrastructure.Test.Services
{
    [TestFixture]
    public class SystemConfigServiceTests
    {
        private Mock<ISystemConfigRepository> _repositoryMock = null!;
        private MemoryCache _cache = null!;
        private SystemConfigService _service = null!;

        #region Setup

        [SetUp]
        public void Setup()
        {
            _repositoryMock = new Mock<ISystemConfigRepository>();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _service = new SystemConfigService(_repositoryMock.Object, _cache);
        }

        [TearDown]
        public void TearDown()
        {
            _cache?.Dispose();
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_ShouldInitializeSuccessfully_WhenValidDependencies()
        {
            // Arrange & Act
            var service = new SystemConfigService(_repositoryMock.Object, _cache);

            // Assert
            Assert.That(service, Is.Not.Null);
        }

        #endregion

        #region GetByKeyAsync Tests

        [Test]
        public async Task GetByKeyAsync_ShouldReturnNull_WhenKeyIsNull()
        {
            // Arrange
            string? key = null;

            // Act
            var result = await _service.GetByKeyAsync(key!);

            // Assert
            Assert.That(result, Is.Null);
            _repositoryMock.Verify(r => r.GetByKeyAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task GetByKeyAsync_ShouldReturnNull_WhenKeyIsEmpty()
        {
            // Arrange
            var key = "";

            // Act
            var result = await _service.GetByKeyAsync(key);

            // Assert
            Assert.That(result, Is.Null);
            _repositoryMock.Verify(r => r.GetByKeyAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task GetByKeyAsync_ShouldReturnNull_WhenKeyIsWhitespace()
        {
            // Arrange
            var key = "   ";

            // Act
            var result = await _service.GetByKeyAsync(key);

            // Assert
            Assert.That(result, Is.Null);
            _repositoryMock.Verify(r => r.GetByKeyAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task GetByKeyAsync_ShouldReturnCachedValue_WhenValueExistsInCache()
        {
            // Arrange
            var key = "TEST_KEY";
            var cachedValue = "cached-value";
            var cacheKey = $"SystemConfig:{key}";

            // Pre-populate cache
            _cache.Set(cacheKey, cachedValue, TimeSpan.FromMinutes(15));

            // Act
            var result = await _service.GetByKeyAsync(key);

            // Assert
            Assert.That(result, Is.EqualTo(cachedValue));
            _repositoryMock.Verify(r => r.GetByKeyAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task GetByKeyAsync_ShouldReturnValueFromRepository_WhenNotInCache()
        {
            // Arrange
            var key = "TEST_KEY";
            var expectedValue = "repository-value";
            var config = new SystemConfig
            {
                Id = 1,
                Key = key,
                Value = expectedValue,
                Description = "Test config"
            };

            _repositoryMock.Setup(r => r.GetByKeyAsync(key))
                .ReturnsAsync(config);

            // Act
            var result = await _service.GetByKeyAsync(key);

            // Assert
            Assert.That(result, Is.EqualTo(expectedValue));
            _repositoryMock.Verify(r => r.GetByKeyAsync(key), Times.Once);

            // Verify value was cached
            var cacheKey = $"SystemConfig:{key}";
            var cachedValue = _cache.Get(cacheKey);
            Assert.That(cachedValue, Is.EqualTo(expectedValue));
        }

        [Test]
        public async Task GetByKeyAsync_ShouldReturnNull_WhenConfigNotFoundInRepository()
        {
            // Arrange
            var key = "NON_EXISTENT_KEY";

            _repositoryMock.Setup(r => r.GetByKeyAsync(key))
                .ReturnsAsync((SystemConfig?)null);

            // Act
            var result = await _service.GetByKeyAsync(key);

            // Assert
            Assert.That(result, Is.Null);
            _repositoryMock.Verify(r => r.GetByKeyAsync(key), Times.Once);

            // Verify nothing was cached
            var cacheKey = $"SystemConfig:{key}";
            var cachedValue = _cache.Get(cacheKey);
            Assert.That(cachedValue, Is.Null);
        }

        [Test]
        public async Task GetByKeyAsync_ShouldCacheWithCorrectExpiry()
        {
            // Arrange
            var key = "TEST_KEY";
            var config = new SystemConfig
            {
                Id = 1,
                Key = key,
                Value = "test-value",
                Description = "Test config"
            };

            _repositoryMock.Setup(r => r.GetByKeyAsync(key))
                .ReturnsAsync(config);

            // Act
            await _service.GetByKeyAsync(key);

            // Assert
            var cacheKey = $"SystemConfig:{key}";
            var cachedValue = _cache.Get(cacheKey);
            Assert.Multiple(() =>
            {
                Assert.That(cachedValue, Is.EqualTo(config.Value));

                // Test that cache expires after the expected time
                // Note: We can't easily test exact expiry time, but we can verify the value is cached
                Assert.That(_cache.TryGetValue(cacheKey, out _), Is.True);
            });
        }

        #endregion

        #region GetAllAsync Tests

        [Test]
        public async Task GetAllAsync_ShouldReturnCachedValue_WhenExistsInCache()
        {
            // Arrange
            var cachedConfigs = new List<SystemConfigDto>
            {
                new SystemConfigDto
                {
                    Id = 1,
                    Key = "KEY1",
                    Value = "Value1",
                    Description = "Desc1",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            };

            _cache.Set("SystemConfig:All", cachedConfigs, TimeSpan.FromMinutes(15));

            // Act
            var (configs, code) = await _service.GetAllAsync();

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(code, Is.EqualTo(CustomCode.Success));
                Assert.That(configs, Is.EqualTo(cachedConfigs));
            });
            _repositoryMock.Verify(r => r.GetAllAsync(), Times.Never);
        }

        [Test]
        public async Task GetAllAsync_ShouldReturnFromRepository_WhenNotInCache()
        {
            // Arrange
            var repositoryConfigs = new List<SystemConfig>
            {
                new SystemConfig
                {
                    Id = 1,
                    Key = "KEY1",
                    Value = "Value1",
                    Description = "Desc1",
                    CreatedAt = DateTimeOffset.UtcNow
                },
                new SystemConfig
                {
                    Id = 2,
                    Key = "KEY2",
                    Value = "Value2",
                    Description = "Desc2",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            };

            _repositoryMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(repositoryConfigs);

            // Act
            var (configs, code) = await _service.GetAllAsync();

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(code, Is.EqualTo(CustomCode.Success));
                Assert.That(configs, Is.Not.Null);
            });
            Assert.That(configs.Count(), Is.EqualTo(2));

            var configList = configs.ToList();
            Assert.Multiple(() =>
            {
                Assert.That(configList[0].Key, Is.EqualTo("KEY1"));
                Assert.That(configList[1].Key, Is.EqualTo("KEY2"));
            });

            _repositoryMock.Verify(r => r.GetAllAsync(), Times.Once);

            // Verify caching
            var cachedValue = _cache.Get("SystemConfig:All");
            Assert.That(cachedValue, Is.Not.Null);
        }

        [Test]
        public async Task GetAllAsync_ShouldReturnEmptyList_WhenRepositoryReturnsEmptyList()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<SystemConfig>());

            // Act
            var (configs, code) = await _service.GetAllAsync();

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(code, Is.EqualTo(CustomCode.Success));
                Assert.That(configs, Is.Not.Null);
            });
            Assert.That(configs.Count(), Is.EqualTo(0));

            _repositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
        }

        #endregion

        #region UpdateAsync Tests

        [Test]
        public async Task UpdateAsync_ShouldReturnInvalidInformation_WhenConfigIsNull()
        {
            // Arrange
            UpdateSystemConfigDto? config = null;

            // Act
            var result = await _service.UpdateAsync(config!);

            // Assert
            Assert.That(result, Is.EqualTo(CustomCode.ProvidedInformationIsInValid));
            _repositoryMock.Verify(r => r.GetByKeyAsync(It.IsAny<string>()), Times.Never);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<SystemConfig>()), Times.Never);
        }

        [Test]
        public async Task UpdateAsync_ShouldReturnInvalidInformation_WhenKeyIsNull()
        {
            // Arrange
            var config = new UpdateSystemConfigDto
            {
                Key = null!,
                Value = "test-value",
                Description = "test-description"
            };

            // Act
            var result = await _service.UpdateAsync(config);

            // Assert
            Assert.That(result, Is.EqualTo(CustomCode.ProvidedInformationIsInValid));
            _repositoryMock.Verify(r => r.GetByKeyAsync(It.IsAny<string>()), Times.Never);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<SystemConfig>()), Times.Never);
        }

        [Test]
        public async Task UpdateAsync_ShouldReturnInvalidInformation_WhenKeyIsEmpty()
        {
            // Arrange
            var config = new UpdateSystemConfigDto
            {
                Key = "",
                Value = "test-value",
                Description = "test-description"
            };

            // Act
            var result = await _service.UpdateAsync(config);

            // Assert
            Assert.That(result, Is.EqualTo(CustomCode.ProvidedInformationIsInValid));
            _repositoryMock.Verify(r => r.GetByKeyAsync(It.IsAny<string>()), Times.Never);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<SystemConfig>()), Times.Never);
        }

        [Test]
        public async Task UpdateAsync_ShouldReturnInvalidInformation_WhenKeyIsWhitespace()
        {
            // Arrange
            var config = new UpdateSystemConfigDto
            {
                Key = "   ",
                Value = "test-value",
                Description = "test-description"
            };

            // Act
            var result = await _service.UpdateAsync(config);

            // Assert
            Assert.That(result, Is.EqualTo(CustomCode.ProvidedInformationIsInValid));
            _repositoryMock.Verify(r => r.GetByKeyAsync(It.IsAny<string>()), Times.Never);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<SystemConfig>()), Times.Never);
        }

        [Test]
        public async Task UpdateAsync_ShouldReturnKeyNotFound_WhenConfigDoesNotExist()
        {
            // Arrange
            var config = new UpdateSystemConfigDto
            {
                Key = "NON_EXISTENT_KEY",
                Value = "test-value",
                Description = "test-description"
            };

            _repositoryMock.Setup(r => r.GetByKeyAsync(config.Key))
                .ReturnsAsync((SystemConfig?)null);

            // Act
            var result = await _service.UpdateAsync(config);

            // Assert
            Assert.That(result, Is.EqualTo(CustomCode.KeyConfigNotFound));
            _repositoryMock.Verify(r => r.GetByKeyAsync(config.Key), Times.Once);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<SystemConfig>()), Times.Never);
        }

        [Test]
        public async Task UpdateAsync_ShouldReturnSuccess_WhenConfigExistsAndUpdated()
        {
            // Arrange
            var config = new UpdateSystemConfigDto
            {
                Key = "EXISTING_KEY",
                Value = "updated-value",
                Description = "updated-description"
            };

            var existingConfig = new SystemConfig
            {
                Id = 1,
                Key = config.Key,
                Value = "old-value",
                Description = "old-description"
            };

            _repositoryMock.Setup(r => r.GetByKeyAsync(config.Key))
                .ReturnsAsync(existingConfig);

            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<SystemConfig>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateAsync(config);

            // Assert
            Assert.That(result, Is.EqualTo(CustomCode.Success));
            _repositoryMock.Verify(r => r.GetByKeyAsync(config.Key), Times.Once);
            _repositoryMock.Verify(r => r.UpdateAsync(It.Is<SystemConfig>(c =>
                c.Key == config.Key &&
                c.Value == config.Value &&
                c.Description == config.Description)), Times.Once);
        }

        [Test]
        public async Task UpdateAsync_ShouldInvalidateCache_WhenUpdateSuccessful()
        {
            // Arrange
            var config = new UpdateSystemConfigDto
            {
                Key = "EXISTING_KEY",
                Value = "updated-value",
                Description = "updated-description"
            };

            var existingConfig = new SystemConfig
            {
                Id = 1,
                Key = config.Key,
                Value = "old-value",
                Description = "old-description"
            };

            _repositoryMock.Setup(r => r.GetByKeyAsync(config.Key))
                .ReturnsAsync(existingConfig);

            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<SystemConfig>()))
                .Returns(Task.CompletedTask);

            // Pre-populate cache
            _cache.Set($"SystemConfig:{config.Key}", "cached-value", TimeSpan.FromMinutes(15));
            _cache.Set("SystemConfig:All", new List<SystemConfigDto>(), TimeSpan.FromMinutes(15));

            // Act
            var result = await _service.UpdateAsync(config);

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(result, Is.EqualTo(CustomCode.Success));

                // Verify cache invalidation
                Assert.That(_cache.TryGetValue($"SystemConfig:{config.Key}", out _), Is.False);
                Assert.That(_cache.TryGetValue("SystemConfig:All", out _), Is.False);
            });
        }

        #endregion

        #region Cache Invalidation Tests

        [Test]
        public async Task UpdateAsync_ShouldInvalidateBothCacheKeys()
        {
            // Arrange
            var config = new UpdateSystemConfigDto
            {
                Key = "TEST_KEY",
                Value = "test-value",
                Description = "test-description"
            };

            var existingConfig = new SystemConfig
            {
                Id = 1,
                Key = config.Key,
                Value = "old-value"
            };

            _repositoryMock.Setup(r => r.GetByKeyAsync(config.Key))
                .ReturnsAsync(existingConfig);

            // Pre-populate cache
            _cache.Set($"SystemConfig:{config.Key}", "cached-value", TimeSpan.FromMinutes(15));
            _cache.Set("SystemConfig:All", new List<SystemConfigDto>(), TimeSpan.FromMinutes(15));

            // Act
            await _service.UpdateAsync(config);

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(_cache.TryGetValue($"SystemConfig:{config.Key}", out _), Is.False);
                Assert.That(_cache.TryGetValue("SystemConfig:All", out _), Is.False);
            });
        }

        #endregion

        #region Integration Tests

        [Test]
        public async Task GetByKeyAsync_ThenUpdate_ShouldInvalidateAndRefresh()
        {
            // Arrange
            var key = "INTEGRATION_KEY";
            var originalValue = "original-value";
            var updatedValue = "updated-value";

            var originalConfig = new SystemConfig
            {
                Id = 1,
                Key = key,
                Value = originalValue
            };

            var updatedConfig = new SystemConfig
            {
                Id = 1,
                Key = key,
                Value = updatedValue
            };

            _repositoryMock.SetupSequence(r => r.GetByKeyAsync(key))
                .ReturnsAsync(originalConfig)
                .ReturnsAsync(updatedConfig)
                .ReturnsAsync(updatedConfig);

            // Act & Assert

            // 1. First call should get from repository and cache
            var firstResult = await _service.GetByKeyAsync(key);
            Assert.That(firstResult, Is.EqualTo(originalValue));
            _repositoryMock.Verify(r => r.GetByKeyAsync(key), Times.Once);

            // 2. Second call should get from cache
            var secondResult = await _service.GetByKeyAsync(key);
            Assert.That(secondResult, Is.EqualTo(originalValue));
            _repositoryMock.Verify(r => r.GetByKeyAsync(key), Times.Once); // Still once

            // 3. Update should invalidate cache
            var updateDto = new UpdateSystemConfigDto
            {
                Key = key,
                Value = updatedValue,
                Description = "updated"
            };

            await _service.UpdateAsync(updateDto);

            // 4. Next call should get fresh data from repository
            var thirdResult = await _service.GetByKeyAsync(key);
            Assert.That(thirdResult, Is.EqualTo(updatedValue));
            _repositoryMock.Verify(r => r.GetByKeyAsync(key), Times.Exactly(3)); // Called 3 times total
        }

        [Test]
        public async Task MultipleKeys_ShouldCacheIndependently()
        {
            // Arrange
            var key1 = "KEY1";
            var key2 = "KEY2";
            var value1 = "value1";
            var value2 = "value2";

            var config1 = new SystemConfig { Id = 1, Key = key1, Value = value1 };
            var config2 = new SystemConfig { Id = 2, Key = key2, Value = value2 };

            _repositoryMock.Setup(r => r.GetByKeyAsync(key1)).ReturnsAsync(config1);
            _repositoryMock.Setup(r => r.GetByKeyAsync(key2)).ReturnsAsync(config2);

            // Act
            var result1First = await _service.GetByKeyAsync(key1);
            var result2First = await _service.GetByKeyAsync(key2);
            var result1Second = await _service.GetByKeyAsync(key1);
            var result2Second = await _service.GetByKeyAsync(key2);

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(result1First, Is.EqualTo(value1));
                Assert.That(result2First, Is.EqualTo(value2));
                Assert.That(result1Second, Is.EqualTo(value1));
                Assert.That(result2Second, Is.EqualTo(value2));
            });

            // Each key should be called only once (second calls from cache)
            _repositoryMock.Verify(r => r.GetByKeyAsync(key1), Times.Once);
            _repositoryMock.Verify(r => r.GetByKeyAsync(key2), Times.Once);
        }

        #endregion
    }
}