using Eduva.Application.Common.Mappings;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Features.Users.Queries;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.Users.Queries
{
    [TestFixture]
    public class GetUserProfileHandlerTests
    {
        private Mock<UserManager<ApplicationUser>> _userManagerMock = default!;
        private Mock<ISchoolSubscriptionService> _schoolSubscriptionServiceMock = default!;
        private GetUserProfileHandler _handler = default!;

        #region GetUserProfileHandler Setup

        [SetUp]
        public void Setup()
        {
            _userManagerMock = MockUserManager<ApplicationUser>();
            _schoolSubscriptionServiceMock = new Mock<ISchoolSubscriptionService>();
            _handler = new GetUserProfileHandler(_userManagerMock.Object, _schoolSubscriptionServiceMock.Object);

            // Initialize AutoMapper
            _ = AppMapper.Mapper;
        }

        #endregion

        #region GetUserProfileHandler Tests

        [Test]
        public async Task Handle_ShouldReturnUserResponse_WhenUserExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = new GetUserProfileQuery(userId);

            var user = new ApplicationUser
            {
                Id = userId,
                FullName = "Test User",
                Email = "test@example.com",
                PhoneNumber = "1234567890",
                AvatarUrl = "https://example.com/avatar.jpg",
                SchoolId = 1
            };

            var roles = new List<string> { "Student", "User" };

            _userManagerMock
                .Setup(um => um.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(roles);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(user.Id));
                Assert.That(result.FullName, Is.EqualTo(user.FullName));
                Assert.That(result.Email, Is.EqualTo(user.Email));
                Assert.That(result.PhoneNumber, Is.EqualTo(user.PhoneNumber));
                Assert.That(result.AvatarUrl, Is.EqualTo(user.AvatarUrl));
                Assert.That(result.SchoolId, Is.EqualTo(user.SchoolId));
                Assert.That(result.Roles, Is.EqualTo(roles));
            });

            _userManagerMock.Verify(um => um.FindByIdAsync(userId.ToString()), Times.Once);
            _userManagerMock.Verify(um => um.GetRolesAsync(user), Times.Once);
        }
        [Test]
        public void Handle_ShouldThrowUserNotExistsException_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = new GetUserProfileQuery(userId);

            _userManagerMock
                .Setup(um => um.FindByIdAsync(userId.ToString()))
                .ReturnsAsync((ApplicationUser?)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<UserNotExistsException>(
                () => _handler.Handle(query, CancellationToken.None));

            Assert.That(exception, Is.Not.Null);
            _userManagerMock.Verify(um => um.FindByIdAsync(userId.ToString()), Times.Once);
            _userManagerMock.Verify(um => um.GetRolesAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Test]
        public async Task Handle_ShouldReturnUserResponseWithEmptyRoles_WhenUserHasNoRoles()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = new GetUserProfileQuery(userId);

            var user = new ApplicationUser
            {
                Id = userId,
                FullName = "Test User",
                Email = "test@example.com",
                PhoneNumber = "1234567890",
                AvatarUrl = "https://example.com/avatar.jpg",
                SchoolId = null
            };

            var roles = new List<string>();

            _userManagerMock
                .Setup(um => um.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(roles);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(user.Id));
                Assert.That(result.FullName, Is.EqualTo(user.FullName));
                Assert.That(result.Email, Is.EqualTo(user.Email));
                Assert.That(result.PhoneNumber, Is.EqualTo(user.PhoneNumber));
                Assert.That(result.AvatarUrl, Is.EqualTo(user.AvatarUrl));
                Assert.That(result.SchoolId, Is.EqualTo(user.SchoolId));
                Assert.That(result.Roles, Is.Empty);
            });
        }

        [Test]
        public async Task Handle_ShouldHandleNullProperties_WhenUserHasNullValues()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = new GetUserProfileQuery(userId);

            var user = new ApplicationUser
            {
                Id = userId,
                FullName = null,
                Email = "test@example.com",
                PhoneNumber = null,
                AvatarUrl = string.Empty,
                SchoolId = null
            };

            var roles = new List<string> { "User" };

            _userManagerMock
                .Setup(um => um.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(roles);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(user.Id));
                Assert.That(result.Email, Is.EqualTo(user.Email));
                Assert.That(result.Roles, Is.EqualTo(roles));
            });
        }

        #endregion

        #region Helper Methods

        private static Mock<UserManager<T>> MockUserManager<T>() where T : class
        {
            var store = new Mock<IUserStore<T>>();
            return new Mock<UserManager<T>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        #endregion
    }
}
