using Eduva.API.Controllers.LessonMaterials;
using Eduva.API.Models.LessonMaterials;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.LessonMaterials;
using Eduva.Application.Features.LessonMaterials.Commands.ApproveLessonMaterial;
using Eduva.Application.Features.LessonMaterials.Commands.CreateLessonMaterial;
using Eduva.Application.Features.LessonMaterials.Commands.DeleteLessonMaterial;
using Eduva.Application.Features.LessonMaterials.Commands.RestoreLessonMaterial;
using Eduva.Application.Features.LessonMaterials.Commands.UpdateLessonMaterial;
using Eduva.Application.Features.LessonMaterials.Queries.GetAllLessonMaterials;
using Eduva.Application.Features.LessonMaterials.Queries.GetLessonMaterialApprovals;
using Eduva.Application.Features.LessonMaterials.Queries.GetLessonMaterialApprovalsById;
using Eduva.Application.Features.LessonMaterials.Queries.GetLessonMaterialById;
using Eduva.Application.Features.LessonMaterials.Queries.GetOwnLessonMaterials;
using Eduva.Application.Features.LessonMaterials.Queries.GetPendingLessonMaterials;
using Eduva.Application.Features.LessonMaterials.Queries.GetSchoolPublicLessonMaterials;
using Eduva.Application.Features.LessonMaterials.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Eduva.API.Test.Controllers.LessonMaterials
{
    [TestFixture]
    public class LessonMaterialControllerTests
    {
        private Mock<IMediator> _mediatorMock = null!;
        private Mock<ILogger<LessonMaterialController>> _loggerMock = null!;
        private LessonMaterialController _controller = null!;
        private const string ValidUserId = "123e4567-e89b-12d3-a456-426614174000";
        private const string ValidSchoolId = "1";

        [SetUp]
        public void Setup()
        {
            _mediatorMock = new Mock<IMediator>();
            _loggerMock = new Mock<ILogger<LessonMaterialController>>();
            _controller = new LessonMaterialController(_mediatorMock.Object, _loggerMock.Object);
            SetupUserContext(ValidUserId, ValidSchoolId);
        }

        private void SetupUserContext(string? userId, string? schoolId)
        {
            var claims = new List<Claim>();
            if (!string.IsNullOrEmpty(userId))
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            if (!string.IsNullOrEmpty(schoolId))
                claims.Add(new Claim("SchoolId", schoolId));
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Test]
        public async Task GetAllLessonMaterials_ShouldReturnSuccess_WhenValidRequest()
        {
            // Arrange
            var expected = new List<LessonMaterialResponse>();
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllLessonMaterialsQuery>(), default))
                .ReturnsAsync(expected);

            // Act
            var result = await _controller.GetAllLessonMaterials();

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
            _mediatorMock.Verify(m => m.Send(It.IsAny<GetAllLessonMaterialsQuery>(), default), Times.Once);
        }

        [Test]
        public async Task CreateLessonMaterial_ShouldReturnSuccess_WhenValidRequest()
        {
            // Arrange
            var command = new CreateLessonMaterialCommand();
            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateLessonMaterialCommand>(), default))
                .ReturnsAsync(Unit.Value);

            // Act
            var result = await _controller.CreateLessonMaterial(command);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
            _mediatorMock.Verify(m => m.Send(It.IsAny<CreateLessonMaterialCommand>(), default), Times.Once);
        }

        [Test]
        public async Task GetAllLessonMaterials_ShouldReturnUserIdNotFound_WhenUserIdMissing()
        {
            SetupUserContext(null, ValidSchoolId);
            var result = await _controller.GetAllLessonMaterials();
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task CreateLessonMaterial_ShouldReturnUserIdNotFound_WhenUserIdMissing()
        {
            SetupUserContext(null, ValidSchoolId);
            var command = new CreateLessonMaterialCommand();
            var result = await _controller.CreateLessonMaterial(command);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task GetSchoolPublicLessonMaterials_ShouldReturnSuccess_WhenValidRequest()
        {
            var request = new GetSchoolPublicLessonMaterialsRequest();
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetSchoolPublicLessonMaterialsQuery>(), default))
                .ReturnsAsync(new Pagination<LessonMaterialResponse>());
            var result = await _controller.GetSchoolPublicLessonMaterials(request);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task GetSchoolPublicLessonMaterials_ShouldReturnUserIdNotFound_WhenUserIdMissing()
        {
            SetupUserContext(null, ValidSchoolId);
            var request = new GetSchoolPublicLessonMaterialsRequest();
            var result = await _controller.GetSchoolPublicLessonMaterials(request);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task GetSchoolPublicLessonMaterials_ShouldReturnSchoolNotFound_WhenSchoolIdInvalid()
        {
            SetupUserContext(ValidUserId, "0");
            var request = new GetSchoolPublicLessonMaterialsRequest();
            var result = await _controller.GetSchoolPublicLessonMaterials(request);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
        }

        [Test]
        public async Task GetLessonMaterialById_ShouldReturnSuccess_WhenValidRequest()
        {
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetLessonMaterialByIdQuery>(), default))
                .ReturnsAsync(new LessonMaterialResponse());
            var id = Guid.NewGuid();
            var result = await _controller.GetLessonMaterialById(id);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task GetLessonMaterialById_ShouldReturnUserIdNotFound_WhenUserIdMissing()
        {
            SetupUserContext(null, ValidSchoolId);
            var id = Guid.NewGuid();
            var result = await _controller.GetLessonMaterialById(id);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task GetPendingLessonMaterials_ShouldReturnSuccess_WhenValidRequest()
        {
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetPendingLessonMaterialsQuery>(), default))
                .ReturnsAsync(new Pagination<LessonMaterialResponse>());
            var request = new GetPendingLessonMaterialsRequest();
            var result = await _controller.GetPendingLessonMaterials(request);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task GetPendingLessonMaterials_ShouldReturnUserIdNotFound_WhenUserIdMissing()
        {
            SetupUserContext(null, ValidSchoolId);
            var request = new GetPendingLessonMaterialsRequest();
            var result = await _controller.GetPendingLessonMaterials(request);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task GetPendingLessonMaterials_ShouldReturnSchoolNotFound_WhenSchoolIdInvalid()
        {
            SetupUserContext(ValidUserId, "0");
            var request = new GetPendingLessonMaterialsRequest();
            var result = await _controller.GetPendingLessonMaterials(request);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
        }

        [Test]
        public async Task ApproveLessonMaterial_ShouldReturnSuccess_WhenValidRequest()
        {
            _mediatorMock.Setup(m => m.Send(It.IsAny<ApproveLessonMaterialCommand>(), default))
                .ReturnsAsync(Unit.Value);
            var id = Guid.NewGuid();
            var command = new ApproveLessonMaterialCommand();
            var result = await _controller.ApproveLessonMaterial(id, command);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task ApproveLessonMaterial_ShouldReturnUserIdNotFound_WhenUserIdMissing()
        {
            SetupUserContext(null, ValidSchoolId);
            var id = Guid.NewGuid();
            var command = new ApproveLessonMaterialCommand();
            var result = await _controller.ApproveLessonMaterial(id, command);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task RestoreLessonMaterials_ShouldReturnSuccess_WhenValidRequest()
        {
            _mediatorMock.Setup(m => m.Send(It.IsAny<RestoreLessonMaterialCommand>(), default))
                .ReturnsAsync(true);
            var folderId = Guid.NewGuid();
            var ids = new List<Guid> { Guid.NewGuid() };
            var result = await _controller.RestoreLessonMaterials(folderId, ids);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task RestoreLessonMaterials_ShouldReturnUserIdNotFound_WhenUserIdMissing()
        {
            SetupUserContext(null, ValidSchoolId);
            var folderId = Guid.NewGuid();
            var ids = new List<Guid> { Guid.NewGuid() };
            var result = await _controller.RestoreLessonMaterials(folderId, ids);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task RestoreLessonMaterials_ShouldReturnValidationResult_WhenModelStateInvalid()
        {
            _controller.ModelState.AddModelError("key", "error");
            var folderId = Guid.NewGuid();
            var ids = new List<Guid> { Guid.NewGuid() };
            var result = await _controller.RestoreLessonMaterials(folderId, ids);

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        [Test]
        public async Task UpdateLessonMaterial_ShouldReturnSuccess_WhenValidRequest()
        {
            _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateLessonMaterialCommand>(), default))
                .ReturnsAsync(Unit.Value);
            var id = Guid.NewGuid();
            var command = new UpdateLessonMaterialCommand();
            var result = await _controller.UpdateLessonMaterial(id, command);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task UpdateLessonMaterial_ShouldReturnUserIdNotFound_WhenUserIdMissing()
        {
            SetupUserContext(null, ValidSchoolId);
            var id = Guid.NewGuid();
            var command = new UpdateLessonMaterialCommand();
            var result = await _controller.UpdateLessonMaterial(id, command);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task GetOwnLessonMaterialsByStatus_ShouldReturnSuccess_WhenValidRequest()
        {
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetOwnLessonMaterialsQuery>(), default))
                .ReturnsAsync(new Pagination<LessonMaterialResponse>());
            var request = new GetOwnLessonMaterialsRequest();
            var result = await _controller.GetOwnLessonMaterialsByStatus(request);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task GetOwnLessonMaterialsByStatus_ShouldReturnUserIdNotFound_WhenUserIdMissing()
        {
            SetupUserContext(null, ValidSchoolId);
            var request = new GetOwnLessonMaterialsRequest();
            var result = await _controller.GetOwnLessonMaterialsByStatus(request);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task GetOwnLessonMaterialsByStatus_ShouldReturnSchoolNotFound_WhenSchoolIdInvalid()
        {
            SetupUserContext(ValidUserId, "0");
            var request = new GetOwnLessonMaterialsRequest();
            var result = await _controller.GetOwnLessonMaterialsByStatus(request);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
        }

        [Test]
        public async Task DeleteLessonMaterial_ShouldReturnSuccess_WhenValidRequest()
        {
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteLessonMaterialCommand>(), default))
                .ReturnsAsync(Unit.Value);
            var command = new DeleteLessonMaterialCommand { Ids = new List<Guid> { Guid.NewGuid() } };
            var result = await _controller.DeleteLessonMaterial(command);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task DeleteLessonMaterial_ShouldReturnUserIdNotFound_WhenUserIdMissing()
        {
            SetupUserContext(null, ValidSchoolId);
            var command = new DeleteLessonMaterialCommand { Ids = new List<Guid> { Guid.NewGuid() } };
            var result = await _controller.DeleteLessonMaterial(command);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task DeleteLessonMaterial_ShouldReturnSchoolNotFound_WhenSchoolIdInvalid()
        {
            SetupUserContext(ValidUserId, "0");
            var command = new DeleteLessonMaterialCommand { Ids = new List<Guid> { Guid.NewGuid() } };
            var result = await _controller.DeleteLessonMaterial(command);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
        }

        [Test]
        public async Task GetLessonMaterialApprovals_ShouldReturnSuccess_WhenValidRequest()
        {
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetLessonMaterialApprovalsQuery>(), default))
                .ReturnsAsync(new Pagination<LessonMaterialApprovalResponse>());
            var request = new GetLessonMaterialApprovalsRequest();
            var result = await _controller.GetLessonMaterialApprovals(request);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task GetLessonMaterialApprovals_ShouldReturnUserIdNotFound_WhenUserIdMissing()
        {
            SetupUserContext(null, ValidSchoolId);
            var request = new GetLessonMaterialApprovalsRequest();
            var result = await _controller.GetLessonMaterialApprovals(request);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task GetLessonMaterialApprovalsById_ShouldReturnSuccess_WhenValidRequest()
        {
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetLessonMaterialApprovalsByIdQuery>(), default))
                .ReturnsAsync(new List<LessonMaterialApprovalResponse>());
            var id = Guid.NewGuid();
            var result = await _controller.GetLessonMaterialApprovalsById(id);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        }

        [Test]
        public async Task GetLessonMaterialApprovalsById_ShouldReturnUserIdNotFound_WhenUserIdMissing()
        {
            SetupUserContext(null, ValidSchoolId);
            var id = Guid.NewGuid();
            var result = await _controller.GetLessonMaterialApprovalsById(id);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }
    }
}
