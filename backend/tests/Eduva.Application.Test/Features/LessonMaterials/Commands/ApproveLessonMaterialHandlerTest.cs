using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Models.Notifications;
using Eduva.Application.Features.LessonMaterials.Commands.ApproveLessonMaterial;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Constants;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.LessonMaterials.Commands
{
    [TestFixture]
    public class ApproveLessonMaterialHandlerTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IHubNotificationService> _hubNotificationServiceMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
        private Mock<IGenericRepository<LessonMaterial, Guid>> _lessonRepoMock = null!;
        private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepoMock = null!;
        private Mock<IGenericRepository<LessonMaterialApproval, Guid>> _approvalRepoMock = null!;
        private ApproveLessonMaterialHandler _handler = null!;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _hubNotificationServiceMock = new Mock<IHubNotificationService>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

            _lessonRepoMock = new Mock<IGenericRepository<LessonMaterial, Guid>>();
            _userRepoMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
            _approvalRepoMock = new Mock<IGenericRepository<LessonMaterialApproval, Guid>>();

            _unitOfWorkMock.Setup(u => u.GetRepository<LessonMaterial, Guid>())
                .Returns(_lessonRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>())
                .Returns(_userRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<LessonMaterialApproval, Guid>())
                .Returns(_approvalRepoMock.Object);

            _handler = new ApproveLessonMaterialHandler(
                _unitOfWorkMock.Object,
                _userManagerMock.Object,
                _hubNotificationServiceMock.Object
            );
        }

        [Test]
        public void Handle_Should_Throw_When_Lesson_Not_Found()
        {
            var cmd = new ApproveLessonMaterialCommand
            {
                Id = Guid.NewGuid(),
                ModeratorId = Guid.NewGuid(),
                Status = LessonMaterialStatus.Approved
            };

            _lessonRepoMock.Setup(r => r.GetByIdAsync(cmd.Id)).ReturnsAsync((LessonMaterial?)null);

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public void Handle_Should_Throw_When_Moderator_Not_Found()
        {
            var lessonId = Guid.NewGuid();
            var moderatorId = Guid.NewGuid();
            var lesson = new LessonMaterial { Id = lessonId, SchoolId = 1 };

            _lessonRepoMock.Setup(r => r.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _userRepoMock.Setup(r => r.GetByIdAsync(moderatorId)).ReturnsAsync((ApplicationUser?)null);

            var cmd = new ApproveLessonMaterialCommand
            {
                Id = lessonId,
                ModeratorId = moderatorId,
                Status = LessonMaterialStatus.Approved
            };

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public void Handle_Should_Throw_When_Moderator_Not_Accessible()
        {
            var lessonId = Guid.NewGuid();
            var moderatorId = Guid.NewGuid();
            var lesson = new LessonMaterial { Id = lessonId, SchoolId = 1 };
            var moderator = new ApplicationUser { Id = moderatorId, SchoolId = 2 };

            _lessonRepoMock.Setup(r => r.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _userRepoMock.Setup(r => r.GetByIdAsync(moderatorId)).ReturnsAsync(moderator);
            _userManagerMock.Setup(m => m.GetRolesAsync(moderator)).ReturnsAsync(new List<string> { nameof(Role.ContentModerator) });

            var cmd = new ApproveLessonMaterialCommand
            {
                Id = lessonId,
                ModeratorId = moderatorId,
                Status = LessonMaterialStatus.Approved
            };

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public void Handle_Should_Throw_When_Reject_Without_Feedback()
        {
            var lessonId = Guid.NewGuid();
            var moderatorId = Guid.NewGuid();
            var lesson = new LessonMaterial { Id = lessonId, SchoolId = 1 };
            var moderator = new ApplicationUser { Id = moderatorId, SchoolId = 1 };

            _lessonRepoMock.Setup(r => r.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _userRepoMock.Setup(r => r.GetByIdAsync(moderatorId)).ReturnsAsync(moderator);
            _userManagerMock.Setup(m => m.GetRolesAsync(moderator)).ReturnsAsync(new List<string> { nameof(Role.ContentModerator) });

            var cmd = new ApproveLessonMaterialCommand
            {
                Id = lessonId,
                ModeratorId = moderatorId,
                Status = LessonMaterialStatus.Rejected,
                Feedback = ""
            };

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public void Handle_Should_Throw_When_Already_Approved()
        {
            var lessonId = Guid.NewGuid();
            var moderatorId = Guid.NewGuid();
            var lesson = new LessonMaterial { Id = lessonId, SchoolId = 1, LessonStatus = LessonMaterialStatus.Approved };
            var moderator = new ApplicationUser { Id = moderatorId, SchoolId = 1 };

            _lessonRepoMock.Setup(r => r.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _userRepoMock.Setup(r => r.GetByIdAsync(moderatorId)).ReturnsAsync(moderator);
            _userManagerMock.Setup(m => m.GetRolesAsync(moderator)).ReturnsAsync(new List<string> { nameof(Role.ContentModerator) });

            var cmd = new ApproveLessonMaterialCommand
            {
                Id = lessonId,
                ModeratorId = moderatorId,
                Status = LessonMaterialStatus.Approved
            };

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public void Handle_Should_Throw_When_Already_Rejected()
        {
            var lessonId = Guid.NewGuid();
            var moderatorId = Guid.NewGuid();
            var lesson = new LessonMaterial { Id = lessonId, SchoolId = 1, LessonStatus = LessonMaterialStatus.Rejected };
            var moderator = new ApplicationUser { Id = moderatorId, SchoolId = 1 };

            _lessonRepoMock.Setup(r => r.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _userRepoMock.Setup(r => r.GetByIdAsync(moderatorId)).ReturnsAsync(moderator);
            _userManagerMock.Setup(m => m.GetRolesAsync(moderator)).ReturnsAsync(new List<string> { nameof(Role.ContentModerator) });

            var cmd = new ApproveLessonMaterialCommand
            {
                Id = lessonId,
                ModeratorId = moderatorId,
                Status = LessonMaterialStatus.Rejected,
                Feedback = "Reason"
            };

            Assert.ThrowsAsync<AppException>(() => _handler.Handle(cmd, CancellationToken.None));
        }

        [Test]
        public async Task Handle_Should_Approve_LessonMaterial_For_Moderator()
        {
            var lessonId = Guid.NewGuid();
            var moderatorId = Guid.NewGuid();
            var createdByUserId = Guid.NewGuid();
            var lesson = new LessonMaterial { Id = lessonId, SchoolId = 1, LessonStatus = LessonMaterialStatus.Pending, Title = "Title", CreatedByUserId = createdByUserId };
            var moderator = new ApplicationUser { Id = moderatorId, SchoolId = 1 };

            _lessonRepoMock.Setup(r => r.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _userRepoMock.Setup(r => r.GetByIdAsync(moderatorId)).ReturnsAsync(moderator);
            _userManagerMock.Setup(m => m.GetRolesAsync(moderator)).ReturnsAsync(new List<string> { nameof(Role.ContentModerator) });

            _approvalRepoMock.Setup(r => r.AddAsync(It.IsAny<LessonMaterialApproval>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).Returns(Task.FromResult(1));
            _hubNotificationServiceMock.Setup(h => h.NotifyLessonMaterialApprovalAsync(
                It.IsAny<LessonMaterialApprovalNotification>(),
                NotificationTypes.LessonMaterialApproved,
                createdByUserId,
                moderator)).Returns(Task.CompletedTask);

            var cmd = new ApproveLessonMaterialCommand
            {
                Id = lessonId,
                ModeratorId = moderatorId,
                Status = LessonMaterialStatus.Approved
            };

            var result = await _handler.Handle(cmd, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo(Unit.Value));
                Assert.That(lesson.LessonStatus, Is.EqualTo(LessonMaterialStatus.Approved));
            });
        }

        [Test]
        public async Task Handle_Should_Reject_LessonMaterial_For_SchoolAdmin()
        {
            var lessonId = Guid.NewGuid();
            var moderatorId = Guid.NewGuid();
            var createdByUserId = Guid.NewGuid();
            var lesson = new LessonMaterial { Id = lessonId, SchoolId = 1, LessonStatus = LessonMaterialStatus.Pending, Title = "Title", CreatedByUserId = createdByUserId };
            var moderator = new ApplicationUser { Id = moderatorId, SchoolId = 1 };

            _lessonRepoMock.Setup(r => r.GetByIdAsync(lessonId)).ReturnsAsync(lesson);
            _userRepoMock.Setup(r => r.GetByIdAsync(moderatorId)).ReturnsAsync(moderator);
            _userManagerMock.Setup(m => m.GetRolesAsync(moderator)).ReturnsAsync(new List<string> { nameof(Role.SchoolAdmin) });

            _approvalRepoMock.Setup(r => r.AddAsync(It.IsAny<LessonMaterialApproval>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(u => u.CommitAsync()).Returns(Task.FromResult(1));
            _hubNotificationServiceMock.Setup(h => h.NotifyLessonMaterialApprovalAsync(
                It.IsAny<LessonMaterialApprovalNotification>(),
                NotificationTypes.LessonMaterialRejected,
                createdByUserId,
                moderator)).Returns(Task.CompletedTask);

            var cmd = new ApproveLessonMaterialCommand
            {
                Id = lessonId,
                ModeratorId = moderatorId,
                Status = LessonMaterialStatus.Rejected,
                Feedback = "Reason"
            };

            var result = await _handler.Handle(cmd, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo(Unit.Value));
                Assert.That(lesson.LessonStatus, Is.EqualTo(LessonMaterialStatus.Rejected));
            });
        }
    }
}