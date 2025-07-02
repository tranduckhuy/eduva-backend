using Eduva.API.Controllers.Classes;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Classes.Commands.ArchiveClass;
using Eduva.Application.Features.Classes.Commands.CreateClass;
using Eduva.Application.Features.Classes.Commands.EnrollByClassCode;
using Eduva.Application.Features.Classes.Commands.RemoveStudentFromClass;
using Eduva.Application.Features.Classes.Commands.ResetClassCode;
using Eduva.Application.Features.Classes.Commands.RestoreClass;
using Eduva.Application.Features.Classes.Commands.UpdateClass;
using Eduva.Application.Features.Classes.Queries.GetAllStudentsInClass;
using Eduva.Application.Features.Classes.Queries.GetClassById;
using Eduva.Application.Features.Classes.Queries.GetClasses;
using Eduva.Application.Features.Classes.Queries.GetStudentById;
using Eduva.Application.Features.Classes.Queries.GetStudentClasses;
using Eduva.Application.Features.Classes.Queries.GetTeacherClasses;
using Eduva.Application.Features.Classes.Responses;
using Eduva.Application.Features.Classes.Specifications;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Eduva.API.Test.Controllers.Classes
{
    [TestFixture]
    public class ClassControllerTests
    {
        #region Setup

        private Mock<IMediator> _mediatorMock;
        private ClassController _controller;
        private Guid _testUserId = Guid.NewGuid();

        [SetUp]
        public void Setup()
        {
            _mediatorMock = new Mock<IMediator>();
            var loggerMock = new Mock<ILogger<ClassController>>();
            _controller = new ClassController(_mediatorMock.Object, loggerMock.Object);
        }

        private void SetupUser(string role = "Teacher", bool includeUserId = true, bool includeSchoolId = false)
        {
            var claims = new List<Claim> { new(ClaimTypes.Role, role) };
            if (includeUserId) claims.Add(new(ClaimTypes.NameIdentifier, _testUserId.ToString()));
            if (includeSchoolId) claims.Add(new("SchoolId", "1"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
                }
            };
        }

        #endregion

        #region Success Tests

        [Test]
        public async Task CreateClass_Success()
        {
            SetupUser(includeSchoolId: true);
            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateClassCommand>(), default)).ReturnsAsync(new ClassResponse());
            var result = await _controller.CreateClass(new CreateClassCommand { Name = "Test" });
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(201));
        }

        [Test]
        public async Task GetClasses_Success()
        {
            SetupUser("SystemAdmin");
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetClassesQuery>(), default)).ReturnsAsync(new Pagination<ClassResponse>());
            var result = await _controller.GetClasses(new ClassSpecParam());
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task GetTeacherClasses_Success()
        {
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetTeacherClassesQuery>(), default)).ReturnsAsync(new Pagination<ClassResponse>());
            var result = await _controller.GetTeacherClasses(new ClassSpecParam());
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task GetMyClasses_Success()
        {
            SetupUser("Student");
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetStudentClassesQuery>(), default)).ReturnsAsync(new Pagination<StudentClassResponse>());
            var result = await _controller.GetMyClasses(new StudentClassSpecParam());
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task GetClassById_Success()
        {
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetClassByIdQuery>(), default)).ReturnsAsync(new ClassResponse());
            var result = await _controller.GetClassById(Guid.NewGuid());
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task GetAllStudentsInClass_Success()
        {
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllStudentsInClassQuery>(), default)).ReturnsAsync(new Pagination<StudentClassResponse>());
            var result = await _controller.GetAllStudentsInClass(Guid.NewGuid(), new StudentClassSpecParam());
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task GetStudentById_Success()
        {
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetStudentByIdQuery>(), default)).ReturnsAsync(new StudentClassResponse());
            var result = await _controller.GetStudentById(Guid.NewGuid());
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task UpdateClass_Success()
        {
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateClassCommand>(), default)).ReturnsAsync(Unit.Value);
            var result = await _controller.UpdateClass(Guid.NewGuid(), new UpdateClassCommand { Name = "Test" });
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task ResetClassCode_Success()
        {
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.IsAny<ResetClassCodeCommand>(), default)).ReturnsAsync(new ClassResponse());
            var result = await _controller.ResetClassCode(Guid.NewGuid());
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task EnrollByClassCode_Success()
        {
            SetupUser("Student");
            _mediatorMock.Setup(m => m.Send(It.IsAny<EnrollByClassCodeCommand>(), default)).ReturnsAsync(new StudentClassResponse());
            var result = await _controller.EnrollByClassCode(new EnrollByClassCodeCommand { ClassCode = "ABC123" });
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task ArchiveClass_Success()
        {
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.IsAny<ArchiveClassCommand>(), default)).ReturnsAsync(Unit.Value);
            var result = await _controller.ArchiveClass(Guid.NewGuid());
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task RestoreClass_Success()
        {
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.IsAny<RestoreClassCommand>(), default)).ReturnsAsync(Unit.Value);
            var result = await _controller.RestoreClass(Guid.NewGuid());
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(200));
        }

        #endregion

        #region Error Tests - Validation & Authentication

        [TestCase("CreateClass")]
        [TestCase("GetClasses")]
        [TestCase("GetTeacherClasses")]
        [TestCase("GetMyClasses")]
        [TestCase("GetClassById")]
        [TestCase("GetAllStudentsInClass")]
        [TestCase("GetStudentById")]
        [TestCase("UpdateClass")]
        [TestCase("ResetClassCode")]
        public async Task Methods_ShouldReturn401_WhenUserIdMissing(string methodName)
        {
            SetupUser(includeUserId: false);
            var result = methodName switch
            {
                "CreateClass" => await _controller.CreateClass(new CreateClassCommand { Name = "Test" }),
                "GetClasses" => await _controller.GetClasses(new ClassSpecParam()),
                "GetTeacherClasses" => await _controller.GetTeacherClasses(new ClassSpecParam()),
                "GetMyClasses" => await _controller.GetMyClasses(new StudentClassSpecParam()),
                "GetClassById" => await _controller.GetClassById(Guid.NewGuid()),
                "GetAllStudentsInClass" => await _controller.GetAllStudentsInClass(Guid.NewGuid(), new StudentClassSpecParam()),
                "GetStudentById" => await _controller.GetStudentById(Guid.NewGuid()),
                "UpdateClass" => await _controller.UpdateClass(Guid.NewGuid(), new UpdateClassCommand { Name = "Test" }),
                "ResetClassCode" => await _controller.ResetClassCode(Guid.NewGuid()),
                _ => throw new ArgumentException("Invalid method name")
            };
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(401));
        }

        [TestCase("CreateClass")]
        [TestCase("GetClasses")]
        [TestCase("UpdateClass")]
        public async Task Methods_ShouldReturn400_WhenModelInvalid(string methodName)
        {
            SetupUser();
            _controller.ModelState.AddModelError("test", "error");
            var result = methodName switch
            {
                "CreateClass" => await _controller.CreateClass(new CreateClassCommand()),
                "GetClasses" => await _controller.GetClasses(new ClassSpecParam()),
                "UpdateClass" => await _controller.UpdateClass(Guid.NewGuid(), new UpdateClassCommand()),
                _ => throw new ArgumentException("Invalid method name")
            };
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(400));
        }

        #endregion

        #region Exception Handling Tests

        [Test]
        public async Task ArchiveClass_AppException()
        {
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.IsAny<ArchiveClassCommand>(), default))
                .ThrowsAsync(new AppException(CustomCode.ClassAlreadyArchived, new List<string>()));
            var result = await _controller.ArchiveClass(Guid.NewGuid());
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(400));
        }

        [Test]
        public async Task ArchiveClass_GenericException()
        {
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.IsAny<ArchiveClassCommand>(), default)).ThrowsAsync(new Exception());
            var result = await _controller.ArchiveClass(Guid.NewGuid());
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(500));
        }

        [Test]
        public async Task RestoreClass_AppException()
        {
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.IsAny<RestoreClassCommand>(), default))
                .ThrowsAsync(new AppException(CustomCode.ClassNotArchived, new List<string>()));
            var result = await _controller.RestoreClass(Guid.NewGuid());
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(400));
        }

        [Test]
        public async Task EnrollByClassCode_AppException()
        {
            SetupUser("Student");
            _mediatorMock.Setup(m => m.Send(It.IsAny<EnrollByClassCodeCommand>(), default))
                .ThrowsAsync(new AppException(CustomCode.StudentAlreadyEnrolled, new List<string>()));
            var result = await _controller.EnrollByClassCode(new EnrollByClassCodeCommand { ClassCode = "ABC" });
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(400));
        }

        #endregion

        #region Business Logic Tests

        [Test]
        public async Task RemoveStudentFromClass_AsTeacher()
        {
            SetupUser("Teacher");
            _mediatorMock.Setup(m => m.Send(It.IsAny<RemoveStudentFromClassCommand>(), default)).ReturnsAsync(Unit.Value);
            var result = await _controller.RemoveStudentFromClass(Guid.NewGuid(), Guid.NewGuid());
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(200));
            _mediatorMock.Verify(m => m.Send(It.Is<RemoveStudentFromClassCommand>(c => c.IsTeacher), default));
        }

        [Test]
        public async Task RemoveStudentFromClass_AsSchoolAdmin()
        {
            SetupUser("SchoolAdmin");
            _mediatorMock.Setup(m => m.Send(It.IsAny<RemoveStudentFromClassCommand>(), default)).ReturnsAsync(Unit.Value);
            var result = await _controller.RemoveStudentFromClass(Guid.NewGuid(), Guid.NewGuid());
            _mediatorMock.Verify(m => m.Send(It.Is<RemoveStudentFromClassCommand>(c => c.IsSchoolAdmin), default));
        }

        [Test]
        public async Task RemoveStudentFromClass_AsSystemAdmin()
        {
            SetupUser("SystemAdmin");
            _mediatorMock.Setup(m => m.Send(It.IsAny<RemoveStudentFromClassCommand>(), default)).ReturnsAsync(Unit.Value);
            var result = await _controller.RemoveStudentFromClass(Guid.NewGuid(), Guid.NewGuid());
            _mediatorMock.Verify(m => m.Send(It.Is<RemoveStudentFromClassCommand>(c => c.IsSystemAdmin), default));
        }

        [Test]
        public async Task CreateClass_SetsSchoolIdFromClaim()
        {
            SetupUser(includeSchoolId: true);
            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateClassCommand>(), default)).ReturnsAsync(new ClassResponse());
            await _controller.CreateClass(new CreateClassCommand { Name = "Test", SchoolId = 0 });
            _mediatorMock.Verify(m => m.Send(It.Is<CreateClassCommand>(c => c.SchoolId == 1), default));
        }

        #endregion
    }
}