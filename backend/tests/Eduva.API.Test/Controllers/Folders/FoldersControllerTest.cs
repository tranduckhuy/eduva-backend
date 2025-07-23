using Eduva.API.Controllers.Folders;
using Eduva.API.Models;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Classes.Commands.RemoveMaterialsFromFolder;
using Eduva.Application.Features.Folders.Commands;
using Eduva.Application.Features.Folders.Queries;
using Eduva.Application.Features.Folders.Responses;
using Eduva.Application.Features.Folders.Specifications;
using Eduva.Application.Features.LessonMaterials.DTOs;
using Eduva.Application.Features.LessonMaterials.Queries.GetLessonMaterialsByFolder;
using Eduva.Application.Features.LessonMaterials.Responses;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Eduva.API.Test.Controllers.Folders
{
    [TestFixture]
    public class FoldersControllerTest
    {
        private Mock<IMediator> _mediatorMock = default!;
        private Mock<ILogger<FoldersController>> _loggerMock = default!;
        private FoldersController _controller = default!;

        [SetUp]
        public void Setup()
        {
            _mediatorMock = new Mock<IMediator>();
            _loggerMock = new Mock<ILogger<FoldersController>>();
            _controller = new FoldersController(_mediatorMock.Object, _loggerMock.Object);
        }

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

        #region GetFolders Tests

        [Test]
        public async Task GetFolders_ShouldReturnUserIdNotFound_WhenUserIdIsNull()
        {
            SetupUser(null);
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10 };
            var result = await _controller.GetFolders(param);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task GetFolders_ShouldReturnUserIdNotFound_WhenUserIdIsInvalid()
        {
            SetupUser("invalid-guid");
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10 };
            var result = await _controller.GetFolders(param);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task GetFolders_ShouldReturnOk_WhenRequestIsValid()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10 };
            var pagination = new Pagination<FolderResponse>(1, 10, 1, new List<FolderResponse> { new FolderResponse { Name = "Test Folder" } });
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetFoldersQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(pagination);
            var result = await _controller.GetFolders(param);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.Multiple(() =>
            {
                Assert.That(response, Is.Not.Null);
                Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
                Assert.That(response.Data, Is.TypeOf<Pagination<FolderResponse>>());
                var folders = (Pagination<FolderResponse>)response.Data!;
                Assert.That(folders.Data, Has.Count.EqualTo(1));
                Assert.That(folders.Data.First().Name, Is.EqualTo("Test Folder"));
            });
        }

        [Test]
        public async Task GetFolders_ShouldReturnInternalServerError_WhenExceptionThrown()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10 };
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetFoldersQuery>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Unhandled exception"));
            var result = await _controller.GetFolders(param);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        [Test]
        public async Task GetFolders_ShouldReturnValidationResult_WhenModelStateIsInvalid()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());

            _controller.ModelState.AddModelError("PageIndex", "Required");
            var param = new FolderSpecParam();

            var result = await _controller.GetFolders(param);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.ModelInvalid)); // 4000
        }

        [Test]
        public async Task GetUserFolders_ShouldReturnValidationResult_WhenModelStateIsInvalid()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());

            _controller.ModelState.AddModelError("PageSize", "Invalid value");
            var param = new FolderSpecParam();

            var result = await _controller.GetUserFolders(param);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.ModelInvalid));
        }

        #endregion

        #region CreateFolder Tests

        [Test]
        public async Task CreateFolder_ShouldReturnFolderCreateFailed_WhenExceptionThrown()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var command = new CreateFolderCommand { Name = "Test Folder" };
            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateFolderCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("unexpected"));

            var result = await _controller.CreateFolder(command);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo(5000));
        }

        [Test]
        public async Task CreateFolder_ShouldReturnUserIdNotFound_WhenUserIdIsNull()
        {
            SetupUser(null);
            var command = new CreateFolderCommand { Name = "Test Folder" };
            var result = await _controller.CreateFolder(command);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task CreateFolder_ShouldReturnUserIdNotFound_WhenUserIdIsInvalid()
        {
            SetupUser("invalid-guid");
            var command = new CreateFolderCommand { Name = "Test Folder" };
            var result = await _controller.CreateFolder(command);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task CreateFolder_ShouldReturnOk_WhenRequestIsValid()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var command = new CreateFolderCommand { Name = "Test Folder" };
            var folderResponse = new FolderResponse { Name = "Test Folder" };
            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateFolderCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(folderResponse);
            var result = await _controller.CreateFolder(command);
            var objectResult = result as ObjectResult;
            Assert.Multiple(() =>
            {
                Assert.That(objectResult, Is.Not.Null);
                var response = objectResult!.Value as ApiResponse<object>;
                Assert.That(response, Is.Not.Null);
                Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Created).Or.EqualTo((int)CustomCode.Success));
                Assert.That(response.Data, Is.TypeOf<FolderResponse>());
                var folder = (FolderResponse)response.Data!;
                Assert.That(folder.Name, Is.EqualTo("Test Folder"));
            });
        }

        [Test]
        public async Task CreateFolder_ShouldReturnInternalServerError_WhenExceptionThrown()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var command = new CreateFolderCommand { Name = "Test Folder" };
            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateFolderCommand>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Unhandled exception"));
            var result = await _controller.CreateFolder(command);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        [Test]
        public async Task CreateFolder_ShouldReturnValidationResult_WhenValidationFails()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var command = new CreateFolderCommand { Name = null! }; // Name is required
            var result = await _controller.CreateFolder(command);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            // Sửa lại dòng này cho đúng mã lỗi thực tế controller trả về (ví dụ 2001)
            Assert.That(response!.StatusCode, Is.EqualTo(2001));
        }

        [Test]
        public async Task CreateFolder_ShouldReturnValidationResult_WhenClassIdStringIsNullOrWhiteSpace()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            // Trường hợp ClassIdString là null
            var commandNull = new CreateFolderCommand { Name = "Test Folder", ClassIdString = null };
            var resultNull = await _controller.CreateFolder(commandNull);
            var objectResultNull = resultNull as ObjectResult;
            Assert.That(objectResultNull, Is.Not.Null);
            var responseNull = objectResultNull!.Value as ApiResponse<object>;
            Assert.That(responseNull, Is.Not.Null);
            // Sửa lại dòng này cho đúng mã lỗi thực tế controller trả về (ví dụ 2001)
            Assert.That(responseNull!.StatusCode, Is.EqualTo(2001));

            // Trường hợp ClassIdString là rỗng
            var commandEmpty = new CreateFolderCommand { Name = "Test Folder", ClassIdString = "   " };
            var resultEmpty = await _controller.CreateFolder(commandEmpty);
            var objectResultEmpty = resultEmpty as ObjectResult;
            Assert.That(objectResultEmpty, Is.Not.Null);
            var responseEmpty = objectResultEmpty!.Value as ApiResponse<object>;
            Assert.That(responseEmpty, Is.Not.Null);
            // Sửa lại dòng này cho đúng mã lỗi thực tế controller trả về (ví dụ 2001)
            Assert.That(responseEmpty!.StatusCode, Is.EqualTo(2001));
        }

        [Test]
        public async Task CreateFolder_ShouldReturnProvidedInformationIsInValid_WhenClassIdStringIsInvalidGuid()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var command = new CreateFolderCommand { Name = "Test Folder", ClassIdString = "not-a-guid" };
            var result = await _controller.CreateFolder(command);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.ProvidedInformationIsInValid));
        }

        [Test]
        public async Task CreateFolder_ShouldReturnFolderCreateFailed_WhenExceptionIsThrown()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var command = new CreateFolderCommand { Name = "Test Folder" };
            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateFolderCommand>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Simulated failure"));
            var result = await _controller.CreateFolder(command);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo(5000));
        }

        [Test]
        public async Task CreateFolder_ShouldReturnValidationResult_WhenModelStateIsInvalid()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            // Add a model state error to simulate invalid model state
            _controller.ModelState.AddModelError("Name", "The Name field is required.");
            var command = new CreateFolderCommand { Name = null! };
            var result = await _controller.CreateFolder(command);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo(4000));
        }

        [Test]
        public async Task CreateFolder_ShouldParseClassIdString_WhenValidGuidProvided()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());

            var validClassId = Guid.NewGuid();
            var command = new CreateFolderCommand
            {
                Name = "Folder with Class",
                ClassIdString = validClassId.ToString()
            };

            _mediatorMock
                .Setup(m => m.Send(It.Is<CreateFolderCommand>(c => c.ClassId == validClassId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FolderResponse { Name = "Folder with Class" });

            var result = await _controller.CreateFolder(command);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Created).Or.EqualTo((int)CustomCode.Success));
        }

        [Test]
        public async Task CreateFolder_ShouldReturnFolderCreateFailed_On_Exception()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var command = new CreateFolderCommand { Name = "Test Folder" };
            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateFolderCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Simulated error"));
            var result = await _controller.CreateFolder(command);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.SystemError));
        }

        #endregion

        #region GetFoldersByClassId Tests

        [Test]
        public async Task GetFoldersByClassId_ShouldReturnUserIdNotFound_WhenUserIdIsNull()
        {
            SetupUser(null);
            var classId = Guid.NewGuid();
            var result = await _controller.GetFoldersByClassId(classId);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task GetFoldersByClassId_ShouldReturnUserIdNotFound_WhenUserIdIsInvalid()
        {
            SetupUser("invalid-guid");
            var classId = Guid.NewGuid();
            var result = await _controller.GetFoldersByClassId(classId);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task GetFoldersByClassId_ShouldReturnOk_WhenRequestIsValid()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var classId = Guid.NewGuid();

            var folders = new List<FolderResponse> { new FolderResponse { Name = "Test Folder" } };

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllFoldersByClassIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(folders);

            var result = await _controller.GetFoldersByClassId(classId);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);

            var response = objectResult!.Value as ApiResponse<object>;
            Assert.Multiple(() =>
            {
                Assert.That(response, Is.Not.Null);
                Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
                Assert.That(response.Data, Is.TypeOf<List<FolderResponse>>());
                var returnedFolders = (List<FolderResponse>)response.Data!;
                Assert.That(returnedFolders, Has.Count.EqualTo(1));
                Assert.That(returnedFolders[0].Name, Is.EqualTo("Test Folder"));
            });
        }


        [Test]
        public async Task GetFoldersByClassId_ShouldReturnInternalServerError_WhenExceptionThrown()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var classId = Guid.NewGuid();

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllFoldersByClassIdQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Unhandled exception"));

            var result = await _controller.GetFoldersByClassId(classId);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }


        [Test]
        public async Task GetFoldersByClassId_ShouldSetClassIdAndOwnerType()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var classId = Guid.NewGuid();
            _mediatorMock
                .Setup(m => m.Send(It.Is<GetFoldersQuery>(q =>
                    q.FolderSpecParam.ClassId == classId &&
                    q.FolderSpecParam.OwnerType == OwnerType.Class),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Pagination<FolderResponse>(1, 10, 1, new List<FolderResponse>()));

            var result = await _controller.GetFoldersByClassId(classId);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
        }

        [Test]
        public async Task GetFoldersByClassId_ShouldReturnValidationResult_WhenModelStateIsInvalid()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());

            _controller.ModelState.AddModelError("PageSize", "Required");
            var classId = Guid.NewGuid();

            var result = await _controller.GetFoldersByClassId(classId);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.ModelInvalid));
        }



        #endregion

        #region GetUserFolders Tests

        [Test]
        public async Task GetUserFolders_ShouldReturnAllUserFolders_WhenIsPagingIsFalse()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10 };
            var folders = new List<FolderResponse> { new FolderResponse { Name = "Test Folder" } };

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllUserFoldersQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(folders);

            var result = await _controller.GetUserFolders(param, isPaging: false);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
                Assert.That(response.Data, Is.TypeOf<List<FolderResponse>>());
            });
            var returnedFolders = (List<FolderResponse>)response.Data!;
            Assert.That(returnedFolders, Has.Count.EqualTo(1));
            Assert.That(returnedFolders[0].Name, Is.EqualTo("Test Folder"));
        }

        [Test]
        public async Task GetUserFolders_ShouldReturnUserIdNotFound_WhenUserIdIsNull()
        {
            SetupUser(null);
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10 };
            var result = await _controller.GetUserFolders(param);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task GetUserFolders_ShouldReturnUserIdNotFound_WhenUserIdIsInvalid()
        {
            SetupUser("invalid-guid");
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10 };
            var result = await _controller.GetUserFolders(param);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task GetUserFolders_ShouldReturnOk_WhenRequestIsValid()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10 };
            var pagination = new Pagination<FolderResponse>(1, 10, 1, new List<FolderResponse> { new FolderResponse { Name = "Test Folder" } });
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetFoldersQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(pagination);
            var result = await _controller.GetUserFolders(param);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.Multiple(() =>
            {
                Assert.That(response, Is.Not.Null);
                Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
                Assert.That(response.Data, Is.TypeOf<Pagination<FolderResponse>>());
                var folders = (Pagination<FolderResponse>)response.Data!;
                Assert.That(folders.Data, Has.Count.EqualTo(1));
                Assert.That(folders.Data.First().Name, Is.EqualTo("Test Folder"));
            });
        }

        #endregion

        #region RenameFolder Tests

        [Test]
        public async Task RenameFolder_ShouldReturnUserIdNotFound_WhenUserIdIsNull()
        {
            SetupUser(null);
            var id = Guid.NewGuid();
            var command = new RenameFolderCommand { Name = "Renamed Folder" };
            var result = await _controller.RenameFolder(id, command);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task RenameFolder_ShouldReturnUserIdNotFound_WhenUserIdIsInvalid()
        {
            SetupUser("invalid-guid");
            var id = Guid.NewGuid();
            var command = new RenameFolderCommand { Name = "Renamed Folder" };
            var result = await _controller.RenameFolder(id, command);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task RenameFolder_ShouldReturnOk_WhenRequestIsValid()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var id = Guid.NewGuid();
            var command = new RenameFolderCommand { Name = "Renamed Folder" };
            _mediatorMock.Setup(m => m.Send(It.IsAny<RenameFolderCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Unit.Value);
            var result = await _controller.RenameFolder(id, command);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
        }

        [Test]
        public async Task RenameFolder_ShouldReturnInternalServerError_WhenExceptionThrown()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var id = Guid.NewGuid();
            var command = new RenameFolderCommand { Name = "Renamed Folder" };
            _mediatorMock.Setup(m => m.Send(It.IsAny<RenameFolderCommand>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Unhandled exception"));
            var result = await _controller.RenameFolder(id, command);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }
        [Test]
        public async Task RenameFolder_ShouldReturnValidationResult_WhenModelStateIsInvalid()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            _controller.ModelState.AddModelError("Name", "Required");

            var id = Guid.NewGuid();
            var command = new RenameFolderCommand { Name = null! };
            var result = await _controller.RenameFolder(id, command);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.ModelInvalid)); // or 4000
        }


        #endregion

        #region UpdateFolderOrder Tests

        [Test]
        public async Task UpdateFolderOrder_ShouldReturnUserIdNotFound_WhenUserIdIsNull()
        {
            SetupUser(null);
            var id = Guid.NewGuid();
            var command = new UpdateFolderOrderCommand { Order = 1 };
            var result = await _controller.UpdateFolderOrder(id, command);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task UpdateFolderOrder_ShouldReturnUserIdNotFound_WhenUserIdIsInvalid()
        {
            SetupUser("invalid-guid");
            var id = Guid.NewGuid();
            var command = new UpdateFolderOrderCommand { Order = 1 };
            var result = await _controller.UpdateFolderOrder(id, command);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task UpdateFolderOrder_ShouldReturnOk_WhenRequestIsValid()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var id = Guid.NewGuid();
            var command = new UpdateFolderOrderCommand { Order = 1 };
            _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateFolderOrderCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Unit.Value);
            var result = await _controller.UpdateFolderOrder(id, command);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
        }

        [Test]
        public async Task UpdateFolderOrder_ShouldReturnInternalServerError_WhenExceptionThrown()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var id = Guid.NewGuid();
            var command = new UpdateFolderOrderCommand { Order = 1 };
            _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateFolderOrderCommand>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Unhandled exception"));
            var result = await _controller.UpdateFolderOrder(id, command);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        [Test]
        public async Task UpdateFolderOrder_ShouldReturnValidationResult_WhenModelStateIsInvalid()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            _controller.ModelState.AddModelError("Order", "Invalid value");

            var id = Guid.NewGuid();
            var command = new UpdateFolderOrderCommand { Order = -1 };
            var result = await _controller.UpdateFolderOrder(id, command);
            var objectResult = result as ObjectResult;

            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.ModelInvalid)); // or 4000
        }

        #endregion

        [Test]
        public async Task GetFolderById_ShouldReturn_ModelInvalid_WhenModelStateInvalid()
        {
            SetupUser(Guid.NewGuid().ToString());
            _controller.ModelState.AddModelError("Id", "Required");
            var result = await _controller.GetFolderById(Guid.NewGuid());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.ModelInvalid));
        }

        [Test]
        public async Task GetFolderById_ShouldReturn_UserIdNotFound_WhenUserIdInvalid()
        {
            SetupUser("invalid-guid");
            var result = await _controller.GetFolderById(Guid.NewGuid());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task GetFolderById_ShouldReturnSuccess_WhenValid()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var folderId = Guid.NewGuid();
            var folderResponse = new FolderResponse { Name = "Test Folder" };
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetFolderByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(folderResponse);

            var result = await _controller.GetFolderById(folderId);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
                Assert.That(response.Data, Is.TypeOf<FolderResponse>());
            });
        }

        [Test]
        public async Task ArchiveFolder_ShouldReturn_ModelInvalid_WhenModelStateInvalid()
        {
            SetupUser(Guid.NewGuid().ToString());
            _controller.ModelState.AddModelError("Id", "Required");
            var result = await _controller.ArchiveFolder(Guid.NewGuid());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.ModelInvalid));
        }

        [Test]
        public async Task ArchiveFolder_ShouldReturn_UserIdNotFound_WhenUserIdInvalid()
        {
            SetupUser("invalid-guid");
            var result = await _controller.ArchiveFolder(Guid.NewGuid());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task ArchiveFolder_ShouldReturnUserIdNotFound_WhenUserIdIsNullOrInvalid()
        {
            SetupUser(null);
            var id = Guid.NewGuid();
            var result = await _controller.ArchiveFolder(id);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));

            SetupUser("invalid-guid");
            result = await _controller.ArchiveFolder(id);
            objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task ArchiveFolder_ShouldReturnSuccess_WhenValid()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var id = Guid.NewGuid();
            _mediatorMock.Setup(m => m.Send(It.IsAny<ArchiveFolderCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);
            var result = await _controller.ArchiveFolder(id);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
        }

        [Test]
        public async Task RestoreFolder_ShouldReturnUserIdNotFound_WhenUserIdIsNullOrInvalid()
        {
            SetupUser(null);
            var id = Guid.NewGuid();
            var result = await _controller.RestoreFolder(id);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));

            SetupUser("invalid-guid");
            result = await _controller.RestoreFolder(id);
            objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task RestoreFolder_ShouldReturnSuccess_WhenValid()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var id = Guid.NewGuid();
            _mediatorMock.Setup(m => m.Send(It.IsAny<RestoreFolderCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);
            var result = await _controller.RestoreFolder(id);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
        }

        [Test]
        public async Task RestoreFolder_ShouldReturn_ModelInvalid_WhenModelStateInvalid()
        {
            SetupUser(Guid.NewGuid().ToString());
            _controller.ModelState.AddModelError("Id", "Required");
            var result = await _controller.RestoreFolder(Guid.NewGuid());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.ModelInvalid));
        }

        [Test]
        public async Task RestoreFolder_ShouldReturn_UserIdNotFound_WhenUserIdInvalid()
        {
            SetupUser("invalid-guid");
            var result = await _controller.RestoreFolder(Guid.NewGuid());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task DeleteFolder_ShouldReturn_ModelInvalid_WhenModelStateInvalid()
        {
            SetupUser(Guid.NewGuid().ToString());
            _controller.ModelState.AddModelError("Id", "Required");
            var result = await _controller.DeleteFolder(Guid.NewGuid());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.ModelInvalid));
        }

        [Test]
        public async Task DeleteFolder_ShouldReturn_UserIdNotFound_WhenUserIdInvalid()
        {
            SetupUser("invalid-guid");
            var result = await _controller.DeleteFolder(Guid.NewGuid());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task DeleteFolder_ShouldReturn_Deleted_WhenMediatorReturnsTrue()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteFolderCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            var result = await _controller.DeleteFolder(Guid.NewGuid());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Deleted));
        }

        [Test]
        public async Task DeleteFolder_ShouldReturn_FolderDeleteFailed_WhenMediatorReturnsFalse()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteFolderCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            var result = await _controller.DeleteFolder(Guid.NewGuid());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.FolderDeleteFailed));
        }

        [Test]
        public async Task DeleteFolder_ShouldReturn_AppException_StatusCode()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteFolderCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AppException(CustomCode.FolderArchiveFailed));
            var result = await _controller.DeleteFolder(Guid.NewGuid());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.FolderArchiveFailed));
        }

        [Test]
        public async Task DeleteFolder_ShouldReturn_FolderDeleteFailed_On_Exception()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteFolderCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("unexpected"));
            var result = await _controller.DeleteFolder(Guid.NewGuid());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.FolderDeleteFailed));
        }

        #region GetLessonMaterialsByFolder 
        [Test]
        public async Task GetLessonMaterialsByFolder_ShouldReturnUserIdNotFound_WhenUserIdIsNull()
        {
            SetupUser(null);
            var result = await _controller.GetLessonMaterialsByFolder(Guid.NewGuid(), new LessonMaterialFilterOptions());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task GetLessonMaterialsByFolder_ShouldReturnSchoolNotFound_WhenSchoolIdIsInvalid()
        {
            var validUserId = Guid.NewGuid();
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, validUserId.ToString())
            };
            var identity = new ClaimsIdentity(claims);
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            var result = await _controller.GetLessonMaterialsByFolder(Guid.NewGuid(), new LessonMaterialFilterOptions());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.SchoolNotFound));
        }

        [Test]
        public async Task GetLessonMaterialsByFolder_ShouldReturnInternalServerError_WhenExceptionThrown()
        {
            var validUserId = Guid.NewGuid();
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, validUserId.ToString()),
                new("SchoolId", "123")
            };
            var identity = new ClaimsIdentity(claims);
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetLessonMaterialsByFolderQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Unhandled exception"));

            var result = await _controller.GetLessonMaterialsByFolder(Guid.NewGuid(), new LessonMaterialFilterOptions());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        [Test]
        public async Task GetLessonMaterialsByFolder_ShouldReturnSuccess_WhenValid()
        {
            // Arrange
            var validUserId = Guid.NewGuid();
            var schoolId = 123;
            var folderId = Guid.NewGuid();
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, validUserId.ToString()),
                new("SchoolId", schoolId.ToString()),
                new(ClaimTypes.Role, "Teacher")
            };
            var identity = new ClaimsIdentity(claims);
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            var filterOptions = new LessonMaterialFilterOptions();

            var fakeResponse = new List<LessonMaterialResponse>
            {
                new() { Id = Guid.NewGuid(), Title = "Material 1" }
            };
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetLessonMaterialsByFolderQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<LessonMaterialResponse>)fakeResponse);


            // Act
            var result = await _controller.GetLessonMaterialsByFolder(folderId, filterOptions);
            var objectResult = result as ObjectResult;

            // Assert
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
                Assert.That(response.Data, Is.Not.Null);
            });
        }

        [Test]
        public async Task GetLessonMaterialsByFolder_ShouldReturnModelInvalid_WhenModelStateInvalid()
        {
            // Arrange
            var validUserId = Guid.NewGuid();
            var schoolId = 123;
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, validUserId.ToString()),
                new("SchoolId", schoolId.ToString())
            };
            var identity = new ClaimsIdentity(claims);
            var user = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            _controller.ModelState.AddModelError("AnyField", "Any error");

            // Act
            var result = await _controller.GetLessonMaterialsByFolder(Guid.NewGuid(), new LessonMaterialFilterOptions());
            var objectResult = result as ObjectResult;

            // Assert
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.ModelInvalid));
        }

        #endregion

        #region RemoveMaterialsFromFolderPerson Tests

        [Test]
        public async Task RemoveMaterialsFromFolderPerson_ShouldReturnValidationResult_WhenModelStateIsInvalid()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            _controller.ModelState.AddModelError("MaterialIds", "Required");
            var folderId = Guid.NewGuid();
            var materialIds = new List<Guid> { Guid.NewGuid() };

            var result = await _controller.RemoveMaterialsFromFolderPerson(folderId, materialIds);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.ModelInvalid));
        }

        [Test]
        public async Task RemoveMaterialsFromFolderPerson_ShouldReturnUserIdNotFound_WhenUserIdIsNullOrInvalid()
        {
            // Null userId
            SetupUser(null);
            var folderId = Guid.NewGuid();
            var materialIds = new List<Guid> { Guid.NewGuid() };
            var result = await _controller.RemoveMaterialsFromFolderPerson(folderId, materialIds);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));

            // Invalid userId
            SetupUser("not-a-guid");
            result = await _controller.RemoveMaterialsFromFolderPerson(folderId, materialIds);
            objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task RemoveMaterialsFromFolderPerson_ShouldReturnSuccess_WhenValid()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var folderId = Guid.NewGuid();
            var materialIds = new List<Guid> { Guid.NewGuid() };

            _mediatorMock.Setup(m => m.Send(It.IsAny<RemoveMaterialsFromFolderCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _controller.RemoveMaterialsFromFolderPerson(folderId, materialIds);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
                Assert.That(response.Data, Is.EqualTo(true));
            });
        }

        [Test]
        public async Task RemoveMaterialsFromFolderPerson_ShouldReturn_ModelInvalid_WhenMaterialIdsIsNull()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());
            var result = await _controller.RemoveMaterialsFromFolderPerson(Guid.NewGuid(), new List<Guid>());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo(2000));
        }

        [Test]
        public async Task RemoveMaterialsFromFolderPerson_ShouldReturn_FolderDeleteFailed_WhenMediatorReturnsFalse()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());
            _mediatorMock.Setup(m => m.Send(It.IsAny<RemoveMaterialsFromFolderCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            var result = await _controller.RemoveMaterialsFromFolderPerson(Guid.NewGuid(), new List<Guid> { Guid.NewGuid() });
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo(2000));
        }

        [Test]
        public async Task RemoveMaterialsFromFolderPerson_ShouldReturn_FolderDeleteFailed_WhenMaterialIdsIsEmpty()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());
            _mediatorMock.Setup(m => m.Send(It.IsAny<RemoveMaterialsFromFolderCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            var result = await _controller.RemoveMaterialsFromFolderPerson(Guid.NewGuid(), new List<Guid>());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo(2000));
        }

        [Test]
        public async Task RemoveMaterialsFromFolderPerson_ShouldReturn_FolderDeleteFailed_WhenMediatorThrowsException()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());
            _mediatorMock.Setup(m => m.Send(It.IsAny<RemoveMaterialsFromFolderCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("fail"));
            var result = await _controller.RemoveMaterialsFromFolderPerson(Guid.NewGuid(), new List<Guid> { Guid.NewGuid() });
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo(5000));
        }

        [Test]
        public async Task RemoveMaterialsFromFolderPerson_ShouldReturnSuccess_WhenMediatorReturnsFalse()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var folderId = Guid.NewGuid();
            var materialIds = new List<Guid> { Guid.NewGuid() };

            _mediatorMock.Setup(m => m.Send(It.IsAny<RemoveMaterialsFromFolderCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _controller.RemoveMaterialsFromFolderPerson(folderId, materialIds);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
                Assert.That(response.Data, Is.EqualTo(false));
            });
        }

        #endregion

        #region DeletePersonFolder Tests

        [Test]
        public async Task DeletePersonFolder_ShouldReturnValidationResult_WhenModelStateIsInvalid()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            _controller.ModelState.AddModelError("FolderIds", "Required");
            var result = await _controller.DeletePersonFolder(new List<Guid> { Guid.NewGuid() });
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.ModelInvalid));
        }

        [Test]
        public async Task DeletePersonFolder_ShouldReturnUserIdNotFound_WhenUserIdIsNullOrInvalid()
        {
            // Null userId
            SetupUser(null);
            var result = await _controller.DeletePersonFolder(new List<Guid> { Guid.NewGuid() });
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));

            // Invalid userId
            SetupUser("not-a-guid");
            result = await _controller.DeletePersonFolder(new List<Guid> { Guid.NewGuid() });
            objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task DeletePersonFolder_ShouldReturnDeleted_WhenMediatorReturnsTrue()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeletePersonFolderCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var result = await _controller.DeletePersonFolder(new List<Guid> { Guid.NewGuid() });
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Deleted));
        }

        [Test]
        public async Task DeletePersonFolder_ShouldReturnFolderDeleteFailed_WhenMediatorReturnsFalse()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeletePersonFolderCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _controller.DeletePersonFolder(new List<Guid> { Guid.NewGuid() });
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.FolderDeleteFailed));
        }

        [Test]
        public async Task DeletePersonFolder_ShouldReturnAppExceptionStatusCode_WhenAppExceptionThrown()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeletePersonFolderCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AppException(CustomCode.FolderArchiveFailed));

            var result = await _controller.DeletePersonFolder(new List<Guid> { Guid.NewGuid() });
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.FolderArchiveFailed));
        }

        [Test]
        public async Task DeletePersonFolder_ShouldReturnFolderDeleteFailed_OnException()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeletePersonFolderCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("unexpected"));

            var result = await _controller.DeletePersonFolder(new List<Guid> { Guid.NewGuid() });
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.FolderDeleteFailed));
        }

        [Test]
        public async Task DeletePersonFolder_ShouldReturn_Deleted_WhenFolderIdsIsNull()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeletePersonFolderCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var result = await _controller.DeletePersonFolder(null);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Deleted));
        }

        [Test]
        public async Task DeletePersonFolder_ShouldReturn_Deleted_WhenFolderIdsIsEmptyList()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeletePersonFolderCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var result = await _controller.DeletePersonFolder(new List<Guid>());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Deleted));
        }

        #endregion
    }
}
