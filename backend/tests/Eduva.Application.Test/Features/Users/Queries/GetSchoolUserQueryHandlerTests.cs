using Eduva.Application.Common.Exceptions;
using Eduva.Application.Common.Mappings;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Features.Users.Queries;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.Users.Queries
{
    [TestFixture]
    public class GetSchoolUserQueryHandlerTests
    {
        #region Setup

        private Mock<IUserRepository> _userRepository = null!;
        private Mock<UserManager<ApplicationUser>> _userManager = null!;
        private Mock<ISchoolSubscriptionService> _schoolSubscriptionService = null!;
        private GetSchoolUserQueryHandler _handler = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Force AutoMapper initialization
            _ = AppMapper.Mapper;
        }

        [SetUp]
        public void Setup()
        {
            _userRepository = new Mock<IUserRepository>();

            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManager = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _schoolSubscriptionService = new Mock<ISchoolSubscriptionService>();

            _handler = new GetSchoolUserQueryHandler(
                _userRepository.Object,
                _userManager.Object,
                _schoolSubscriptionService.Object);
        }

        #endregion

        #region Test Cases

        [Test]
        public void Handle_SchoolAdminNotInSchool_ThrowsAppException()
        {
            // Arrange
            var schoolAdminId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var query = new GetSchoolUserQuery(targetUserId, schoolAdminId);

            _userRepository.Setup(x => x.GetByIdWithSchoolAsync(schoolAdminId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ApplicationUser { Id = schoolAdminId });

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.UserNotPartOfSchool));
        }

        [Test]
        public void Handle_TargetUserNotFound_ThrowsUserNotExistsException()
        {
            // Arrange
            var schoolAdminId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var query = new GetSchoolUserQuery(targetUserId, schoolAdminId);

            // Setup schoolAdmin with valid SchoolId
            var schoolAdmin = new ApplicationUser
            {
                Id = schoolAdminId,
                SchoolId = 1,
                Email = "admin@school.com",
                UserName = "admin@school.com",
                FullName = "Admin User"
            };

            _userRepository.Setup(x => x.GetByIdWithSchoolAsync(schoolAdminId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(schoolAdmin);

            // Setup targetUser as null
            _userRepository.Setup(x => x.GetByIdWithSchoolAsync(targetUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ApplicationUser?)null);

            // Act & Assert
            Assert.ThrowsAsync<UserNotExistsException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
        }

        [Test]
        public void Handle_DifferentSchools_ThrowsAppException()
        {
            // Arrange
            var schoolAdminId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var query = new GetSchoolUserQuery(targetUserId, schoolAdminId);

            var schoolAdmin = new ApplicationUser { Id = schoolAdminId, SchoolId = 1 };
            var targetUser = new ApplicationUser { Id = targetUserId, SchoolId = 2 };

            _userRepository.Setup(x => x.GetByIdWithSchoolAsync(schoolAdminId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(schoolAdmin);

            _userRepository.Setup(x => x.GetByIdWithSchoolAsync(targetUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(targetUser);

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.CannotViewUserFromDifferentSchool));
        }

        [Test]
        public void Handle_RestrictedUserRole_ThrowsAppException()
        {
            // Arrange
            var schoolAdminId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var query = new GetSchoolUserQuery(targetUserId, schoolAdminId);

            var schoolAdmin = new ApplicationUser { Id = schoolAdminId, SchoolId = 1 };
            var targetUser = new ApplicationUser { Id = targetUserId, SchoolId = 1 };

            _userRepository.Setup(x => x.GetByIdWithSchoolAsync(schoolAdminId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(schoolAdmin);

            _userRepository.Setup(x => x.GetByIdWithSchoolAsync(targetUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(targetUser);

            _userManager.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { nameof(Role.SchoolAdmin) });

            // Act & Assert
            var ex = Assert.ThrowsAsync<AppException>(async () =>
                await _handler.Handle(query, CancellationToken.None));
            Assert.That(ex.StatusCode, Is.EqualTo(CustomCode.CannotViewRestrictedUserRoles));
        }

        [Test]
        public async Task Handle_ValidRequest_ReturnsUserResponse()
        {
            // Arrange
            var schoolAdminId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var query = new GetSchoolUserQuery(targetUserId, schoolAdminId);

            var schoolAdmin = new ApplicationUser { Id = schoolAdminId, SchoolId = 1 };
            var targetUser = new ApplicationUser
            {
                Id = targetUserId,
                SchoolId = 1,
                Email = "test@test.com",
                UserName = "test@test.com",
                FullName = "Test User",
                TotalCredits = 100,
                PhoneNumber = "1234567890",
                AvatarUrl = "avatar.jpg",
                Status = EntityStatus.Active
            };

            _userRepository.Setup(x => x.GetByIdWithSchoolAsync(schoolAdminId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(schoolAdmin);

            _userRepository.Setup(x => x.GetByIdWithSchoolAsync(targetUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(targetUser);

            _userManager.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { nameof(Role.Student) });

            _userManager.Setup(x => x.GetTwoFactorEnabledAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(true);

            _userManager.Setup(x => x.IsEmailConfirmedAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(true);

            _schoolSubscriptionService.Setup(x => x.GetUserSubscriptionStatusAsync(targetUserId))
                .ReturnsAsync((true, DateTime.UtcNow.AddDays(30)));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(targetUserId));
                Assert.That(result.Email, Is.EqualTo("test@test.com"));
                Assert.That(result.FullName, Is.EqualTo("Test User"));
                Assert.That(result.Is2FAEnabled, Is.True);
                Assert.That(result.IsEmailConfirmed, Is.True);
                Assert.That(result.UserSubscriptionResponse!.IsSubscriptionActive, Is.True);
                Assert.That(result.Roles, Contains.Item(nameof(Role.Student)));
                Assert.That(result.CreditBalance, Is.EqualTo(100));
            });
        }

        #endregion
    }
}