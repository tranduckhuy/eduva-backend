using Eduva.API.Controllers.Schools;
using Eduva.API.Models;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Schools.Commands.ActivateSchool;
using Eduva.Application.Features.Schools.Commands.ArchiveSchool;
using Eduva.Application.Features.Schools.Commands.CreateSchool;
using Eduva.Application.Features.Schools.Commands.UpdateSchool;
using Eduva.Application.Features.Schools.Queries;
using Eduva.Application.Features.Schools.Responses;
using Eduva.Application.Features.Schools.Specifications;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Eduva.API.Test.Controllers.School
{
    [TestFixture]
    public class SchoolControllerTests
    {
        private Mock<IMediator> _mediatorMock = default!;
        private Mock<ILogger<SchoolController>> _loggerMock = default!;
        private SchoolController _controller = default!;

        #region SchoolController Setup

        [SetUp]
        public void Setup()
        {
            _mediatorMock = new Mock<IMediator>();
            _loggerMock = new Mock<ILogger<SchoolController>>();
            _controller = new SchoolController(_mediatorMock.Object, _loggerMock.Object);
        }

        #endregion

        #region CreateSchool Tests

        [Test]
        public async Task CreateSchool_ShouldReturnUserIdNotFound_WhenUserIdIsNull()
        {
            SetupUser(null);
            var command = new CreateSchoolCommand { Name = "Test School" };

            var result = await _controller.CreateSchool(command);

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);

            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task CreateSchool_ShouldReturnUserIdNotFound_WhenUserIdIsInvalid()
        {
            SetupUser("invalid-guid");
            var command = new CreateSchoolCommand { Name = "Test School" };

            var result = await _controller.CreateSchool(command);

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);

            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task CreateSchool_ShouldReturnOk_WhenRequestIsValid()
        {
            // Arrange
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());

            var command = new CreateSchoolCommand { Name = "Test School" };

            var mockSchoolResponse = new SchoolResponse
            {
                Id = 1,
                Name = "Test School",
                ContactEmail = "test@school.edu",
                ContactPhone = "0123456789",
                Address = "123 Test Street",
                WebsiteUrl = "https://school.edu.vn",
                Status = EntityStatus.Inactive
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<CreateSchoolCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockSchoolResponse);

            // Act
            var result = await _controller.CreateSchool(command);

            // Assert
            Assert.That(result, Is.Not.Null);

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);

            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
                Assert.That(response.Data, Is.TypeOf<SchoolResponse>());
            });

            var school = (SchoolResponse)response.Data!;
            Assert.That(school.Name, Is.EqualTo("Test School"));
        }

        [Test]
        public async Task CreateSchool_ShouldReturnInternalServerError_WhenExceptionThrown()
        {
            var validUserId = Guid.NewGuid();
            SetupUser(validUserId.ToString());

            var command = new CreateSchoolCommand { Name = "Test School" };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<CreateSchoolCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Unhandled exception"));

            var result = await _controller.CreateSchool(command);

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        #endregion

        #region UpdateSchool Tests

        [Test]
        public async Task UpdateSchool_ShouldReturnOk_WhenCommandIsValid()
        {
            // Arrange
            var id = 1;
            var command = new UpdateSchoolCommand
            {
                Name = "Updated School",
                ContactEmail = "updated@eduva.vn",
                ContactPhone = "0123456789",
                Address = "New Address",
                WebsiteUrl = "https://eduva.vn"
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<UpdateSchoolCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            // Act
            var result = await _controller.UpdateSchool(id, command);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);

            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
        }

        [Test]
        public async Task UpdateSchool_ShouldReturnInternalServerError_WhenExceptionThrown()
        {
            var id = 1;
            var command = new UpdateSchoolCommand { Name = "Fail School" };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<UpdateSchoolCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            var result = await _controller.UpdateSchool(id, command);

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        #endregion

        #region ArchiveSchool Tests

        [Test]
        public async Task ArchiveSchool_ShouldReturnOk_WhenCommandIsHandled()
        {
            var id = 1;

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<ArchiveSchoolCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            var result = await _controller.ArchiveSchool(id);

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);

            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
        }

        [Test]
        public async Task ArchiveSchool_ShouldReturnInternalServerError_WhenExceptionThrown()
        {
            var id = 1;

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<ArchiveSchoolCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Fail"));

            var result = await _controller.ArchiveSchool(id);

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        #endregion

        #region ActivateSchool Tests

        [Test]
        public async Task ActivateSchool_ShouldReturnOk_WhenCommandIsHandled()
        {
            var id = 2;

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<ActivateSchoolCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            var result = await _controller.ActivateSchool(id);

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);

            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
        }

        [Test]
        public async Task ActivateSchool_ShouldReturnInternalServerError_WhenExceptionThrown()
        {
            var id = 2;

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<ActivateSchoolCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Boom"));

            var result = await _controller.ActivateSchool(id);

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        #endregion

        #region GetSchools Tests

        [Test]
        public async Task GetSchools_ShouldReturnOk_WithPaginationData()
        {
            // Arrange
            var param = new SchoolSpecParam { PageIndex = 1, PageSize = 10 };
            var mockPagination = new Pagination<SchoolResponse>
            {
                PageIndex = 1,
                PageSize = 10,
                Count = 1,
                Data = new List<SchoolResponse>
        {
            new() { Id = 1, Name = "Test", Status = EntityStatus.Active }
        }
            };

            _mediatorMock
                .Setup(m => m.Send(It.Is<GetSchoolQuery>(q => q.Param == param), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockPagination);

            // Act
            var result = await _controller.GetSchools(param);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
        }

        #endregion

        #region GetSchoolById Tests

        [Test]
        public async Task GetSchoolById_ShouldReturnOk_WhenIdIsValid()
        {
            var schoolDetail = new SchoolDetailResponse
            {
                Id = 1,
                Name = "Test",
                ContactEmail = "test@email.com",
                Status = EntityStatus.Active
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetSchoolByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(schoolDetail);

            var result = await _controller.GetSchoolById(1);

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
        }

        #endregion

        #region GetCurrentSchool Tests

        [Test]
        public async Task GetCurrentSchool_ShouldReturnUserIdNotFound_WhenUserIdIsInvalid()
        {
            SetupUser("not-a-guid");
            var result = await _controller.GetCurrentSchool();
            var objectResult = result as ObjectResult;
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task GetCurrentSchool_ShouldReturnOk_WhenUserIdIsValid()
        {
            var userId = Guid.NewGuid();
            SetupUser(userId.ToString());

            _mediatorMock
                .Setup(m => m.Send(It.Is<GetMySchoolQuery>(q => q.SchoolAdminId == userId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SchoolResponse { Id = 1, Name = "Test School", Status = EntityStatus.Active });

            var result = await _controller.GetCurrentSchool();

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
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