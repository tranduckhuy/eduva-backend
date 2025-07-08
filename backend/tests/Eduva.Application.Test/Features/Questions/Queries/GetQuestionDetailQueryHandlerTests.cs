using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Questions.Queries;
using Eduva.Application.Features.Questions.Responses;
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

        #region Response Classes Tests

        [Test]
        public void QuestionReplyResponse_ShouldInitializeWithDefaultValues()
        {
            // Act
            var response = new QuestionReplyResponse();

            // Assert
            Assert.That(response.Id, Is.EqualTo(Guid.Empty));
            Assert.That(response.Content, Is.EqualTo(default(string)));
            Assert.That(response.CreatedAt, Is.EqualTo(default(DateTimeOffset)));
            Assert.That(response.LastModifiedAt, Is.Null);
            Assert.That(response.CreatedByUserId, Is.EqualTo(Guid.Empty));
            Assert.That(response.CreatedByName, Is.Null);
            Assert.That(response.CreatedByAvatar, Is.Null);
            Assert.That(response.CreatedByRole, Is.Null);
            Assert.That(response.CanUpdate, Is.False);
            Assert.That(response.CanDelete, Is.False);
            Assert.That(response.ParentCommentId, Is.EqualTo(Guid.Empty));
        }

        [Test]
        public void QuestionReplyResponse_ShouldSetAllProperties()
        {
            // Arrange
            var id = Guid.NewGuid();
            var createdByUserId = Guid.NewGuid();
            var parentCommentId = Guid.NewGuid();
            var createdAt = DateTimeOffset.Now;
            var lastModifiedAt = DateTimeOffset.Now.AddMinutes(30);

            // Act
            var response = new QuestionReplyResponse
            {
                Id = id,
                Content = "Test Reply Content",
                CreatedAt = createdAt,
                LastModifiedAt = lastModifiedAt,
                CreatedByUserId = createdByUserId,
                CreatedByName = "Reply Author",
                CreatedByAvatar = "reply-avatar-url",
                CreatedByRole = "Student",
                CanUpdate = true,
                CanDelete = true,
                ParentCommentId = parentCommentId
            };

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(response.Id, Is.EqualTo(id));
                Assert.That(response.Content, Is.EqualTo("Test Reply Content"));
                Assert.That(response.CreatedAt, Is.EqualTo(createdAt));
                Assert.That(response.LastModifiedAt, Is.EqualTo(lastModifiedAt));
                Assert.That(response.CreatedByUserId, Is.EqualTo(createdByUserId));
                Assert.That(response.CreatedByName, Is.EqualTo("Reply Author"));
                Assert.That(response.CreatedByAvatar, Is.EqualTo("reply-avatar-url"));
                Assert.That(response.CreatedByRole, Is.EqualTo("Student"));
                Assert.That(response.CanUpdate, Is.True);
                Assert.That(response.CanDelete, Is.True);
                Assert.That(response.ParentCommentId, Is.EqualTo(parentCommentId));
            });
        }

        [Test]
        public void QuestionReplyResponse_ShouldSetNullablePropertiesToNull()
        {
            // Act
            var response = new QuestionReplyResponse
            {
                Id = Guid.NewGuid(),
                Content = "Content only",
                CreatedByName = null,
                CreatedByAvatar = null,
                CreatedByRole = null,
                LastModifiedAt = null
            };

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(response.CreatedByName, Is.Null);
                Assert.That(response.CreatedByAvatar, Is.Null);
                Assert.That(response.CreatedByRole, Is.Null);
                Assert.That(response.LastModifiedAt, Is.Null);
                Assert.That(response.Content, Is.EqualTo("Content only"));
            });
        }

        [Test]
        public void QuestionCommentResponse_ShouldInitializeWithDefaultValues()
        {
            // Act
            var response = new QuestionCommentResponse();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(response.Id, Is.EqualTo(Guid.Empty));
                Assert.That(response.QuestionId, Is.EqualTo(Guid.Empty));
                Assert.That(response.Content, Is.EqualTo(default(string))); // Changed from Is.Not.Null to default(string) which is null
                Assert.That(response.CreatedAt, Is.EqualTo(default(DateTimeOffset)));
                Assert.That(response.LastModifiedAt, Is.Null);
                Assert.That(response.CreatedByUserId, Is.EqualTo(Guid.Empty));
                Assert.That(response.CreatedByName, Is.Null);
                Assert.That(response.CreatedByAvatar, Is.Null);
                Assert.That(response.CreatedByRole, Is.Null);
                Assert.That(response.CanUpdate, Is.False);
                Assert.That(response.CanDelete, Is.False);
                Assert.That(response.ParentCommentId, Is.Null);
                Assert.That(response.Replies, Is.Not.Null); // This stays the same because it's initialized with = []
                Assert.That(response.Replies, Is.Empty);
                Assert.That(response.ReplyCount, Is.EqualTo(0));
            });
        }

        [Test]
        public void QuestionCommentResponse_ShouldSetRepliesList()
        {
            // Arrange
            var replies = new List<QuestionReplyResponse>
            {
                new QuestionReplyResponse
                {
                    Id = Guid.NewGuid(),
                    Content = "Reply 1",
                    ParentCommentId = Guid.NewGuid()
                },
                new QuestionReplyResponse
                {
                    Id = Guid.NewGuid(),
                    Content = "Reply 2",
                    ParentCommentId = Guid.NewGuid()
                }
            };

            // Act
            var response = new QuestionCommentResponse
            {
                Replies = replies,
                ReplyCount = 2
            };

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(response.Replies, Is.EqualTo(replies));
                Assert.That(response.Replies.Count, Is.EqualTo(2));
                Assert.That(response.ReplyCount, Is.EqualTo(2));
                Assert.That(response.Replies[0].Content, Is.EqualTo("Reply 1"));
                Assert.That(response.Replies[1].Content, Is.EqualTo("Reply 2"));
            });
        }

        [Test]
        public void QuestionDetailResponse_ShouldInheritFromQuestionResponse()
        {
            // Act
            var response = new QuestionDetailResponse();

            // Assert
            Assert.That(response, Is.InstanceOf<QuestionResponse>());
            Assert.That(response, Is.InstanceOf<QuestionDetailResponse>());
        }

        [Test]
        public void QuestionDetailResponse_ShouldInitializeCommentsCollection()
        {
            // Act
            var response = new QuestionDetailResponse();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(response.Comments, Is.Not.Null);
                Assert.That(response.Comments, Is.Empty);
                Assert.That(response.CanUpdate, Is.False);
                Assert.That(response.CanDelete, Is.False);
                Assert.That(response.CanComment, Is.False);
            });
        }

        [Test]
        public void QuestionDetailResponse_ShouldSetPermissionFlags()
        {
            // Act
            var response = new QuestionDetailResponse
            {
                CanUpdate = true,
                CanDelete = true,
                CanComment = true
            };

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(response.CanUpdate, Is.True);
                Assert.That(response.CanDelete, Is.True);
                Assert.That(response.CanComment, Is.True);
            });
        }

        #endregion

    }
}