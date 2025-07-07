using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Questions.Queries;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Moq;

namespace Eduva.Application.Test.Features.Questions.Queries
{
    [TestFixture]
    public class GetQuestionDetailQueryHandlerTests
    {
        #region Setup

        private Mock<ILessonMaterialQuestionRepository> _questionRepositoryMock = null!;
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IQuestionPermissionService> _permissionServiceMock = null!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepositoryMock = null!;
        private GetQuestionDetailQueryHandler _handler = null!;

        [SetUp]
        public void Setup()
        {
            _questionRepositoryMock = new Mock<ILessonMaterialQuestionRepository>();
            _userRepositoryMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _permissionServiceMock = new Mock<IQuestionPermissionService>();

            _unitOfWorkMock.Setup(x => x.GetRepository<ApplicationUser, Guid>())
                .Returns(_userRepositoryMock.Object);

            _handler = new GetQuestionDetailQueryHandler(
                _questionRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _permissionServiceMock.Object);
        }

        #endregion

        #region User Validation Tests

        [Test]
        public void Handle_ShouldThrowUserNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var query = new GetQuestionDetailQuery(Guid.NewGuid(), Guid.NewGuid());

            _userRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((ApplicationUser?)null);

            // Act & Assert
            Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
        }

        #endregion

        #region Question Access Tests

