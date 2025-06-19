using Eduva.API.Controllers.Schools;
using Eduva.API.Models;
using Eduva.Application.Features.Schools.Commands;
using Eduva.Application.Features.Schools.Reponses;
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
            var expectedResponse = new SchoolResponse
            {
                Id = 1,
                Name = "Test School",
                ContactEmail = "school@eduva.vn",
                ContactPhone = "0909123456"
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<CreateSchoolCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.CreateSchool(command);

            // Assert
            Assert.That(result, Is.Not.Null, "Controller returned null");

            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null, "Result is not an ObjectResult");
            Assert.That(objectResult!.Value, Is.Not.Null, "Returned Value is null");

            var apiResponseJson = Newtonsoft.Json.JsonConvert.SerializeObject(objectResult.Value);
            var apiResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResponse<SchoolResponse>>(apiResponseJson);

            Assert.That(apiResponse!.StatusCode, Is.EqualTo((int)CustomCode.Success), "StatusCode is not Success");
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