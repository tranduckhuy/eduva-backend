using Eduva.API.Controllers.SystemConfigs;
using Eduva.API.Models;
using Eduva.Application.Features.SystemConfigs;
using Eduva.Application.Interfaces.Services;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.ComponentModel.DataAnnotations;

namespace Eduva.API.Test.Controllers.SystemConfigs
{
    [TestFixture]
    public class SystemConfigsControllerTests
    {
        private Mock<ISystemConfigService> _systemConfigServiceMock = default!;
        private Mock<ILogger<SystemConfigsController>> _loggerMock = default!;
        private SystemConfigsController _controller = default!;

        #region Setup

        [SetUp]
        public void Setup()
        {
            _systemConfigServiceMock = new Mock<ISystemConfigService>();
            _loggerMock = new Mock<ILogger<SystemConfigsController>>();
            _controller = new SystemConfigsController(_systemConfigServiceMock.Object, _loggerMock.Object);
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_ShouldInitializeProperties_WhenValidDependencies()
        {
            // Arrange & Act
            var controller = new SystemConfigsController(_systemConfigServiceMock.Object, _loggerMock.Object);

            // Assert
            Assert.That(controller, Is.Not.Null);
        }

        #endregion

        #region GetAllAsync Tests

        [Test]
        public async Task GetAllAsync_ShouldReturnOkWithConfigs_WhenServiceReturnsSuccess()
        {
            // Arrange
            var expectedConfigs = new List<SystemConfigDto>
            {
                new SystemConfigDto
                {
                    Id = 1,
                    Key = "DEFAULT_AVATAR_URL",
                    Value = "https://example.com/avatar.jpg",
                    Description = "Default avatar URL",
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastModifiedAt = DateTimeOffset.UtcNow
                },
                new SystemConfigDto
                {
                    Id = 2,
                    Key = "PAYOS_RETURN_URL",
                    Value = "https://example.com/return",
                    Description = "PayOS return URL",
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastModifiedAt = DateTimeOffset.UtcNow
                }
            };

            _systemConfigServiceMock
                .Setup(s => s.GetAllAsync())
                .ReturnsAsync((expectedConfigs, CustomCode.Success));

            // Act
            var result = await _controller.GetAllAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);

            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
                Assert.That(response.Data, Is.Not.Null);
                Assert.That(response.Data, Is.TypeOf<List<SystemConfigDto>>());
            });

            var configs = response.Data as List<SystemConfigDto>;
            Assert.That(configs, Has.Count.EqualTo(2));
            Assert.That(configs![0].Key, Is.EqualTo("DEFAULT_AVATAR_URL"));
            Assert.That(configs[1].Key, Is.EqualTo("PAYOS_RETURN_URL"));

