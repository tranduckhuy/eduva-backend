using Eduva.API.Controllers.LessonMaterials;
using Eduva.API.Models;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.LessonMaterials.Commands;
using Eduva.Application.Features.LessonMaterials.Queries;
using Eduva.Application.Features.LessonMaterials.Responses;
using Eduva.Application.Features.LessonMaterials.Specifications;
using Eduva.Application.Features.LessonMaterials;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
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
        private Mock<IMediator> _mediatorMock;
        private Mock<ILogger<LessonMaterialController>> _loggerMock;
        private LessonMaterialController _controller;
        private readonly Guid _testUserId = Guid.NewGuid();
        private readonly int _testSchoolId = 1;

        [SetUp]
        public void Setup()
        {
            _mediatorMock = new Mock<IMediator>();
            _loggerMock = new Mock<ILogger<LessonMaterialController>>();
            _controller = new LessonMaterialController(_mediatorMock.Object, _loggerMock.Object);
            
            SetupUserContext();
        }

        private void SetupUserContext(string? userId = null, string? schoolId = null, string role = "Teacher")
        {
            userId ??= _testUserId.ToString();
            schoolId ??= _testSchoolId.ToString();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim("SchoolId", schoolId),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };
        }

        private void SetupUserContextWithoutUser()
        {
            // Create a controller context with no user claims to simulate unauthenticated user
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        #region GetAllLessonMaterials Tests

        [Test]
        public async Task GetAllLessonMaterials_ShouldReturn200_WhenSuccessful()
        {
            // Arrange
            var expectedResponse = new List<LessonMaterialResponse>
            {
                new LessonMaterialResponse { Id = Guid.NewGuid(), Title = "Test Material 1" },
                new LessonMaterialResponse { Id = Guid.NewGuid(), Title = "Test Material 2" }
            };

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllLessonMaterialsQuery>(), default))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetAllLessonMaterials();

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(200));
            _mediatorMock.Verify(m => m.Send(It.IsAny<GetAllLessonMaterialsQuery>(), default), Times.Once);
        }

        [Test]
        public async Task GetAllLessonMaterials_ShouldReturn401_WhenUserIdNotFound()
        {
            // Arrange
            SetupUserContextWithoutUser();

            // Act
            var result = await _controller.GetAllLessonMaterials();

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(401));
        }

        [Test]
        public async Task GetAllLessonMaterials_WithClassId_ShouldPassCorrectParameters()
        {
            // Arrange
            var classId = Guid.NewGuid();
            GetAllLessonMaterialsQuery? capturedQuery = null;

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllLessonMaterialsQuery>(), default))
                .Callback<IRequest<IReadOnlyList<LessonMaterialResponse>>, CancellationToken>((query, _) => 
                {
                    capturedQuery = query as GetAllLessonMaterialsQuery;
                })
                .ReturnsAsync(new List<LessonMaterialResponse>());

            // Act
            await _controller.GetAllLessonMaterials(classId);

            // Assert
            Assert.That(capturedQuery, Is.Not.Null);
            Assert.That(capturedQuery.ClassId, Is.EqualTo(classId));
            Assert.That(capturedQuery.UserId, Is.EqualTo(_testUserId));
        }

        [Test]
        public async Task GetAllLessonMaterials_WithFolderId_ShouldPassCorrectParameters()
        {
            // Arrange
            var folderId = Guid.NewGuid();
            GetAllLessonMaterialsQuery? capturedQuery = null;

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllLessonMaterialsQuery>(), default))
                .Callback<IRequest<IReadOnlyList<LessonMaterialResponse>>, CancellationToken>((query, _) => 
                {
                    capturedQuery = query as GetAllLessonMaterialsQuery;
                })
                .ReturnsAsync(new List<LessonMaterialResponse>());

            // Act
            await _controller.GetAllLessonMaterials(null, folderId);

            // Assert
            Assert.That(capturedQuery, Is.Not.Null);
            Assert.That(capturedQuery.FolderId, Is.EqualTo(folderId));
        }

        [Test]
        public async Task GetAllLessonMaterials_AsStudent_ShouldSetIsStudentFlag()
        {
            // Arrange
            SetupUserContext(role: "Student");
            GetAllLessonMaterialsQuery? capturedQuery = null;

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllLessonMaterialsQuery>(), default))
                .Callback<IRequest<IReadOnlyList<LessonMaterialResponse>>, CancellationToken>((query, _) => 
                {
                    capturedQuery = query as GetAllLessonMaterialsQuery;
                })
                .ReturnsAsync(new List<LessonMaterialResponse>());

            // Act
            await _controller.GetAllLessonMaterials();

            // Assert
            Assert.That(capturedQuery, Is.Not.Null);
            Assert.That(capturedQuery.IsStudent, Is.True);
        }

        [Test]
        public async Task GetAllLessonMaterials_ShouldReturn500_WhenMediatorThrowsException()
        {
            // Arrange
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllLessonMaterialsQuery>(), default))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetAllLessonMaterials();

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(500));
        }

        #endregion

        #region CreateLessonMaterial Tests

        [Test]
        public async Task CreateLessonMaterial_ShouldReturn200_WhenSuccessful()
        {
            // Arrange
            var command = new CreateLessonMaterialCommand
            {
                FolderId = Guid.NewGuid(),
                BlobNames = new List<string> { "test.pdf" },
                LessonMaterials = new List<LessonMaterialRequest>
                {
                    new LessonMaterialRequest
                    {
                        Title = "Test Material",
                        ContentType = ContentType.PDF,
                        FileSize = 1024,
                        SourceUrl = "test.pdf"
                    }
                }
            };

            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateLessonMaterialCommand>(), default))
                .Returns(Task.FromResult(Unit.Value));

            // Act
            var result = await _controller.CreateLessonMaterial(command);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(200));
            Assert.That(command.CreatedBy, Is.EqualTo(_testUserId));
            Assert.That(command.SchoolId, Is.EqualTo(_testSchoolId));
        }

        [Test]
        public async Task CreateLessonMaterial_ShouldReturn401_WhenUserIdNotFound()
        {
            // Arrange
            SetupUserContextWithoutUser();
            var command = new CreateLessonMaterialCommand();

            // Act
            var result = await _controller.CreateLessonMaterial(command);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(401));
        }

        [Test]
        public async Task CreateLessonMaterial_WithoutSchoolId_ShouldSetSchoolIdToNull()
        {
            // Arrange
            SetupUserContext(schoolId: "0");
            var command = new CreateLessonMaterialCommand();

            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateLessonMaterialCommand>(), default))
                .Returns(Task.FromResult(Unit.Value));

            // Act
            await _controller.CreateLessonMaterial(command);

            // Assert
            Assert.That(command.SchoolId, Is.Null);
        }

        [Test]
        public async Task CreateLessonMaterial_ShouldReturn500_WhenMediatorThrowsException()
        {
            // Arrange
            var command = new CreateLessonMaterialCommand();
            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateLessonMaterialCommand>(), default))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CreateLessonMaterial(command);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(500));
        }

        [Test]
        public async Task CreateLessonMaterial_ShouldReturn400_WhenAppExceptionThrown()
        {
            // Arrange
            var command = new CreateLessonMaterialCommand();
            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateLessonMaterialCommand>(), default))
                .ThrowsAsync(new AppException(CustomCode.FolderNotFound));

            // Act
            var result = await _controller.CreateLessonMaterial(command);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(404));
        }

        #endregion

        #region GetLessonMaterials Tests

        [Test]
        public async Task GetLessonMaterials_ShouldReturn200_WhenSuccessful()
        {
            // Arrange
            var specParam = new LessonMaterialSpecParam
            {
                PageIndex = 1,
                PageSize = 10,
                Tag = "test"
            };

            var expectedResponse = new Pagination<LessonMaterialResponse>
            {
                Data = new List<LessonMaterialResponse>
                {
                    new LessonMaterialResponse { Id = Guid.NewGuid(), Title = "Test Material" }
                },
                Count = 1,
                PageIndex = 1,
                PageSize = 10
            };

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetLessonMaterialsQuery>(), default))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetLessonMaterials(specParam);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task GetLessonMaterials_ShouldReturn401_WhenUserIdNotFound()
        {
            // Arrange
            SetupUserContextWithoutUser();
            var specParam = new LessonMaterialSpecParam();

            // Act
            var result = await _controller.GetLessonMaterials(specParam);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(401));
        }

        [Test]
        public async Task GetLessonMaterials_ShouldPassCorrectParameters()
        {
            // Arrange
            var specParam = new LessonMaterialSpecParam { Tag = "test" };
            GetLessonMaterialsQuery? capturedQuery = null;

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetLessonMaterialsQuery>(), default))
                .Callback<IRequest<Pagination<LessonMaterialResponse>>, CancellationToken>((query, _) => 
                {
                    capturedQuery = query as GetLessonMaterialsQuery;
                })
                .ReturnsAsync(new Pagination<LessonMaterialResponse>());

            // Act
            await _controller.GetLessonMaterials(specParam);

            // Assert
            Assert.That(capturedQuery, Is.Not.Null);
            _mediatorMock.Verify(m => m.Send(It.IsAny<GetLessonMaterialsQuery>(), default), Times.Once);
        }

        #endregion

        #region GetLessonMaterialById Tests

        [Test]
        public async Task GetLessonMaterialById_ShouldReturn200_WhenSuccessful()
        {
            // Arrange
            var materialId = Guid.NewGuid();
            var expectedResponse = new LessonMaterialResponse 
            { 
                Id = materialId, 
                Title = "Test Material" 
            };

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetLessonMaterialByIdQuery>(), default))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetLessonMaterialById(materialId);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task GetLessonMaterialById_ShouldReturn401_WhenUserIdNotFound()
        {
            // Arrange
            SetupUserContextWithoutUser();
            var materialId = Guid.NewGuid();

            // Act
            var result = await _controller.GetLessonMaterialById(materialId);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(401));
        }

        [Test]
        public async Task GetLessonMaterialById_ShouldReturn404_WhenMaterialNotFound()
        {
            // Arrange
            var materialId = Guid.NewGuid();
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetLessonMaterialByIdQuery>(), default))
                .ThrowsAsync(new AppException(CustomCode.LessonMaterialNotFound));

            // Act
            var result = await _controller.GetLessonMaterialById(materialId);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public async Task GetLessonMaterialById_ShouldPassCorrectParameters()
        {
            // Arrange
            var materialId = Guid.NewGuid();
            GetLessonMaterialByIdQuery? capturedQuery = null;

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetLessonMaterialByIdQuery>(), default))
                .Callback<IRequest<LessonMaterialResponse>, CancellationToken>((query, _) => 
                {
                    capturedQuery = query as GetLessonMaterialByIdQuery;
                })
                .ReturnsAsync(new LessonMaterialResponse());

            // Act
            await _controller.GetLessonMaterialById(materialId);

            // Assert
            Assert.That(capturedQuery, Is.Not.Null);
            Assert.That(capturedQuery.Id, Is.EqualTo(materialId));
            Assert.That(capturedQuery.UserId, Is.EqualTo(_testUserId));
            Assert.That(capturedQuery.SchoolId, Is.EqualTo(_testSchoolId));
        }

        [Test]
        public async Task GetLessonMaterialById_WithoutSchoolId_ShouldSetSchoolIdToNull()
        {
            // Arrange
            SetupUserContext(schoolId: "invalid");
            var materialId = Guid.NewGuid();
            GetLessonMaterialByIdQuery? capturedQuery = null;

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetLessonMaterialByIdQuery>(), default))
                .Callback<IRequest<LessonMaterialResponse>, CancellationToken>((query, _) => 
                {
                    capturedQuery = query as GetLessonMaterialByIdQuery;
                })
                .ReturnsAsync(new LessonMaterialResponse());

            // Act
            await _controller.GetLessonMaterialById(materialId);

            // Assert
            Assert.That(capturedQuery, Is.Not.Null);
            Assert.That(capturedQuery.SchoolId, Is.Null);
        }

        [Test]
        public async Task GetLessonMaterialById_ShouldReturn403_WhenForbidden()
        {
            // Arrange
            var materialId = Guid.NewGuid();
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetLessonMaterialByIdQuery>(), default))
                .ThrowsAsync(new AppException(CustomCode.Forbidden));

            // Act
            var result = await _controller.GetLessonMaterialById(materialId);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(403));
        }

        #endregion
    }
}
