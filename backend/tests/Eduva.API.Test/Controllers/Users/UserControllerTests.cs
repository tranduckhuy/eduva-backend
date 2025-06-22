using Eduva.API.Controllers.Users;
using Eduva.API.Models;
using Eduva.Application.Features.Users.Commands;
using Eduva.Application.Features.Users.Queries;
using Eduva.Application.Features.Users.Responses;
using Eduva.Infrastructure.Configurations.ExcelTemplate;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;

namespace Eduva.API.Test.Controllers.Users
{
    [TestFixture]
    public class UserControllerTests
    {
        private Mock<IMediator> _mediatorMock = default!;
        private Mock<ILogger<UserController>> _loggerMock = default!;
        private UserController _controller = default!;
        private Mock<IOptions<ImportTemplateConfig>> _importTemplateOptionsMock = default!;
        private Mock<IHttpClientFactory> _httpClientFactoryMock = default!;


        #region UserController Setup

        [SetUp]
        public void Setup()
        {
            _mediatorMock = new Mock<IMediator>();
            _loggerMock = new Mock<ILogger<UserController>>();
            _importTemplateOptionsMock = new Mock<IOptions<ImportTemplateConfig>>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();

            _importTemplateOptionsMock
                .Setup(o => o.Value)
                .Returns(new ImportTemplateConfig { Url = "https://mock-url.com/template.xlsx" });

            _controller = new UserController(
                _loggerMock.Object,
                _importTemplateOptionsMock.Object,
                _httpClientFactoryMock.Object,
                _mediatorMock.Object
            );
        }

        #endregion

        #region GetUserProfileAsync Tests

        [Test]
        public async Task GetUserProfileAsync_ShouldReturnUserIdNotFound_WhenUserIdIsNull()
        {
            SetupUser(null);

            var result = await _controller.GetUserProfileAsync();

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);

            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task GetUserProfileAsync_ShouldReturnUserIdNotFound_WhenUserIdIsInvalid()
        {
            SetupUser("invalid-guid");

            var result = await _controller.GetUserProfileAsync();

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);

            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }
        [Test]
        public async Task GetUserProfileAsync_ShouldReturnOk_WhenRequestIsValid()
        {
            // Arrange
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());

            var expectedUserResponse = new UserResponse
            {
                Id = validUserId,
                FullName = "Test User",
                Email = "test@example.com",
                PhoneNumber = "1234567890",
                AvatarUrl = "https://example.com/avatar.jpg",
                SchoolId = 1,
                Roles = new List<string> { "Student" },
                CreditBalance = 100
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetUserProfileQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedUserResponse);

            // Act
            var result = await _controller.GetUserProfileAsync();