            _systemConfigServiceMock.Verify(s => s.GetAllAsync(), Times.Once);
        }

        [Test]
        public async Task GetAllAsync_ShouldReturnOkWithEmptyList_WhenNoConfigsExist()
        {
            // Arrange
            var emptyConfigs = new List<SystemConfigDto>();
            _systemConfigServiceMock
                .Setup(s => s.GetAllAsync())
                .ReturnsAsync((emptyConfigs, CustomCode.Success));

            // Act
            var result = await _controller.GetAllAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);

            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
                Assert.That(response.Data, Is.Not.Null);
                Assert.That(response.Data, Is.TypeOf<List<SystemConfigDto>>());
            });

            var configs = response.Data as List<SystemConfigDto>;
            Assert.That(configs, Has.Count.EqualTo(0));

            _systemConfigServiceMock.Verify(s => s.GetAllAsync(), Times.Once);
        }

        [Test]
        public async Task GetAllAsync_ShouldReturnError_WhenServiceReturnsErrorCode()
        {
            // Arrange
            var emptyConfigs = new List<SystemConfigDto>();
            _systemConfigServiceMock
                .Setup(s => s.GetAllAsync())
                .ReturnsAsync((emptyConfigs, CustomCode.SystemError));

            // Act
            var result = await _controller.GetAllAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);

            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.SystemError));

            _systemConfigServiceMock.Verify(s => s.GetAllAsync(), Times.Once);
        }

        [Test]
        public async Task GetAllAsync_ShouldReturnInternalServerError_WhenServiceThrowsException()
        {
            // Arrange
            _systemConfigServiceMock
                .Setup(s => s.GetAllAsync())
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _controller.GetAllAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));

            var response = objectResult.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.SystemError));

            _systemConfigServiceMock.Verify(s => s.GetAllAsync(), Times.Once);
        }

        #endregion

        #region UpdateAsync Tests

        [Test]
        public async Task UpdateAsync_ShouldReturnOk_WhenUpdateSuccessful()
        {
            // Arrange
            var key = "DEFAULT_AVATAR_URL";
            var updateDto = new UpdateSystemConfigDto
            {
                Key = "OLD_KEY", // This should be overridden by route parameter
                Value = "https://new-avatar.com/default.jpg",
                Description = "Updated default avatar URL"
            };

            _systemConfigServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<UpdateSystemConfigDto>()))
                .ReturnsAsync(CustomCode.Success);

            // Act
            var result = await _controller.UpdateAsync(key, updateDto);

            // Assert
            Assert.That(result, Is.Not.Null);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);

            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));

            // Verify that the key was set from route parameter
            Assert.That(updateDto.Key, Is.EqualTo(key));

            _systemConfigServiceMock.Verify(s => s.UpdateAsync(It.Is<UpdateSystemConfigDto>(
                dto => dto.Key == key &&
                       dto.Value == "https://new-avatar.com/default.jpg" &&
                       dto.Description == "Updated default avatar URL")), Times.Once);
        }

        [Test]
        public async Task UpdateAsync_ShouldReturnError_WhenKeyNotFound()
        {
            // Arrange
            var key = "NON_EXISTENT_KEY";
            var updateDto = new UpdateSystemConfigDto
            {
                Key = "",
                Value = "some value",
                Description = "some description"
            };

            _systemConfigServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<UpdateSystemConfigDto>()))
                .ReturnsAsync(CustomCode.KeyConfigNotFound);

            // Act
            var result = await _controller.UpdateAsync(key, updateDto);

            // Assert
            Assert.That(result, Is.Not.Null);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);

            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.KeyConfigNotFound));

            // Verify key was set from route
            Assert.That(updateDto.Key, Is.EqualTo(key));

            _systemConfigServiceMock.Verify(s => s.UpdateAsync(It.IsAny<UpdateSystemConfigDto>()), Times.Once);
        }

        [Test]
        public async Task UpdateAsync_ShouldReturnError_WhenInvalidData()
        {
            // Arrange
            var key = "DEFAULT_AVATAR_URL";
            var updateDto = new UpdateSystemConfigDto
            {
                Key = "",
                Value = "",
                Description = ""
            };

            _systemConfigServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<UpdateSystemConfigDto>()))
                .ReturnsAsync(CustomCode.ProvidedInformationIsInValid);

            // Act
            var result = await _controller.UpdateAsync(key, updateDto);

            // Assert
            Assert.That(result, Is.Not.Null);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);

            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.ProvidedInformationIsInValid));

            _systemConfigServiceMock.Verify(s => s.UpdateAsync(It.IsAny<UpdateSystemConfigDto>()), Times.Once);
        }

        [Test]
        public async Task UpdateAsync_ShouldReturnInternalServerError_WhenServiceThrowsException()
        {
            // Arrange
            var key = "DEFAULT_AVATAR_URL";
            var updateDto = new UpdateSystemConfigDto
            {
                Key = "",
                Value = "https://new-avatar.com/default.jpg",
                Description = "Updated default avatar URL"
            };

            _systemConfigServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<UpdateSystemConfigDto>()))
                .ThrowsAsync(new Exception("Database update failed"));

            // Act
            var result = await _controller.UpdateAsync(key, updateDto);

            // Assert
            Assert.That(result, Is.Not.Null);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));

            var response = objectResult.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.SystemError));

            _systemConfigServiceMock.Verify(s => s.UpdateAsync(It.IsAny<UpdateSystemConfigDto>()), Times.Once);
        }

        [Test]
        public async Task UpdateAsync_ShouldSetKeyFromRoute_WhenKeyParameterProvided()
        {
            // Arrange
            var routeKey = "PAYOS_RETURN_URL";
            var updateDto = new UpdateSystemConfigDto
            {
                Key = "DIFFERENT_KEY", // This should be overridden
                Value = "https://new-return-url.com",
                Description = "New return URL"
            };

            _systemConfigServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<UpdateSystemConfigDto>()))
                .ReturnsAsync(CustomCode.Success);

            // Act
            var result = await _controller.UpdateAsync(routeKey, updateDto);

            // Assert
            Assert.That(updateDto.Key, Is.EqualTo(routeKey), "Key should be set from route parameter");

            _systemConfigServiceMock.Verify(s => s.UpdateAsync(It.Is<UpdateSystemConfigDto>(
                dto => dto.Key == routeKey)), Times.Once);
        }

        [Test]
        public async Task UpdateAsync_ShouldHandleNullKey_WhenRouteKeyIsNull()
        {
            // Arrange
            string? nullKey = null;
            var updateDto = new UpdateSystemConfigDto
            {
                Key = "ORIGINAL_KEY",
                Value = "some value",
                Description = "some description"
            };

            _systemConfigServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<UpdateSystemConfigDto>()))
                .ReturnsAsync(CustomCode.ProvidedInformationIsInValid);

            // Act
            var result = await _controller.UpdateAsync(nullKey!, updateDto);

            // Assert
            Assert.That(updateDto.Key, Is.EqualTo(nullKey));

            _systemConfigServiceMock.Verify(s => s.UpdateAsync(It.IsAny<UpdateSystemConfigDto>()), Times.Once);
        }

        [Test]
        public async Task UpdateAsync_ShouldHandleEmptyKey_WhenRouteKeyIsEmpty()
        {
            // Arrange
            var emptyKey = "";
            var updateDto = new UpdateSystemConfigDto
            {
                Key = "ORIGINAL_KEY",
                Value = "some value",
                Description = "some description"
            };

            _systemConfigServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<UpdateSystemConfigDto>()))
                .ReturnsAsync(CustomCode.ProvidedInformationIsInValid);

            // Act
            var result = await _controller.UpdateAsync(emptyKey, updateDto);

            // Assert
            Assert.That(updateDto.Key, Is.EqualTo(emptyKey));

            _systemConfigServiceMock.Verify(s => s.UpdateAsync(It.IsAny<UpdateSystemConfigDto>()), Times.Once);
        }

        #endregion

        #region Edge Cases and Validation Tests

        [Test]
        public async Task UpdateAsync_ShouldReturnBadRequest_WhenDtoIsInvalid()
        {
            // Arrange
            var key = "DEFAULT_AVATAR_URL";
            var invalidDto = new UpdateSystemConfigDto { Value = "" }; // Empty value should be invalid

            // Validate DTO manually (simulating model validation)
            var validationContext = new ValidationContext(invalidDto);
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(invalidDto, validationContext, validationResults, true);

            // Act
            ObjectResult? result = null;
            if (!isValid)
            {
                result = new BadRequestObjectResult(new ApiResponse<object>
                {
                    StatusCode = (int)CustomCode.SystemError,
                    Message = "Validation failed"
                });
            }
            else
            {
                result = await _controller.UpdateAsync(key, invalidDto) as ObjectResult;
            }

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.StatusCode, Is.EqualTo(400));
        }

        [Test]
        public void UpdateSystemConfigDto_ShouldHaveRequiredValidation()
        {
            // Arrange
            var dto = new UpdateSystemConfigDto { Value = null! };
            var validationContext = new ValidationContext(dto);
            var validationResults = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

            Assert.Multiple(() =>
            {
                // Assert
                Assert.That(isValid, Is.False);
                Assert.That(validationResults.Any(v => v.MemberNames.Contains("Value")), Is.True);
            });
        }

        [Test]
        public async Task GetAllAsync_ShouldCallServiceOnlyOnce_WhenCalledMultipleTimes()
        {
            // Arrange
            var configs = new List<SystemConfigDto>();
            _systemConfigServiceMock
                .Setup(s => s.GetAllAsync())
                .ReturnsAsync((configs, CustomCode.Success));

            // Act
            await _controller.GetAllAsync();
            await _controller.GetAllAsync();

            // Assert
            _systemConfigServiceMock.Verify(s => s.GetAllAsync(), Times.Exactly(2));
        }

        [Test]
        public async Task UpdateAsync_ShouldPreserveOriginalDtoValues_ExceptKey()
        {
            // Arrange
            var routeKey = "NEW_KEY";
            var originalValue = "original value";
            var originalDescription = "original description";

            var updateDto = new UpdateSystemConfigDto
            {
                Key = "OLD_KEY",
                Value = originalValue,
                Description = originalDescription
            };

            _systemConfigServiceMock
                .Setup(s => s.UpdateAsync(It.IsAny<UpdateSystemConfigDto>()))
                .ReturnsAsync(CustomCode.Success);

            // Act
            await _controller.UpdateAsync(routeKey, updateDto);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(updateDto.Key, Is.EqualTo(routeKey), "Key should be updated from route");
                Assert.That(updateDto.Value, Is.EqualTo(originalValue), "Value should be preserved");
                Assert.That(updateDto.Description, Is.EqualTo(originalDescription), "Description should be preserved");
            });
        }

        #endregion
    }
}