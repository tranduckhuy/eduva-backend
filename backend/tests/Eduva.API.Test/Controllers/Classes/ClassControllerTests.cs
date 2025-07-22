using Eduva.API.Controllers.Classes;
using Eduva.API.Models;
using Eduva.Application.Common.Constants;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Classes.Commands.AddMaterialsToFolder;
using Eduva.Application.Features.Classes.Commands.ArchiveClass;
using Eduva.Application.Features.Classes.Commands.CreateClass;
using Eduva.Application.Features.Classes.Commands.EnrollByClassCode;
using Eduva.Application.Features.Classes.Commands.RemoveMaterialsFromFolder;
using Eduva.Application.Features.Classes.Commands.RemoveStudentsFromClass;
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
using Eduva.Application.Features.LessonMaterials.Queries.GetFoldersWithLessonMaterialsByClassId;
using Eduva.Application.Features.LessonMaterials.Responses;
using Eduva.Domain.Enums;
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

        [Test]
        public async Task RemoveStudentsFromClass_Success()
        {
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.IsAny<RemoveStudentsFromClassCommand>(), default)).ReturnsAsync(Unit.Value);
            var studentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var result = await _controller.RemoveStudentsFromClass(Guid.NewGuid(), studentIds);
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task AddMaterialsToFolder_Success()
        {
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.IsAny<AddMaterialsToFolderCommand>(), default)).ReturnsAsync(true);
            var materialIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var result = await _controller.AddMaterialsToFolder(Guid.NewGuid(), Guid.NewGuid(), materialIds);
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
        [TestCase("EnrollByClassCode")]
        [TestCase("ArchiveClass")]
        [TestCase("RestoreClass")]
        [TestCase("RemoveStudentsFromClass")]
        [TestCase("AddMaterialsToFolder")]
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
                "EnrollByClassCode" => await _controller.EnrollByClassCode(new EnrollByClassCodeCommand { ClassCode = "ABC123" }),
                "ArchiveClass" => await _controller.ArchiveClass(Guid.NewGuid()),
                "RestoreClass" => await _controller.RestoreClass(Guid.NewGuid()),
                "RemoveStudentsFromClass" => await _controller.RemoveStudentsFromClass(Guid.NewGuid(), new List<Guid> { Guid.NewGuid() }),
                "AddMaterialsToFolder" => await _controller.AddMaterialsToFolder(Guid.NewGuid(), Guid.NewGuid(), new List<Guid>()),
                _ => throw new ArgumentException("Invalid method name")
            };
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(401));
        }

        [TestCase("CreateClass")]
        [TestCase("GetClasses")]
        [TestCase("GetTeacherClasses")]
        [TestCase("GetMyClasses")]
        [TestCase("GetClassById")]
        [TestCase("GetAllStudentsInClass")]
        [TestCase("GetStudentById")]
        [TestCase("UpdateClass")]
        [TestCase("ResetClassCode")]
        [TestCase("EnrollByClassCode")]
        [TestCase("ArchiveClass")]
        [TestCase("RestoreClass")]
        [TestCase("RemoveStudentsFromClass")]
        [TestCase("AddMaterialsToFolder")]
        public async Task Methods_ShouldReturn400_WhenModelInvalid(string methodName)
        {
            SetupUser();
            _controller.ModelState.AddModelError("test", "error");
            var result = methodName switch
            {
                "CreateClass" => await _controller.CreateClass(new CreateClassCommand()),
                "GetClasses" => await _controller.GetClasses(new ClassSpecParam()),
                "GetTeacherClasses" => await _controller.GetTeacherClasses(new ClassSpecParam()),
                "GetMyClasses" => await _controller.GetMyClasses(new StudentClassSpecParam()),
                "GetClassById" => await _controller.GetClassById(Guid.NewGuid()),
                "GetAllStudentsInClass" => await _controller.GetAllStudentsInClass(Guid.NewGuid(), new StudentClassSpecParam()),
                "GetStudentById" => await _controller.GetStudentById(Guid.NewGuid()),
                "UpdateClass" => await _controller.UpdateClass(Guid.NewGuid(), new UpdateClassCommand()),
                "ResetClassCode" => await _controller.ResetClassCode(Guid.NewGuid()),
                "EnrollByClassCode" => await _controller.EnrollByClassCode(new EnrollByClassCodeCommand()),
                "ArchiveClass" => await _controller.ArchiveClass(Guid.NewGuid()),
                "RestoreClass" => await _controller.RestoreClass(Guid.NewGuid()),
                "RemoveStudentsFromClass" => await _controller.RemoveStudentsFromClass(Guid.NewGuid(), new List<Guid> { Guid.NewGuid() }),
                "AddMaterialsToFolder" => await _controller.AddMaterialsToFolder(Guid.NewGuid(), Guid.NewGuid(), new List<Guid>()),
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
        public async Task RestoreClass_GenericException()
        {
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.IsAny<RestoreClassCommand>(), default)).ThrowsAsync(new Exception());
            var result = await _controller.RestoreClass(Guid.NewGuid());
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(500));
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

        [Test]
        public async Task EnrollByClassCode_GenericException()
        {
            SetupUser("Student");
            _mediatorMock.Setup(m => m.Send(It.IsAny<EnrollByClassCodeCommand>(), default)).ThrowsAsync(new Exception());
            var result = await _controller.EnrollByClassCode(new EnrollByClassCodeCommand { ClassCode = "ABC" });
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(500));
        }

        [Test]
        public async Task RemoveStudentsFromClass_AppException()
        {
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.IsAny<RemoveStudentsFromClassCommand>(), default))
                .ThrowsAsync(new AppException(CustomCode.StudentNotEnrolled, new List<string>()));
            var studentIds = new List<Guid> { Guid.NewGuid() };
            var result = await _controller.RemoveStudentsFromClass(Guid.NewGuid(), studentIds);
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(404));
        }

        [Test]
        public async Task RemoveStudentsFromClass_GenericException()
        {
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.IsAny<RemoveStudentsFromClassCommand>(), default)).ThrowsAsync(new Exception());
            var studentIds = new List<Guid> { Guid.NewGuid() };
            var result = await _controller.RemoveStudentsFromClass(Guid.NewGuid(), studentIds);
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(500));
        }

        [Test]
        public async Task AddMaterialsToFolder_AppException()
        {
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.IsAny<AddMaterialsToFolderCommand>(), default))
                .ThrowsAsync(new AppException(CustomCode.FolderNotFound, new List<string>()));
            var materialIds = new List<Guid> { Guid.NewGuid() };
            var result = await _controller.AddMaterialsToFolder(Guid.NewGuid(), Guid.NewGuid(), materialIds);
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(404));
        }

        [Test]
        public async Task AddMaterialsToFolder_GenericException()
        {
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.IsAny<AddMaterialsToFolderCommand>(), default)).ThrowsAsync(new Exception());
            var materialIds = new List<Guid> { Guid.NewGuid() };
            var result = await _controller.AddMaterialsToFolder(Guid.NewGuid(), Guid.NewGuid(), materialIds);
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(500));
        }

        #endregion

        #region Business Logic Tests

        [Test]
        public async Task RemoveStudentsFromClass_AsTeacher()
        {
            SetupUser("Teacher");
            _mediatorMock.Setup(m => m.Send(It.IsAny<RemoveStudentsFromClassCommand>(), default)).ReturnsAsync(Unit.Value);
            var studentIds = new List<Guid> { Guid.NewGuid() };
            var result = await _controller.RemoveStudentsFromClass(Guid.NewGuid(), studentIds);
            Assert.That(((ObjectResult)result).StatusCode, Is.EqualTo(200));
            _mediatorMock.Verify(m => m.Send(It.Is<RemoveStudentsFromClassCommand>(c => c.IsTeacher), default));
        }

        [Test]
        public async Task RemoveStudentsFromClass_AsSchoolAdmin()
        {
            SetupUser("SchoolAdmin");
            _mediatorMock.Setup(m => m.Send(It.IsAny<RemoveStudentsFromClassCommand>(), default)).ReturnsAsync(Unit.Value);
            var studentIds = new List<Guid> { Guid.NewGuid() };
            await _controller.RemoveStudentsFromClass(Guid.NewGuid(), studentIds);
            _mediatorMock.Verify(m => m.Send(It.Is<RemoveStudentsFromClassCommand>(c => c.IsSchoolAdmin), default));
        }

        [Test]
        public async Task RemoveStudentsFromClass_AsSystemAdmin()
        {
            SetupUser("SystemAdmin");
            _mediatorMock.Setup(m => m.Send(It.IsAny<RemoveStudentsFromClassCommand>(), default)).ReturnsAsync(Unit.Value);
            var studentIds = new List<Guid> { Guid.NewGuid() };
            await _controller.RemoveStudentsFromClass(Guid.NewGuid(), studentIds);
            _mediatorMock.Verify(m => m.Send(It.Is<RemoveStudentsFromClassCommand>(c => c.IsSystemAdmin), default));
        }

        [Test]
        public async Task CreateClass_SetsSchoolIdFromClaim()
        {
            SetupUser(includeSchoolId: true);
            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateClassCommand>(), default)).ReturnsAsync(new ClassResponse());
            await _controller.CreateClass(new CreateClassCommand { Name = "Test", SchoolId = 0 });
            _mediatorMock.Verify(m => m.Send(It.Is<CreateClassCommand>(c => c.SchoolId == 1), default));
        }

        [Test]
        public async Task CreateClass_DoesNotOverrideExistingSchoolId()
        {
            SetupUser(includeSchoolId: true);
            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateClassCommand>(), default)).ReturnsAsync(new ClassResponse());
            await _controller.CreateClass(new CreateClassCommand { Name = "Test", SchoolId = 5 });
            _mediatorMock.Verify(m => m.Send(It.Is<CreateClassCommand>(c => c.SchoolId == 5), default));
        }

        [Test]
        public async Task EnrollByClassCode_SetsStudentIdFromClaim()
        {
            SetupUser("Student");
            _mediatorMock.Setup(m => m.Send(It.IsAny<EnrollByClassCodeCommand>(), default)).ReturnsAsync(new StudentClassResponse());
            await _controller.EnrollByClassCode(new EnrollByClassCodeCommand { ClassCode = "ABC123" });
            _mediatorMock.Verify(m => m.Send(It.Is<EnrollByClassCodeCommand>(c => c.StudentId == _testUserId), default));
        }

        [Test]
        public async Task AddMaterialsToFolder_SetsCurrentUserIdFromClaim()
        {
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.IsAny<AddMaterialsToFolderCommand>(), default)).ReturnsAsync(true);
            var materialIds = new List<Guid> { Guid.NewGuid() };
            await _controller.AddMaterialsToFolder(Guid.NewGuid(), Guid.NewGuid(), materialIds);
            _mediatorMock.Verify(m => m.Send(It.Is<AddMaterialsToFolderCommand>(c => c.CurrentUserId == _testUserId), default));
        }

        [Test]
        public async Task RemoveStudentsFromClass_SetsRequestUserIdAndRolesFromClaims()
        {
            SetupUser("Teacher");
            _mediatorMock.Setup(m => m.Send(It.IsAny<RemoveStudentsFromClassCommand>(), default)).ReturnsAsync(Unit.Value);
            var studentIds = new List<Guid> { Guid.NewGuid() };
            await _controller.RemoveStudentsFromClass(Guid.NewGuid(), studentIds);
            _mediatorMock.Verify(m => m.Send(It.Is<RemoveStudentsFromClassCommand>(c =>
                c.RequestUserId == _testUserId &&
                c.IsTeacher &&
                !c.IsSchoolAdmin &&
                !c.IsSystemAdmin), default));
        }

        #endregion

        #region Additional Coverage Tests

        [Test]
        public async Task CreateClass_WithoutSchoolIdClaim_DoesNotSetSchoolId()
        {
            SetupUser(includeSchoolId: false);
            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateClassCommand>(), default)).ReturnsAsync(new ClassResponse());
            await _controller.CreateClass(new CreateClassCommand { Name = "Test", SchoolId = 0 });
            _mediatorMock.Verify(m => m.Send(It.Is<CreateClassCommand>(c => c.SchoolId == 0), default));
        }

        [Test]
        public async Task ArchiveClass_SetsTeacherIdFromClaim()
        {
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.IsAny<ArchiveClassCommand>(), default)).ReturnsAsync(Unit.Value);
            await _controller.ArchiveClass(Guid.NewGuid());
            _mediatorMock.Verify(m => m.Send(It.Is<ArchiveClassCommand>(c => c.TeacherId == _testUserId), default));
        }

        [Test]
        public async Task RestoreClass_SetsTeacherIdFromClaim()
        {
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.IsAny<RestoreClassCommand>(), default)).ReturnsAsync(Unit.Value);
            await _controller.RestoreClass(Guid.NewGuid());
            _mediatorMock.Verify(m => m.Send(It.Is<RestoreClassCommand>(c => c.TeacherId == _testUserId), default));
        }

        [Test]
        public async Task ResetClassCode_SetsTeacherIdFromClaim()
        {
            SetupUser();
            _mediatorMock.Setup(m => m.Send(It.IsAny<ResetClassCodeCommand>(), default)).ReturnsAsync(new ClassResponse());
            await _controller.ResetClassCode(Guid.NewGuid());
            _mediatorMock.Verify(m => m.Send(It.Is<ResetClassCodeCommand>(c => c.TeacherId == _testUserId), default));
        }

        [Test]
        public async Task RemoveMaterialsFromFolder_Should_Return_ModelInvalid_When_ModelState_Invalid()
        {
            SetupUser(_testUserId.ToString());
            _controller.ModelState.AddModelError("test", "error");
            var objectResult = await _controller.RemoveMaterialsFromFolder(Guid.NewGuid(), Guid.NewGuid(), new List<Guid>()) as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.ModelInvalid));
        }

        [Test]
        public async Task RemoveMaterialsFromFolder_Should_Return_UserIdNotFound_When_UserId_Invalid()
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "invalid-guid"),
                new(ClaimTypes.Role, "Teacher")
            };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
                }
            };

            var result = await _controller.RemoveMaterialsFromFolder(Guid.NewGuid(), Guid.NewGuid(), new List<Guid>());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task RemoveMaterialsFromFolder_Should_Return_Success_When_Valid()
        {
            SetupUser(_testUserId.ToString());
            _mediatorMock.Setup(m => m.Send(It.IsAny<RemoveMaterialsFromFolderCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var classId = Guid.NewGuid();
            var folderId = Guid.NewGuid();
            var materialIds = new List<Guid> { Guid.NewGuid() };

            var result = await _controller.RemoveMaterialsFromFolder(classId, folderId, materialIds);
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
            _mediatorMock.Verify(m => m.Send(It.Is<RemoveMaterialsFromFolderCommand>(c =>
                c.ClassId == classId &&
                c.FolderId == folderId &&
                c.MaterialIds == materialIds &&
                c.CurrentUserId == _testUserId
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region GetLessonMaterialsByFolder Tests

        [Test]
        public async Task GetLessonMaterialsByFolder_ShouldReturnUserIdNotFound_WhenUserIdIsNull()
        {
            // Arrange
            var claims = new List<Claim> { new(ClaimTypes.Role, "Teacher") };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
                }
            };

            // Act
            var result = await _controller.GetLessonMaterialsByFolder(Guid.NewGuid(), null, null);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.UserIdNotFound));
        }

        [Test]
        public async Task GetLessonMaterialsByFolder_ShouldReturnSchoolNotFound_WhenSchoolIdIsInvalid()
        {
            // Arrange
            var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, _testUserId.ToString()),
        new(ClaimTypes.Role, "Teacher")
        // No SchoolId claim or invalid SchoolId
    };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
                }
            };

            // Act
            var result = await _controller.GetLessonMaterialsByFolder(Guid.NewGuid(), null, null);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.SchoolNotFound));
        }

        [Test]
        public async Task GetLessonMaterialsByFolder_ShouldReturnSuccess_WhenRequestIsValid()
        {
            // Arrange
            var schoolId = 123;
            var classId = Guid.NewGuid();
            var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, _testUserId.ToString()),
        new(ClaimConstants.SchoolId, schoolId.ToString()),
        new(ClaimTypes.Role, "Teacher")
    };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
                }
            };

            var lessonStatus = LessonMaterialStatus.Pending;
            var status = EntityStatus.Active;

            var expectedResponse = new List<FolderWithLessonMaterialsResponse>();
            _mediatorMock.Setup(m => m.Send(
                It.Is<GetFoldersWithLessonMaterialsByClassIdQuery>(q =>
                    q.ClassId == classId &&
                    q.SchoolId == schoolId &&
                    q.UserId == _testUserId &&
                    q.LessonStatus == lessonStatus &&
                    q.Status == status),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetLessonMaterialsByFolder(classId, lessonStatus, status);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.Success));
                Assert.That(response.Data, Is.EqualTo(expectedResponse));
            });
        }

        [Test]
        public async Task GetLessonMaterialsByFolder_ShouldReturnAppExceptionStatusCode_WhenAppExceptionThrown()
        {
            // Arrange
            var schoolId = 123;
            var classId = Guid.NewGuid();
            var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, _testUserId.ToString()),
        new(ClaimConstants.SchoolId, schoolId.ToString()),
        new(ClaimTypes.Role, "Teacher")
    };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
                }
            };

            var appException = new AppException(CustomCode.ClassNotFound);
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetFoldersWithLessonMaterialsByClassIdQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(appException);

            // Act
            var result = await _controller.GetLessonMaterialsByFolder(classId, null, null);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.ClassNotFound));
        }

        [Test]
        public async Task GetLessonMaterialsByFolder_ShouldReturnInternalServerError_WhenGenericExceptionThrown()
        {
            // Arrange
            var schoolId = 123;
            var classId = Guid.NewGuid();
            var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, _testUserId.ToString()),
        new(ClaimConstants.SchoolId, schoolId.ToString()),
        new(ClaimTypes.Role, "Teacher")
    };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
                }
            };

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetFoldersWithLessonMaterialsByClassIdQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.GetLessonMaterialsByFolder(classId, null, null);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
        }

        [Test]
        public async Task GetLessonMaterialsByFolder_ShouldReturnModelInvalid_WhenModelStateIsInvalid()
        {
            // Arrange
            var schoolId = 123;
            var classId = Guid.NewGuid();
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, _testUserId.ToString()),
                new(ClaimConstants.SchoolId, schoolId.ToString()),
                new(ClaimTypes.Role, "Teacher")
            };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
                }
            };

            _controller.ModelState.AddModelError("test", "error");

            // Act
            var result = await _controller.GetLessonMaterialsByFolder(classId, null, null);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            var response = objectResult!.Value as ApiResponse<object>;
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.StatusCode, Is.EqualTo((int)CustomCode.ModelInvalid));
        }

        [Test]
        public async Task GetLessonMaterialsByFolder_ShouldSendCorrectParameters_WhenRequestIsValid()
        {
            // Arrange
            var schoolId = 123;
            var classId = Guid.NewGuid();
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, _testUserId.ToString()),
                new(ClaimConstants.SchoolId, schoolId.ToString()),
                new(ClaimTypes.Role, "Teacher"),
                new(ClaimTypes.Role, "ContentCreator")
            };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
                }
            };

            var lessonStatus = LessonMaterialStatus.Approved;
            var status = EntityStatus.Active;

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetFoldersWithLessonMaterialsByClassIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FolderWithLessonMaterialsResponse>());

            // Act
            await _controller.GetLessonMaterialsByFolder(classId, lessonStatus, status);

            // Assert
            _mediatorMock.Verify(m => m.Send(
                It.Is<GetFoldersWithLessonMaterialsByClassIdQuery>(q =>
                    q.ClassId == classId &&
                    q.SchoolId == schoolId &&
                    q.UserId == _testUserId &&
                    q.UserRoles.Count == 2 &&
                    q.UserRoles.Contains("Teacher") &&
                    q.UserRoles.Contains("ContentCreator") &&
                    q.LessonStatus == lessonStatus &&
                    q.Status == status),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task GetLessonMaterialsByFolder_ShouldHandleNullFilters_WhenNotProvided()
        {
            // Arrange
            var schoolId = 123;
            var classId = Guid.NewGuid();
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, _testUserId.ToString()),
                new(ClaimConstants.SchoolId, schoolId.ToString()),
                new(ClaimTypes.Role, "Teacher")
            };
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
                }
            };

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetFoldersWithLessonMaterialsByClassIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FolderWithLessonMaterialsResponse>());

            // Act
            await _controller.GetLessonMaterialsByFolder(classId, null, null);

            // Assert
            _mediatorMock.Verify(m => m.Send(
                It.Is<GetFoldersWithLessonMaterialsByClassIdQuery>(q =>
                    q.ClassId == classId &&
                    q.LessonStatus == null &&
                    q.Status == null),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion
    }
}