            // Assert
            Assert.That(result, Is.Not.Null, "Controller result is null"); var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null, "Result is not ObjectResult");

            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null, "Response value is null");
            Assert.Multiple(() =>
            {
                Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success), "Status code mismatch");
                Assert.That(response.Data, Is.Not.Null, "Response data is null");
            });

            // Cast the data to UserResponse
            var userData = response.Data as UserResponse;
            Assert.That(userData, Is.Not.Null, "User data is null");
            Assert.That(userData!.Id, Is.EqualTo(expectedUserResponse.Id), "User ID mismatch");

            _mediatorMock.Verify(m => m.Send(
                It.Is<GetUserProfileQuery>(q => q.UserId == validUserId),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetUserProfileAsync_ShouldReturnInternalServerError_WhenExceptionThrown()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetUserProfileQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            var result = await _controller.GetUserProfileAsync();

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        #endregion

        #region GetUserByIdAsync Tests        
        [Test]
        public async Task GetUserByIdAsync_ShouldReturnOk_WhenRequestIsValid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var expectedUserResponse = new UserResponse
            {
                Id = userId,
                FullName = "Test User",
                Email = "test@example.com",
                PhoneNumber = "1234567890",
                AvatarUrl = "https://example.com/avatar.jpg",
                SchoolId = 1,
                Roles = new List<string> { "Student" },
                CreditBalance = 100
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetUserProfileQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedUserResponse);

            // Act
            var result = await _controller.GetUserByIdAsync(userId);

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
            });

            var userData = response.Data as UserResponse;
            Assert.That(userData, Is.Not.Null);
            Assert.That(userData!.Id, Is.EqualTo(expectedUserResponse.Id));

            _mediatorMock.Verify(m => m.Send(
                It.Is<GetUserProfileQuery>(q => q.UserId == userId),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetUserByIdAsync_ShouldReturnInternalServerError_WhenExceptionThrown()
        {
            var userId = Guid.NewGuid();

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetUserProfileQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            var result = await _controller.GetUserByIdAsync(userId);

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        #endregion

        #region UpdateUserProfileAsync Tests

        [Test]
        public async Task UpdateUserProfileAsync_ShouldReturnUserIdNotFound_WhenUserIdIsNull()
        {
            SetupUser(null);
            var command = new UpdateUserProfileCommand
            {
                FullName = "Updated Name",
                PhoneNumber = "9876543210"
            };

            var result = await _controller.UpdateUserProfileAsync(command);

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);

            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task UpdateUserProfileAsync_ShouldReturnUserIdNotFound_WhenUserIdIsInvalid()
        {
            SetupUser("invalid-guid");
            var command = new UpdateUserProfileCommand
            {
                FullName = "Updated Name",
                PhoneNumber = "9876543210"
            };

            var result = await _controller.UpdateUserProfileAsync(command);

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);

            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }
        [Test]
        public async Task UpdateUserProfileAsync_ShouldReturnOk_WhenRequestIsValid()
        {
            // Arrange
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());

            var command = new UpdateUserProfileCommand
            {
                FullName = "Updated Name",
                PhoneNumber = "9876543210",
                AvatarUrl = "https://example.com/new-avatar.jpg"
            };

            var expectedUserResponse = new UserResponse
            {
                Id = validUserId,
                FullName = command.FullName,
                PhoneNumber = command.PhoneNumber,
                AvatarUrl = command.AvatarUrl,
                Email = "test@example.com",
                SchoolId = 1,
                Roles = new List<string> { "Student" },
                CreditBalance = 100
            }; _mediatorMock
                .Setup(m => m.Send(It.IsAny<UpdateUserProfileCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedUserResponse);

            // Act
            var result = await _controller.UpdateUserProfileAsync(command);

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
            });

            var userData = response.Data as UserResponse;
            Assert.That(userData, Is.Not.Null);
            Assert.That(userData!.Id, Is.EqualTo(expectedUserResponse.Id));

            _mediatorMock.Verify(m => m.Send(
                It.Is<UpdateUserProfileCommand>(c =>
                    c.UserId == validUserId &&
                    c.FullName == command.FullName &&
                    c.PhoneNumber == command.PhoneNumber &&
                    c.AvatarUrl == command.AvatarUrl),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task UpdateUserProfileAsync_ShouldReturnInternalServerError_WhenExceptionThrown()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());

            var command = new UpdateUserProfileCommand
            {
                FullName = "Updated Name",
                PhoneNumber = "9876543210"
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<UpdateUserProfileCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            var result = await _controller.UpdateUserProfileAsync(command);

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        #endregion

        #region CreateUserAsync Tests

        [Test]
        public async Task CreateUserAsync_ShouldReturnUserIdNotFound_WhenUserIdIsInvalid()
        {
            SetupUser("invalid-guid");
            var command = new CreateUserByAdminCommand { Email = "test@example.com" };

            var result = await _controller.CreateUserAsync(command);

            var objectResult = result as ObjectResult;
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task CreateUserAsync_ShouldCallMediator_WhenRequestIsValid()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var command = new CreateUserByAdminCommand { Email = "test@example.com" };

            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateUserByAdminCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            var result = await _controller.CreateUserAsync(command);

            var objectResult = result as ObjectResult;
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
        }

        #endregion

        #region Helper Methods

        private void SetupUser(string? userId)
        {
            var claims = new List<Claim>();
            if (userId != null)
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

            var identity = new ClaimsIdentity(claims);
            var user = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        #endregion
    }
}