        [Test]
        public void Handle_ShouldThrowQuestionNotFound_WhenUserHasNoAccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var query = new GetQuestionDetailQuery(questionId, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _permissionServiceMock.Setup(x => x.GetUserRoleSafelyAsync(user))
                .ReturnsAsync("Student");
            _questionRepositoryMock.Setup(x => x.IsQuestionAccessibleToUserAsync(questionId, userId, "Student"))
                .ReturnsAsync(false);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.QuestionNotFound));
        }

        [Test]
        public void Handle_ShouldThrowQuestionNotFound_WhenQuestionDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var query = new GetQuestionDetailQuery(questionId, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _permissionServiceMock.Setup(x => x.GetUserRoleSafelyAsync(user))
                .ReturnsAsync("Student");
            _questionRepositoryMock.Setup(x => x.IsQuestionAccessibleToUserAsync(questionId, userId, "Student"))
                .ReturnsAsync(true);
            _questionRepositoryMock.Setup(x => x.GetQuestionWithFullDetailsAsync(questionId))
                .ReturnsAsync((LessonMaterialQuestion?)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.QuestionNotFound));
        }

        #endregion

        #region Lesson Material Validation Tests

        [Test]
        public void Handle_ShouldThrowLessonMaterialNotActive_WhenLessonMaterialIsInactive()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var query = new GetQuestionDetailQuery(questionId, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                LessonMaterial = new LessonMaterial
                {
                    Status = EntityStatus.Inactive,
                    LessonStatus = LessonMaterialStatus.Approved,
                    SchoolId = 1
                }
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _permissionServiceMock.Setup(x => x.GetUserRoleSafelyAsync(user))
                .ReturnsAsync("Student");
            _questionRepositoryMock.Setup(x => x.IsQuestionAccessibleToUserAsync(questionId, userId, "Student"))
                .ReturnsAsync(true);
            _questionRepositoryMock.Setup(x => x.GetQuestionWithFullDetailsAsync(questionId))
                .ReturnsAsync(question);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.LessonMaterialNotActive));
        }

        [Test]
        public void Handle_ShouldThrowCannotCreateQuestionForPendingLesson_WhenLessonMaterialIsPending()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var query = new GetQuestionDetailQuery(questionId, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                LessonMaterial = new LessonMaterial
                {
                    Status = EntityStatus.Active,
                    LessonStatus = LessonMaterialStatus.Pending,
                    SchoolId = 1
                }
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _permissionServiceMock.Setup(x => x.GetUserRoleSafelyAsync(user))
                .ReturnsAsync("Student");
            _questionRepositoryMock.Setup(x => x.IsQuestionAccessibleToUserAsync(questionId, userId, "Student"))
                .ReturnsAsync(true);
            _questionRepositoryMock.Setup(x => x.GetQuestionWithFullDetailsAsync(questionId))
                .ReturnsAsync(question);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.CannotCreateQuestionForPendingLesson));
        }

        [Test]
        public void Handle_ShouldThrowUserNotPartOfSchool_WhenUserSchoolIdMismatch()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var query = new GetQuestionDetailQuery(questionId, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                LessonMaterial = new LessonMaterial
                {
                    Status = EntityStatus.Active,
                    LessonStatus = LessonMaterialStatus.Approved,
                    SchoolId = 2 // Different school
                }
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _permissionServiceMock.Setup(x => x.GetUserRoleSafelyAsync(user))
                .ReturnsAsync("Student");
            _questionRepositoryMock.Setup(x => x.IsQuestionAccessibleToUserAsync(questionId, userId, "Student"))
                .ReturnsAsync(true);
            _questionRepositoryMock.Setup(x => x.GetQuestionWithFullDetailsAsync(questionId))
                .ReturnsAsync(question);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.UserNotPartOfSchool));
        }

        #endregion

        #region Success Response Tests

        [Test]
        public async Task Handle_ShouldReturnQuestionDetail_WhenAllValidationsPass()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var query = new GetQuestionDetailQuery(questionId, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                Title = "Test Question",
                Content = "Test Content",
                CreatedByUser = new ApplicationUser { Id = Guid.NewGuid(), FullName = "Test User" },
                LessonMaterial = new LessonMaterial
                {
                    Status = EntityStatus.Active,
                    LessonStatus = LessonMaterialStatus.Approved,
                    SchoolId = 1
                },
                Comments = []
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _permissionServiceMock.Setup(x => x.GetUserRoleSafelyAsync(user))
                .ReturnsAsync("Student");
            _questionRepositoryMock.Setup(x => x.IsQuestionAccessibleToUserAsync(questionId, userId, "Student"))
                .ReturnsAsync(true);
            _questionRepositoryMock.Setup(x => x.GetQuestionWithFullDetailsAsync(questionId))
                .ReturnsAsync(question);
            _permissionServiceMock.Setup(x => x.GetUserRoleSafelyAsync(question.CreatedByUser))
                .ReturnsAsync("Teacher");
            _permissionServiceMock.Setup(x => x.CalculateTotalCommentCount(It.IsAny<ICollection<QuestionComment>>()))
                .Returns(0);
            _permissionServiceMock.Setup(x => x.CanUserUpdateQuestion(It.IsAny<LessonMaterialQuestion>(), user, "Student"))
                .Returns(false);
            _permissionServiceMock.Setup(x => x.CanUserDeleteQuestionAsync(It.IsAny<LessonMaterialQuestion>(), user, "Student"))
                .ReturnsAsync(false);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Title, Is.EqualTo("Test Question"));
                Assert.That(result.Content, Is.EqualTo("Test Content"));
                Assert.That(result.CreatedByRole, Is.EqualTo("Teacher"));
                Assert.That(result.CanUpdate, Is.False);
                Assert.That(result.CanDelete, Is.False);
                Assert.That(result.CanComment, Is.True);
            });
        }

        #endregion

        #region Comment Structure Tests

        [Test]
        public async Task Handle_ShouldBuildCommentStructure_WhenQuestionHasComments()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var query = new GetQuestionDetailQuery(questionId, userId);
            var user = new ApplicationUser { Id = userId, SchoolId = 1 };
            var comment = new QuestionComment
            {
                Id = Guid.NewGuid(),
                Content = "Test Comment",
                CreatedByUser = new ApplicationUser { Id = Guid.NewGuid(), FullName = "Comment User" },
                Replies = []
            };
            var question = new LessonMaterialQuestion
            {
                Id = questionId,
                CreatedByUser = new ApplicationUser { Id = Guid.NewGuid(), FullName = "Test User" },
                LessonMaterial = new LessonMaterial
                {
                    Status = EntityStatus.Active,
                    LessonStatus = LessonMaterialStatus.Approved,
                    SchoolId = 1
                },
                Comments = [comment]
            };

            _userRepositoryMock.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);
            _permissionServiceMock.Setup(x => x.GetUserRoleSafelyAsync(user))
                .ReturnsAsync("Student");
            _questionRepositoryMock.Setup(x => x.IsQuestionAccessibleToUserAsync(questionId, userId, "Student"))
                .ReturnsAsync(true);
            _questionRepositoryMock.Setup(x => x.GetQuestionWithFullDetailsAsync(questionId))
                .ReturnsAsync(question);
            _permissionServiceMock.Setup(x => x.GetUserRoleSafelyAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync("Student");
            _permissionServiceMock.Setup(x => x.CalculateTotalCommentCount(It.IsAny<ICollection<QuestionComment>>()))
                .Returns(1);
            _permissionServiceMock.Setup(x => x.CanUserUpdateQuestion(It.IsAny<LessonMaterialQuestion>(), user, "Student"))
                .Returns(false);
            _permissionServiceMock.Setup(x => x.CanUserDeleteQuestionAsync(It.IsAny<LessonMaterialQuestion>(), user, "Student"))
                .ReturnsAsync(false);
            _permissionServiceMock.Setup(x => x.CanUserUpdateComment(It.IsAny<QuestionComment>(), user, "Student"))
                .Returns(false);
            _permissionServiceMock.Setup(x => x.CanUserDeleteCommentAsync(It.IsAny<QuestionComment>(), user, "Student"))
                .ReturnsAsync(false);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Comments, Has.Count.EqualTo(1));
                Assert.That(result.CommentCount, Is.EqualTo(1));
            });
        }

        #endregion
    }
}