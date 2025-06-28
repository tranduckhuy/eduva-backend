using Eduva.API.Controllers.Folders;
using Eduva.API.Models;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Folders.Commands;
using Eduva.Application.Features.Folders.Queries;
using Eduva.Application.Features.Folders.Responses;
using Eduva.Application.Features.Folders.Specifications;
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

        #endregion

        #region CreateFolder Tests

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

        #endregion

        #region GetFoldersByClassId Tests

        [Test]
        public async Task GetFoldersByClassId_ShouldReturnUserIdNotFound_WhenUserIdIsNull()
        {
            SetupUser(null);
            var classId = Guid.NewGuid();
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10 };
            var result = await _controller.GetFoldersByClassId(classId, param);
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
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10 };
            var result = await _controller.GetFoldersByClassId(classId, param);
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
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10 };
            var pagination = new Pagination<FolderResponse>(1, 10, 1, new List<FolderResponse> { new FolderResponse { Name = "Test Folder" } });
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetFoldersQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(pagination);
            var result = await _controller.GetFoldersByClassId(classId, param);
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
        public async Task GetFoldersByClassId_ShouldReturnInternalServerError_WhenExceptionThrown()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var classId = Guid.NewGuid();
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10 };
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetFoldersQuery>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Unhandled exception"));
            var result = await _controller.GetFoldersByClassId(classId, param);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        #endregion

        #region GetUserFolders Tests

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

        [Test]
        public async Task GetUserFolders_ShouldReturnInternalServerError_WhenExceptionThrown()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());
            var param = new FolderSpecParam { PageIndex = 1, PageSize = 10 };
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetFoldersQuery>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Unhandled exception"));
            var result = await _controller.GetUserFolders(param);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
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

        #endregion
    }
}
