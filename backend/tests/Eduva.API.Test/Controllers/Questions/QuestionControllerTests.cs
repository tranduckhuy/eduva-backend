using Eduva.API.Controllers.Questions;
using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Models;
using Eduva.Application.Features.Questions.Commands.CreateQuestion;
using Eduva.Application.Features.Questions.Commands.CreateQuestionComment;
using Eduva.Application.Features.Questions.Commands.DeleteQuestion;
using Eduva.Application.Features.Questions.Commands.DeleteQuestionComment;
using Eduva.Application.Features.Questions.Commands.UpdateQuestion;
using Eduva.Application.Features.Questions.Commands.UpdateQuestionComment;
using Eduva.Application.Features.Questions.Queries;
using Eduva.Application.Features.Questions.Responses;
using Eduva.Application.Features.Questions.Specifications;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Eduva.API.Test.Controllers.Questions
{
    [TestFixture]
    public class QuestionControllerTests
    {
        private Mock<IMediator> _mediatorMock;
        private Mock<ILogger<QuestionController>> _loggerMock;
        private QuestionController _controller;
        private readonly Guid _validUserId = Guid.NewGuid();
        private const string ValidUserIdString = "123e4567-e89b-12d3-a456-426614174000";

        #region Setup

        [SetUp]
        public void Setup()
        {
            _mediatorMock = new Mock<IMediator>();
            _loggerMock = new Mock<ILogger<QuestionController>>();
            _controller = new QuestionController(_mediatorMock.Object, _loggerMock.Object);

            SetupControllerContext(ValidUserIdString);
        }

        private void SetupControllerContext(string? userId)
        {
            var claims = new List<Claim>();
            if (!string.IsNullOrEmpty(userId))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            }

            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_ShouldInitialize_WithValidParameters()
        {
            // Arrange & Act
            var controller = new QuestionController(_mediatorMock.Object, _loggerMock.Object);

            // Assert
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller, Is.InstanceOf<QuestionController>());
        }

        #endregion

        #region GetQuestionsByLesson Tests

        [Test]
        public async Task GetQuestionsByLesson_ShouldReturnSuccess_WhenValidRequest()
        {
            // Arrange
            var lessonMaterialId = Guid.NewGuid();
            var specParam = new QuestionsByLessonSpecParam();
            var expectedResult = new Pagination<QuestionResponse>();

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetQuestionsByLessonQuery>(), default))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetQuestionsByLesson(lessonMaterialId, specParam);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

            _mediatorMock.Verify(m => m.Send(
                It.Is<GetQuestionsByLessonQuery>(q =>
                    q.LessonMaterialId == lessonMaterialId &&
                    q.CurrentUserId == Guid.Parse(ValidUserIdString)),
                default), Times.Once);
        }

        [Test]
        public async Task GetQuestionsByLesson_ShouldReturnUserIdNotFound_WhenInvalidUserId()
        {
            // Arrange
            SetupControllerContext("invalid-guid");
            var lessonMaterialId = Guid.NewGuid();
            var specParam = new QuestionsByLessonSpecParam();

            // Act
            var result = await _controller.GetQuestionsByLesson(lessonMaterialId, specParam);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));

            _mediatorMock.Verify(m => m.Send(It.IsAny<GetQuestionsByLessonQuery>(), default), Times.Never);
        }

        [Test]
        public async Task GetQuestionsByLesson_ShouldReturnUserIdNotFound_WhenUserIdIsNull()
        {
            // Arrange
            SetupControllerContext(null);
            var lessonMaterialId = Guid.NewGuid();
            var specParam = new QuestionsByLessonSpecParam();

            // Act
            var result = await _controller.GetQuestionsByLesson(lessonMaterialId, specParam);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task GetQuestionsByLesson_ShouldHandleException_WhenMediatorThrows()
        {
            // Arrange
            var lessonMaterialId = Guid.NewGuid();
            var specParam = new QuestionsByLessonSpecParam();

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetQuestionsByLessonQuery>(), default))
                .ThrowsAsync(new AppException(CustomCode.SystemError));

            // Act
            var result = await _controller.GetQuestionsByLesson(lessonMaterialId, specParam);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        }

        #endregion

        #region GetMyQuestions Tests

        [Test]
        public async Task GetMyQuestions_ShouldReturnSuccess_WhenValidRequest()
        {
            // Arrange
            var specParam = new MyQuestionsSpecParam();
            var expectedResult = new Pagination<QuestionResponse>();

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetMyQuestionsQuery>(), default))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetMyQuestions(specParam);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

            _mediatorMock.Verify(m => m.Send(
                It.Is<GetMyQuestionsQuery>(q => q.UserId == Guid.Parse(ValidUserIdString)),
                default), Times.Once);
        }

        [Test]
        public async Task GetMyQuestions_ShouldReturnUserIdNotFound_WhenInvalidUserId()
        {
            // Arrange
            SetupControllerContext("invalid-guid");
            var specParam = new MyQuestionsSpecParam();

            // Act
            var result = await _controller.GetMyQuestions(specParam);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        #endregion

        #region GetQuestionDetail Tests

        [Test]
        public async Task GetQuestionDetail_ShouldReturnSuccess_WhenValidRequest()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var expectedResult = new QuestionDetailResponse();

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetQuestionDetailQuery>(), default))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetQuestionDetail(questionId);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

            _mediatorMock.Verify(m => m.Send(
                It.Is<GetQuestionDetailQuery>(q =>
                    q.QuestionId == questionId &&
                    q.CurrentUserId == Guid.Parse(ValidUserIdString)),
                default), Times.Once);
        }

        [Test]
        public async Task GetQuestionDetail_ShouldReturnUserIdNotFound_WhenInvalidUserId()
        {
            // Arrange
            SetupControllerContext("invalid-guid");
            var questionId = Guid.NewGuid();

            // Act
            var result = await _controller.GetQuestionDetail(questionId);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        #endregion

        #region CreateQuestion Tests

        [Test]
        public async Task CreateQuestion_ShouldReturnCreated_WhenValidRequest()
        {
            // Arrange
            var command = new CreateQuestionCommand
            {
                LessonMaterialId = Guid.NewGuid(),
                Title = "Test Question",
                Content = "Test Content"
            };
            var expectedResult = new QuestionResponse();

            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateQuestionCommand>(), default))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.CreateQuestion(command);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status201Created));

            _mediatorMock.Verify(m => m.Send(
                It.Is<CreateQuestionCommand>(c => c.CreatedByUserId == Guid.Parse(ValidUserIdString)),
                default), Times.Once);
        }

        [Test]
        public async Task CreateQuestion_ShouldReturnUserIdNotFound_WhenInvalidUserId()
        {
            // Arrange
            SetupControllerContext("invalid-guid");
            var command = new CreateQuestionCommand();

            // Act
            var result = await _controller.CreateQuestion(command);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        [Test]
        public async Task CreateQuestion_ShouldHandleAppException()
        {
            // Arrange
            var command = new CreateQuestionCommand();
            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateQuestionCommand>(), default))
                .ThrowsAsync(new AppException(CustomCode.QuestionNotFound));

            // Act
            var result = await _controller.CreateQuestion(command);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
        }

        #endregion

        #region UpdateQuestion Tests

        [Test]
        public async Task UpdateQuestion_ShouldReturnSuccess_WhenValidRequest()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var command = new UpdateQuestionCommand
            {
                Title = "Updated Title",
                Content = "Updated Content"
            };
            var expectedResult = new QuestionResponse();

            _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateQuestionCommand>(), default))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.UpdateQuestion(questionId, command);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

            _mediatorMock.Verify(m => m.Send(
                It.Is<UpdateQuestionCommand>(c =>
                    c.Id == questionId &&
                    c.UpdatedByUserId == Guid.Parse(ValidUserIdString)),
                default), Times.Once);
        }

        [Test]
        public async Task UpdateQuestion_ShouldReturnUserIdNotFound_WhenInvalidUserId()
        {
            // Arrange
            SetupControllerContext("invalid-guid");
            var questionId = Guid.NewGuid();
            var command = new UpdateQuestionCommand();

            // Act
            var result = await _controller.UpdateQuestion(questionId, command);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        #endregion

        #region DeleteQuestion Tests

        [Test]
        public async Task DeleteQuestion_ShouldReturnSuccess_WhenValidRequest()
        {
            // Arrange
            var questionId = Guid.NewGuid();

            // Mock setup for Delete commands that return Task<bool>
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteQuestionCommand>(), default))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteQuestion(questionId);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

            _mediatorMock.Verify(m => m.Send(
                It.Is<DeleteQuestionCommand>(c =>
                    c.Id == questionId &&
                    c.DeletedByUserId == Guid.Parse(ValidUserIdString)),
                default), Times.Once);
        }

        [Test]
        public async Task DeleteQuestion_ShouldReturnUserIdNotFound_WhenInvalidUserId()
        {
            // Arrange
            SetupControllerContext("invalid-guid");
            var questionId = Guid.NewGuid();

            // Act
            var result = await _controller.DeleteQuestion(questionId);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        #endregion

        #region CreateQuestionComment Tests

        [Test]
        public async Task CreateQuestionComment_ShouldReturnCreated_WhenValidRequest()
        {
            // Arrange
            var command = new CreateQuestionCommentCommand
            {
                QuestionId = Guid.NewGuid(),
                Content = "Test Comment"
            };
            var expectedResult = new QuestionCommentResponse();

            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateQuestionCommentCommand>(), default))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.CreateQuestionComment(command);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status201Created));

            _mediatorMock.Verify(m => m.Send(
                It.Is<CreateQuestionCommentCommand>(c => c.CreatedByUserId == Guid.Parse(ValidUserIdString)),
                default), Times.Once);
        }

        [Test]
        public async Task CreateQuestionComment_ShouldReturnUserIdNotFound_WhenInvalidUserId()
        {
            // Arrange
            SetupControllerContext("invalid-guid");
            var command = new CreateQuestionCommentCommand();

            // Act
            var result = await _controller.CreateQuestionComment(command);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        #endregion

        #region UpdateQuestionComment Tests

        [Test]
        public async Task UpdateQuestionComment_ShouldReturnSuccess_WhenValidRequest()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            var command = new UpdateQuestionCommentCommand
            {
                Content = "Updated Comment"
            };
            var expectedResult = new QuestionCommentResponse();

            _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateQuestionCommentCommand>(), default))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.UpdateQuestionComment(commentId, command);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

            _mediatorMock.Verify(m => m.Send(
                It.Is<UpdateQuestionCommentCommand>(c =>
                    c.Id == commentId &&
                    c.UpdatedByUserId == Guid.Parse(ValidUserIdString)),
                default), Times.Once);
        }

        [Test]
        public async Task UpdateQuestionComment_ShouldReturnUserIdNotFound_WhenInvalidUserId()
        {
            // Arrange
            SetupControllerContext("invalid-guid");
            var commentId = Guid.NewGuid();
            var command = new UpdateQuestionCommentCommand();

            // Act
            var result = await _controller.UpdateQuestionComment(commentId, command);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        #endregion

        #region DeleteQuestionComment Tests

        [Test]
        public async Task DeleteQuestionComment_ShouldReturnSuccess_WhenValidRequest()
        {
            // Arrange
            var commentId = Guid.NewGuid();

            // Mock setup for Delete commands that return Task<bool>
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteQuestionCommentCommand>(), default))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteQuestionComment(commentId);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status200OK));

            _mediatorMock.Verify(m => m.Send(
                It.Is<DeleteQuestionCommentCommand>(c =>
                    c.Id == commentId &&
                    c.DeletedByUserId == Guid.Parse(ValidUserIdString)),
                default), Times.Once);
        }

        [Test]
        public async Task DeleteQuestionComment_ShouldReturnUserIdNotFound_WhenInvalidUserId()
        {
            // Arrange
            SetupControllerContext("invalid-guid");
            var commentId = Guid.NewGuid();

            // Act
            var result = await _controller.DeleteQuestionComment(commentId);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        }

        #endregion

        #region Model State Tests

        [Test]
        public async Task CreateQuestion_ShouldReturnBadRequest_WhenModelStateInvalid()
        {
            // Arrange
            var command = new CreateQuestionCommand();
            _controller.ModelState.AddModelError("Title", "Title is required");

            // Act
            var result = await _controller.CreateQuestion(command);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));

            _mediatorMock.Verify(m => m.Send(It.IsAny<CreateQuestionCommand>(), default), Times.Never);
        }

        [Test]
        public async Task CreateQuestionComment_ShouldReturnBadRequest_WhenModelStateInvalid()
        {
            // Arrange
            var command = new CreateQuestionCommentCommand();
            _controller.ModelState.AddModelError("Content", "Content is required");

            // Act
            var result = await _controller.CreateQuestionComment(command);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        }

        #endregion

        #region Exception Handling Tests

        [Test]
        public async Task GetQuestionDetail_ShouldReturnInternalServerError_WhenUnhandledExceptionThrown()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetQuestionDetailQuery>(), default))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.GetQuestionDetail(questionId);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        }

        [Test]
        public async Task UpdateQuestion_ShouldHandleAppException_WithCustomCode()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            var command = new UpdateQuestionCommand();
            _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateQuestionCommand>(), default))
                .ThrowsAsync(new AppException(CustomCode.InsufficientPermissionToUpdateQuestion));

            // Act
            var result = await _controller.UpdateQuestion(questionId, command);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
        }

        [Test]
        public async Task DeleteQuestion_ShouldHandleException_WhenMediatorThrows()
        {
            // Arrange
            var questionId = Guid.NewGuid();
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteQuestionCommand>(), default))
                .ThrowsAsync(new Exception("Delete failed"));

            // Act
            var result = await _controller.DeleteQuestion(questionId);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        }

        [Test]
        public async Task DeleteQuestionComment_ShouldHandleAppException()
        {
            // Arrange
            var commentId = Guid.NewGuid();
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteQuestionCommentCommand>(), default))
                .ThrowsAsync(new AppException(CustomCode.InsufficientPermissionToDeleteComment));

            // Act
            var result = await _controller.DeleteQuestionComment(commentId);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
        }

        #endregion

        #region TearDown

        [TearDown]
        public void TearDown()
        {
            _mediatorMock?.Reset();
            _loggerMock?.Reset();
        }

        #endregion
    }
}