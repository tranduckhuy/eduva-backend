using Eduva.Application.Common.Mappings;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Features.Users.Queries;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Eduva.Application.Test.Features.Users.Queries;

[TestFixture]
public class GetUserProfileHandlerTests
{
    private Mock<IUserRepository> _userRepositoryMock = default!;
    private Mock<UserManager<ApplicationUser>> _userManagerMock = default!;
    private Mock<ISchoolSubscriptionService> _schoolSubscriptionServiceMock = default!;
    private GetUserProfileHandler _handler = default!;

    #region GetUserProfileHandlerTests Setup

    [SetUp]
    public void Setup()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _userManagerMock = MockUserManager<ApplicationUser>();
        _schoolSubscriptionServiceMock = new Mock<ISchoolSubscriptionService>();

        _handler = new GetUserProfileHandler(
            _userRepositoryMock.Object,
            _userManagerMock.Object,
            _schoolSubscriptionServiceMock.Object
        );

        _ = AppMapper.Mapper; // ensure mapper is initialized
    }

    #endregion

    #region GetUserProfileHandler Tests

    [Test]
    public async Task Handle_ShouldReturnUserResponse_WhenUserExists()
    {
        var userId = Guid.NewGuid();
        var query = new GetUserProfileQuery(userId);

        var user = new ApplicationUser
        {
            Id = userId,
            FullName = "Test User",
            Email = "test@example.com",
            PhoneNumber = "1234567890",
            AvatarUrl = "https://example.com/avatar.jpg",
            SchoolId = 1,
            School = new School { Id = 1, Name = "Test School" }
        };

        var roles = new List<string> { "Student", "User" };

        _userRepositoryMock.Setup(r => r.GetByIdWithSchoolAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(roles);
        _userManagerMock.Setup(x => x.GetTwoFactorEnabledAsync(user)).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(true);

        _schoolSubscriptionServiceMock.Setup(x => x.GetUserSubscriptionStatusAsync(userId))
            .ReturnsAsync((true, DateTimeOffset.UtcNow.AddDays(30)));

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(user.Id));
            Assert.That(result.Email, Is.EqualTo(user.Email));
            Assert.That(result.FullName, Is.EqualTo(user.FullName));
            Assert.That(result.School, Is.Not.Null);
            Assert.That(result.School!.Id, Is.EqualTo(user.SchoolId));
            Assert.That(result.Roles, Is.EqualTo(roles));
            Assert.That(result.Is2FAEnabled, Is.True);
            Assert.That(result.IsEmailConfirmed, Is.True);
        });
    }

    [Test]
    public void Handle_ShouldThrowUserNotExistsException_WhenUserNotFound()
    {
        var userId = Guid.NewGuid();
        var query = new GetUserProfileQuery(userId);

        _userRepositoryMock.Setup(r => r.GetByIdWithSchoolAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        var ex = Assert.ThrowsAsync<UserNotExistsException>(() =>
            _handler.Handle(query, CancellationToken.None));

        Assert.That(ex, Is.Not.Null);
        _userManagerMock.Verify(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Test]
    public async Task Handle_ShouldReturnUserResponseWithEmptyRoles_WhenNoRoles()
    {
        var userId = Guid.NewGuid();
        var query = new GetUserProfileQuery(userId);

        var user = new ApplicationUser
        {
            Id = userId,
            Email = "nobody@example.com",
            FullName = "Anonymous",
            SchoolId = null
        };

        _userRepositoryMock.Setup(r => r.GetByIdWithSchoolAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string>());
        _userManagerMock.Setup(x => x.GetTwoFactorEnabledAsync(user)).ReturnsAsync(false);
        _userManagerMock.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(false);
        _schoolSubscriptionServiceMock.Setup(x => x.GetUserSubscriptionStatusAsync(userId))
            .ReturnsAsync((false, DateTimeOffset.UtcNow));

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Roles, Is.Empty);
            Assert.That(result.School, Is.Null);
        });
    }

    [Test]
    public async Task Handle_ShouldHandleNullFields()
    {
        var userId = Guid.NewGuid();
        var query = new GetUserProfileQuery(userId);

        var user = new ApplicationUser
        {
            Id = userId,
            Email = "test@example.com",
            FullName = null,
            PhoneNumber = null,
            AvatarUrl = null!,
            SchoolId = null
        };

        var roles = new List<string> { "User" };

        _userRepositoryMock.Setup(r => r.GetByIdWithSchoolAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(roles);
        _userManagerMock.Setup(x => x.GetTwoFactorEnabledAsync(user)).ReturnsAsync(false);
        _userManagerMock.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
        _schoolSubscriptionServiceMock.Setup(x => x.GetUserSubscriptionStatusAsync(userId))
            .ReturnsAsync((true, DateTimeOffset.UtcNow.AddDays(10)));

        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Email, Is.EqualTo(user.Email));
            Assert.That(result.FullName, Is.Null);
            Assert.That(result.PhoneNumber, Is.Null);
            Assert.That(result.AvatarUrl, Is.Null.Or.Empty);
            Assert.That(result.School, Is.Null);
            Assert.That(result.Roles, Is.EqualTo(roles));
        });
    }

    #endregion

    #region Method Helpers

    private static Mock<UserManager<T>> MockUserManager<T>() where T : class
    {
        var store = new Mock<IUserStore<T>>();
        return new Mock<UserManager<T>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    #endregion

}