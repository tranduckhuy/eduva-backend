using Eduva.API.Controllers.Users;
using Eduva.API.Models;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Schools.Responses;
using Eduva.Application.Features.Users.Commands;
using Eduva.Application.Features.Users.Queries;
using Eduva.Application.Features.Users.Requests;
using Eduva.Application.Features.Users.Responses;
using Eduva.Application.Features.Users.Specifications;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Infrastructure.Configurations.ExcelTemplate;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
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
        private Mock<UserManager<ApplicationUser>> _userManagerMock = default!;


        #region UserController Setup

        [SetUp]
        public void Setup()
        {
            _mediatorMock = new Mock<IMediator>();
            _loggerMock = new Mock<ILogger<UserController>>();
            _importTemplateOptionsMock = new Mock<IOptions<ImportTemplateConfig>>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var storeMock = new Mock<IUserStore<ApplicationUser>>();

            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                storeMock.Object,
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<IPasswordHasher<ApplicationUser>>(),
                new List<IUserValidator<ApplicationUser>>(),
                new List<IPasswordValidator<ApplicationUser>>(),
                Mock.Of<ILookupNormalizer>(),
                new IdentityErrorDescriber(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<ApplicationUser>>>()
            );

            _importTemplateOptionsMock
                .Setup(o => o.Value)
                .Returns(new ImportTemplateConfig { UrlTemplateImportUser = "https://mock-url.com/template.xlsx" });

            _controller = new UserController(
                _loggerMock.Object,
                _importTemplateOptionsMock.Object,
                _httpClientFactoryMock.Object,
                _mediatorMock.Object,
                _userManagerMock.Object
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
                School = new SchoolResponse
                {
                    Id = 1,
                    Name = "Test School",
                    ContactEmail = "contact@test.edu.vn",
                    ContactPhone = "123456789",
                    WebsiteUrl = "https://test.edu.vn"
                },
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
                School = new SchoolResponse
                {
                    Id = 1,
                    Name = "Test School",
                    ContactEmail = "contact@test.edu.vn",
                    ContactPhone = "123456789",
                    WebsiteUrl = "https://test.edu.vn"
                },
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
                FullName = "Test User",
                Email = "test@example.com",
                PhoneNumber = "1234567890",
                AvatarUrl = "https://example.com/avatar.jpg",
                School = new SchoolResponse
                {
                    Id = 1,
                    Name = "Test School",
                    ContactEmail = "contact@test.edu.vn",
                    ContactPhone = "123456789",
                    WebsiteUrl = "https://test.edu.vn"
                },
                Roles = new List<string> { "Student" },
                CreditBalance = 100
            };

            _mediatorMock
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

        #region GetUsersAsync Tests

        [Test]
        public async Task GetUsersAsync_ShouldReturnUserNotPartOfSchool_WhenUserManagerReturnsNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString(), role: nameof(Role.SchoolAdmin));

            // UserManager returns null instead of user
            _userManagerMock
                .Setup(m => m.FindByIdAsync(userId.ToString()))
                .ReturnsAsync((ApplicationUser?)null);

            var param = new UserSpecParam();

            // Act
            var result = await _controller.GetUsersAsync(param);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);

            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserNotPartOfSchool));
        }

        [Test]
        public async Task GetUsersAsync_ShouldSetSchoolId_WhenSchoolAdminHasSchool()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var schoolId = 100;

            SetupUser(userId.ToString(), role: nameof(Role.SchoolAdmin));

            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(new ApplicationUser { Id = userId, SchoolId = schoolId });

            var param = new UserSpecParam();

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetUsersBySpecQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Pagination<UserResponse>());

            // Act
            var result = await _controller.GetUsersAsync(param);

            // Assert
            _mediatorMock.Verify(m => m.Send(
                It.Is<GetUsersBySpecQuery>(q => ((UserSpecParam)q.Param).SchoolId == schoolId),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetUsersAsync_ShouldReturnUserNotPartOfSchool_WhenSchoolAdminHasNoSchool()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new ApplicationUser
            {
                Id = userId,
                SchoolId = null
            };

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Role, nameof(Role.SchoolAdmin))
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var controller = new UserController(_loggerMock.Object, _importTemplateOptionsMock.Object, _httpClientFactoryMock.Object, _mediatorMock.Object, _userManagerMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _userManagerMock
                .Setup(m => m.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);

            var param = new UserSpecParam();

            // Act
            var result = await controller.GetUsersAsync(param);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);

            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserNotPartOfSchool));
        }

        [Test]
        public async Task GetUsersAsync_ShouldReturnInternalServerError_WhenMediatorThrows()
        {
            SetupUser(Guid.NewGuid().ToString(), role: nameof(Role.SystemAdmin));

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetUsersBySpecQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            var result = await _controller.GetUsersAsync(new UserSpecParam());

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        [Test]
        public async Task GetUsersAsync_ShouldCallMediator_WhenSystemAdmin()
        {
            SetupUser(Guid.NewGuid().ToString(), role: nameof(Role.SystemAdmin));

            var param = new UserSpecParam();

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetUsersBySpecQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Pagination<UserResponse> { PageIndex = 1, PageSize = 10, Count = 0, Data = [] });

            var result = await _controller.GetUsersAsync(param);

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);

            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
        }

        [Test]
        public async Task GetUsersAsync_ShouldCallMediator_WhenSchoolAdminHasValidSchoolId()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString(), role: nameof(Role.SchoolAdmin));

            var schoolId = 123;
            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(new ApplicationUser { Id = userId, SchoolId = schoolId });

            var param = new UserSpecParam();

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetUsersBySpecQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Pagination<UserResponse> { PageIndex = 1, PageSize = 10, Count = 1, Data = [] });

            var result = await _controller.GetUsersAsync(param);

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);

            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
                Assert.That(response.Data, Is.InstanceOf<Pagination<UserResponse>>());
            });

            _mediatorMock.Verify(m => m.Send(
                It.Is<GetUsersBySpecQuery>(q => ((UserSpecParam)q.Param).SchoolId == schoolId),
                It.IsAny<CancellationToken>()), Times.Once);
        }


        [Test]
        public async Task GetUsersAsync_ShouldReturnUserIdNotFound_WhenUserIdIsInvalid()
        {
            SetupUser("invalid-guid", role: nameof(Role.SchoolAdmin));

            var result = await _controller.GetUsersAsync(new UserSpecParam());

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);

            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }


        [Test]
        public async Task GetUsersAsync_ShouldReturnUserNotPartOfSchool_WhenSchoolIdIsNull()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString(), role: nameof(Role.SchoolAdmin));

            _userManagerMock.Setup(u => u.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(new ApplicationUser { Id = userId, SchoolId = null });

            var result = await _controller.GetUsersAsync(new UserSpecParam());

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);

            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserNotPartOfSchool));
        }

        #endregion

        #region ImportUsersFromExcel Tests

        [Test]
        public async Task ImportUsersFromExcel_ShouldReturnFile_WhenFileBytesIsNotNull()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());

            var fileBytes = new byte[] { 1, 2, 3 };

            var fileMock = new Mock<IFormFile>();
            var request = new ImportUsersFromExcelRequest { File = fileMock.Object };

            _mediatorMock.Setup(m => m.Send(It.IsAny<ImportUsersFromExcelCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fileBytes);

            var result = await _controller.ImportUsersFromExcel(request);

            var fileResult = result as FileContentResult;
            Assert.That(fileResult, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(fileResult!.FileContents, Is.EqualTo(fileBytes));
                Assert.That(fileResult.ContentType, Is.EqualTo("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
                Assert.That(fileResult.FileDownloadName, Does.Contain("user_import_error_"));
            });
        }

        [Test]
        public async Task ImportUsersFromExcel_ShouldReturnEmptyFile_WhenFileBytesIsNull()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());

            var fileMock = new Mock<IFormFile>();
            var request = new ImportUsersFromExcelRequest { File = fileMock.Object };

            _mediatorMock.Setup(m => m.Send(It.IsAny<ImportUsersFromExcelCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[]?)null);

            var result = await _controller.ImportUsersFromExcel(request);

            var fileResult = result as FileContentResult;
            Assert.That(fileResult, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(fileResult!.FileContents, Is.Empty);
                Assert.That(fileResult.ContentType, Is.EqualTo("application/octet-stream"));
                Assert.That(fileResult.FileDownloadName, Is.EqualTo("Empty.xlsx"));
            });
        }

        [Test]
        public async Task ImportUsersFromExcel_ShouldReturnAppExceptionStatusCode_WhenThrown()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());

            var fileMock = new Mock<IFormFile>();
            var request = new ImportUsersFromExcelRequest
            {
                File = fileMock.Object
            };

            _mediatorMock.Setup(m => m.Send(It.IsAny<ImportUsersFromExcelCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AppException(CustomCode.InvalidFileType));

            var result = await _controller.ImportUsersFromExcel(request);

            var objectResult = result as ObjectResult;
            var response = objectResult!.Value as ApiResponse<object>;

            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.InvalidFileType));
        }

        [Test]
        public async Task ImportUsersFromExcel_ShouldReturnUserIdNotFound_WhenUserIdIsInvalid()
        {
            SetupUser("invalid-guid");

            var request = new ImportUsersFromExcelRequest
            {
                File = Mock.Of<IFormFile>()
            };

            var result = await _controller.ImportUsersFromExcel(request);

            var objectResult = result as ObjectResult;
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        #endregion

        #region LockUserAccount Tests

        [Test]
        public async Task LockUserAccount_ShouldReturnUserIdNotFound_WhenUserIdIsInvalid()
        {
            SetupUser("invalid-guid");
            var result = await _controller.LockUserAccount(Guid.NewGuid());

            var objectResult = result as ObjectResult;
            var response = objectResult!.Value as ApiResponse<object>;

            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task LockUserAccount_ShouldCallMediator_WhenRequestIsValid()
        {
            var executorId = Guid.NewGuid();
            SetupUser(executorId.ToString());

            var targetUserId = Guid.NewGuid();
            _mediatorMock.Setup(m => m.Send(It.IsAny<LockAccountCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            var result = await _controller.LockUserAccount(targetUserId);

            var objectResult = result as ObjectResult;
            var response = objectResult!.Value as ApiResponse<object>;

            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));

            _mediatorMock.Verify(m => m.Send(
                It.Is<LockAccountCommand>(cmd =>
                    cmd.UserId == targetUserId && cmd.ExecutorId == executorId),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region UnlockUserAccount Tests

        [Test]
        public async Task UnlockUserAccount_ShouldReturnUserIdNotFound_WhenUserIdIsInvalid()
        {
            SetupUser("invalid-guid");
            var result = await _controller.UnlockUserAccount(Guid.NewGuid());

            var objectResult = result as ObjectResult;
            var response = objectResult!.Value as ApiResponse<object>;

            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task UnlockUserAccount_ShouldCallMediator_WhenRequestIsValid()
        {
            var executorId = Guid.NewGuid();
            SetupUser(executorId.ToString());

            var targetUserId = Guid.NewGuid();
            _mediatorMock.Setup(m => m.Send(It.IsAny<UnlockAccountCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            var result = await _controller.UnlockUserAccount(targetUserId);

            var objectResult = result as ObjectResult;
            var response = objectResult!.Value as ApiResponse<object>;

            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));

            _mediatorMock.Verify(m => m.Send(
                It.Is<UnlockAccountCommand>(cmd =>
                    cmd.UserId == targetUserId && cmd.ExecutorId == executorId),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region DownloadImportTemplate Tests

        [Test]
        public async Task DownloadImportTemplate_ShouldReturnFile_WhenDownloadSucceeds()
        {
            var type = ImportTemplateType.User;
            var fileBytes = new byte[] { 11, 22, 33 };

            _importTemplateOptionsMock.Setup(o => o.Value)
                .Returns(new ImportTemplateConfig { UrlTemplateImportUser = "https://mock-url.com/template.xlsx" });

            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new ByteArrayContent(fileBytes)
                });

            var client = new HttpClient(handler.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient("EduvaHttpClient")).Returns(client);

            _controller = new UserController(
                _loggerMock.Object,
                _importTemplateOptionsMock.Object,
                _httpClientFactoryMock.Object,
                _mediatorMock.Object,
                _userManagerMock.Object
            );

            var result = await _controller.DownloadImportTemplate(type);

            var fileResult = result as FileContentResult;
            Assert.That(fileResult, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(fileResult!.FileContents, Is.EqualTo(fileBytes));
                Assert.That(fileResult.ContentType, Is.EqualTo("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
                Assert.That(fileResult.FileDownloadName, Is.EqualTo("user_import_template.xlsx")); // or similar
            });
        }

        [Test]
        public async Task DownloadImportTemplate_ShouldReturnSystemError_WhenExceptionThrown()
        {
            var mockClient = new Mock<HttpMessageHandler>();
            mockClient
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new Exception("Unexpected"));

            var clientFactory = new Mock<IHttpClientFactory>();
            clientFactory.Setup(f => f.CreateClient("EduvaHttpClient"))
                .Returns(new HttpClient(mockClient.Object));

            _httpClientFactoryMock = clientFactory;
            _controller = new UserController(
                _loggerMock.Object,
                _importTemplateOptionsMock.Object,
                _httpClientFactoryMock.Object,
                _mediatorMock.Object,
                _userManagerMock.Object
            );

            var result = await _controller.DownloadImportTemplate(ImportTemplateType.User);

            var objectResult = result as ObjectResult;
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.SystemError));
        }

        [Test]
        public async Task DownloadImportTemplate_ShouldReturnInvalidTemplateType_WhenUrlIsEmpty()
        {
            _importTemplateOptionsMock.Setup(x => x.Value)
                .Returns(new ImportTemplateConfig());

            var result = await _controller.DownloadImportTemplate((ImportTemplateType)999);

            var objectResult = result as ObjectResult;
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.InvalidTemplateType));
        }

        [Test]
        public async Task DownloadImportTemplate_ShouldReturnFileDownloadFailed_WhenHttpFails()
        {
            var mockClient = new Mock<HttpMessageHandler>();
            mockClient
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException());

            var clientFactory = new Mock<IHttpClientFactory>();
            clientFactory.Setup(f => f.CreateClient("EduvaHttpClient"))
                .Returns(new HttpClient(mockClient.Object));

            _httpClientFactoryMock = clientFactory;
            _controller = new UserController(
                _loggerMock.Object,
                _importTemplateOptionsMock.Object,
                _httpClientFactoryMock.Object,
                _mediatorMock.Object,
                _userManagerMock.Object
            );

            var result = await _controller.DownloadImportTemplate(ImportTemplateType.User);

            var objectResult = result as ObjectResult;
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.FileDownloadFailed));
        }

        #endregion

        #region DeleteUser Tests

        [Test]
        public async Task DeleteUser_ShouldReturnUserIdNotFound_WhenExecutorIdIsNull()
        {
            SetupUser(null); // No claim

            var result = await _controller.DeleteUser(Guid.NewGuid());

            var objectResult = result as ObjectResult;
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task DeleteUser_ShouldReturnUserIdNotFound_WhenExecutorIdIsInvalid()
        {
            SetupUser("not-a-guid");

            var result = await _controller.DeleteUser(Guid.NewGuid());

            var objectResult = result as ObjectResult;
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task DeleteUser_ShouldCallMediator_WhenValidRequest()
        {
            var executorId = Guid.NewGuid();
            SetupUser(executorId.ToString());

            var userId = Guid.NewGuid();
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteUserCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            var result = await _controller.DeleteUser(userId);

            var objectResult = result as ObjectResult;
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));

            _mediatorMock.Verify(m => m.Send(
                It.Is<DeleteUserCommand>(c =>
                    c.UserId == userId && c.ExecutorId == executorId),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task DeleteUser_ShouldReturnInternalServerError_WhenExceptionThrown()
        {
            var executorId = Guid.NewGuid();
            SetupUser(executorId.ToString());

            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteUserCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Something went wrong"));

            var result = await _controller.DeleteUser(Guid.NewGuid());

            var objectResult = result as ObjectResult;
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        #endregion

        #region Helper Methods

        private void SetupUser(string? userId, string? role = null)
        {
            var claims = new List<Claim>();
            if (userId != null)
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

            if (role != null)
                claims.Add(new Claim(ClaimTypes.Role, role));

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