using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Features.Schools.Queries;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.Schools.Queries
{
    [TestFixture]
    public class GetUserByIdForSchoolAdminQueryHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = default!;
        private Mock<IUserRepository> _userRepositoryMock = default!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = default!;
        private Mock<ISchoolSubscriptionService> _schoolSubscriptionServiceMock = default!;
        private GetUserByIdForSchoolAdminQueryHandler _handler = default!;

        #region Setup

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _userManagerMock = MockUserManager<ApplicationUser>();
            _schoolSubscriptionServiceMock = new Mock<ISchoolSubscriptionService>();

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<IUserRepository>())
                .Returns(_userRepositoryMock.Object);

            _handler = new GetUserByIdForSchoolAdminQueryHandler(
                _unitOfWorkMock.Object,
                _userManagerMock.Object,
                _schoolSubscriptionServiceMock.Object
            );

            // Ensure AppMapper is initialized
            _ = AppMapper<AppMappingProfile>.Mapper;
        }

        private static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class
        {
            var store = new Mock<IUserStore<TUser>>();
            var mgr = new Mock<UserManager<TUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            mgr.Object.UserValidators.Add(new UserValidator<TUser>());
            mgr.Object.PasswordValidators.Add(new PasswordValidator<TUser>());
            return mgr;
        }

        #endregion

        #region Tests

        [Test]
        public async Task Handle_ShouldReturnUserResponse_WhenAllConditionsAreMet()
        {
            // Arrange
            var requesterId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var schoolId = 1;

            var requester = new ApplicationUser
            {
                Id = requesterId,
                SchoolId = schoolId,
                Email = "admin@school.com"
            };

            var targetUser = new ApplicationUser
            {
                Id = targetUserId,
                SchoolId = schoolId,
                Email = "user@school.com",
                FullName = "Target User",
                School = new School { Id = schoolId, Name = "Test School" }
            };

            var requesterRoles = new List<string> { nameof(Role.SchoolAdmin) };
            var targetUserRoles = new List<string> { nameof(Role.Student) };

            _userRepositoryMock.Setup(r => r.GetByIdAsync(requesterId))
                .ReturnsAsync(requester);

            _userRepositoryMock.Setup(r => r.GetByIdWithSchoolAsync(targetUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(targetUser);

            _userManagerMock.Setup(m => m.GetRolesAsync(requester))
                .ReturnsAsync(requesterRoles);

            _userManagerMock.Setup(m => m.GetRolesAsync(targetUser))
                .ReturnsAsync(targetUserRoles);

            _userManagerMock.Setup(m => m.GetTwoFactorEnabledAsync(targetUser))
                .ReturnsAsync(true);

            _userManagerMock.Setup(m => m.IsEmailConfirmedAsync(targetUser))
                .ReturnsAsync(true);

            _schoolSubscriptionServiceMock.Setup(s => s.GetUserSubscriptionStatusAsync(targetUserId))
                .ReturnsAsync((true, DateTimeOffset.UtcNow.AddDays(30)));

            var query = new GetUserByIdForSchoolAdminQuery(requesterId, targetUserId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(targetUserId));
                Assert.That(result.Email, Is.EqualTo(targetUser.Email));
                Assert.That(result.FullName, Is.EqualTo(targetUser.FullName));
                Assert.That(result.Roles, Is.EqualTo(targetUserRoles));
                Assert.That(result.Is2FAEnabled, Is.True);
                Assert.That(result.IsEmailConfirmed, Is.True);
                Assert.That(result.UserSubscriptionResponse, Is.Not.Null);
                Assert.That(result.UserSubscriptionResponse!.IsSubscriptionActive, Is.True);
            });
        }

        [Test]
        public void Handle_ShouldThrowUserNotExistsException_WhenRequesterNotFound()
        {
            // Arrange
            var requesterId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();

            _userRepositoryMock.Setup(r => r.GetByIdAsync(requesterId))
                .ReturnsAsync((ApplicationUser?)null);

            var query = new GetUserByIdForSchoolAdminQuery(requesterId, targetUserId);

            // Act & Assert
            Assert.ThrowsAsync<UserNotExistsException>(() =>
                _handler.Handle(query, CancellationToken.None));

            // Verify target user lookup was not called
            _userRepositoryMock.Verify(r => r.GetByIdWithSchoolAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public void Handle_ShouldThrowUserNotExistsException_WhenTargetUserNotFound()
        {
            // Arrange
            var requesterId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();

            var requester = new ApplicationUser
            {
                Id = requesterId,
                SchoolId = 1
            };

            _userRepositoryMock.Setup(r => r.GetByIdAsync(requesterId))
                .ReturnsAsync(requester);

            _userRepositoryMock.Setup(r => r.GetByIdWithSchoolAsync(targetUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ApplicationUser?)null);

            var query = new GetUserByIdForSchoolAdminQuery(requesterId, targetUserId);

            // Act & Assert
            Assert.ThrowsAsync<UserNotExistsException>(() =>
                _handler.Handle(query, CancellationToken.None));

            // Verify role check was not called
            _userManagerMock.Verify(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Test]
        public void Handle_ShouldThrowAppException_WhenRequesterIsNotSchoolAdmin()
        {
            // Arrange
            var requesterId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var schoolId = 1;

            var requester = new ApplicationUser
            {
                Id = requesterId,
                SchoolId = schoolId
            };

            var targetUser = new ApplicationUser
            {
                Id = targetUserId,
                SchoolId = schoolId
            };

            var requesterRoles = new List<string> { nameof(Role.Teacher) }; // Not SchoolAdmin

            _userRepositoryMock.Setup(r => r.GetByIdAsync(requesterId))
                .ReturnsAsync(requester);

            _userRepositoryMock.Setup(r => r.GetByIdWithSchoolAsync(targetUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(targetUser);

            _userManagerMock.Setup(m => m.GetRolesAsync(requester))
                .ReturnsAsync(requesterRoles);

            var query = new GetUserByIdForSchoolAdminQuery(requesterId, targetUserId);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(() =>
                _handler.Handle(query, CancellationToken.None));

            Assert.That(exception!.StatusCode, Is.EqualTo(CustomCode.Forbidden));
        }

        [Test]
        public void Handle_ShouldThrowUserNotPartOfSchoolException_WhenRequesterHasNoSchool()
        {
            // Arrange
            var requesterId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();

            var requester = new ApplicationUser
            {
                Id = requesterId,
                SchoolId = null // No school
            };

            var targetUser = new ApplicationUser
            {
                Id = targetUserId,
                SchoolId = 1
            };

            var requesterRoles = new List<string> { nameof(Role.SchoolAdmin) };

            _userRepositoryMock.Setup(r => r.GetByIdAsync(requesterId))
                .ReturnsAsync(requester);

            _userRepositoryMock.Setup(r => r.GetByIdWithSchoolAsync(targetUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(targetUser);

            _userManagerMock.Setup(m => m.GetRolesAsync(requester))
                .ReturnsAsync(requesterRoles);

            var query = new GetUserByIdForSchoolAdminQuery(requesterId, targetUserId);

            // Act & Assert
            Assert.ThrowsAsync<UserNotPartOfSchoolException>(() =>
                _handler.Handle(query, CancellationToken.None));
        }

        [Test]
        public void Handle_ShouldThrowAppException_WhenUsersFromDifferentSchools()
        {
            // Arrange
            var requesterId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();

            var requester = new ApplicationUser
            {
                Id = requesterId,
                SchoolId = 1 // School 1
            };

            var targetUser = new ApplicationUser
            {
                Id = targetUserId,
                SchoolId = 2 // School 2 (different)
            };

            var requesterRoles = new List<string> { nameof(Role.SchoolAdmin) };

            _userRepositoryMock.Setup(r => r.GetByIdAsync(requesterId))
                .ReturnsAsync(requester);

            _userRepositoryMock.Setup(r => r.GetByIdWithSchoolAsync(targetUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(targetUser);

            _userManagerMock.Setup(m => m.GetRolesAsync(requester))
                .ReturnsAsync(requesterRoles);

            var query = new GetUserByIdForSchoolAdminQuery(requesterId, targetUserId);

            // Act & Assert
            var exception = Assert.ThrowsAsync<AppException>(() =>
                _handler.Handle(query, CancellationToken.None));

            Assert.That(exception!.StatusCode, Is.EqualTo(CustomCode.Forbidden));
        }

        [Test]
        public async Task Handle_ShouldSetCorrectSubscriptionInfo_WhenSubscriptionServiceReturnsData()
        {
            // Arrange
            var requesterId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var schoolId = 1;
            var subscriptionEndDate = DateTimeOffset.UtcNow.AddDays(15);

            var requester = new ApplicationUser
            {
                Id = requesterId,
                SchoolId = schoolId
            };

            var targetUser = new ApplicationUser
            {
                Id = targetUserId,
                SchoolId = schoolId,
                Email = "user@test.com"
            };

            var requesterRoles = new List<string> { nameof(Role.SchoolAdmin) };
            var targetUserRoles = new List<string> { nameof(Role.Student) };

            _userRepositoryMock.Setup(r => r.GetByIdAsync(requesterId))
                .ReturnsAsync(requester);

            _userRepositoryMock.Setup(r => r.GetByIdWithSchoolAsync(targetUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(targetUser);

            _userManagerMock.Setup(m => m.GetRolesAsync(requester))
                .ReturnsAsync(requesterRoles);

            _userManagerMock.Setup(m => m.GetRolesAsync(targetUser))
                .ReturnsAsync(targetUserRoles);

            _userManagerMock.Setup(m => m.GetTwoFactorEnabledAsync(targetUser))
                .ReturnsAsync(false);

            _userManagerMock.Setup(m => m.IsEmailConfirmedAsync(targetUser))
                .ReturnsAsync(false);

            _schoolSubscriptionServiceMock.Setup(s => s.GetUserSubscriptionStatusAsync(targetUserId))
                .ReturnsAsync((false, subscriptionEndDate)); // Inactive subscription

            var query = new GetUserByIdForSchoolAdminQuery(requesterId, targetUserId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result.UserSubscriptionResponse, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.UserSubscriptionResponse!.IsSubscriptionActive, Is.False);
                Assert.That(result.UserSubscriptionResponse.SubscriptionEndDate, Is.EqualTo(subscriptionEndDate));
                Assert.That(result.Is2FAEnabled, Is.False);
                Assert.That(result.IsEmailConfirmed, Is.False);
            });
        }

        #endregion
    }
}