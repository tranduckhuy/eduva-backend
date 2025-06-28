using Eduva.Application.Common.Exceptions;
using Eduva.Application.Features.Users.Commands;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Eduva.Application.Test.Features.Users.Commands;

[TestFixture]
public class DeleteUserCommandHandlerTests
{
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<UserManager<ApplicationUser>> _userManagerMock = default!;
    private DeleteUserCommandHandler _handler = default!;

    private static ApplicationUser CreateUser(Guid id, EntityStatus status)
        => new ApplicationUser { Id = id, Status = status };

    #region DeleteUserCommandHandlerTests Setup

    [SetUp]
    public void Setup()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        var storeMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            storeMock.Object,
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<ApplicationUser>>(),
            new List<IUserValidator<ApplicationUser>>(),
            new List<IPasswordValidator<ApplicationUser>>(),
            Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<ApplicationUser>>>()
        );

        _handler = new DeleteUserCommandHandler(_unitOfWorkMock.Object, _userManagerMock.Object);
    }

    #endregion

    #region DeleteUserCommandHandler Tests

    [Test]
    public void Should_Throw_When_Deleting_Self()
    {
        var userId = Guid.NewGuid();
        var command = new DeleteUserCommand(userId, userId);

        var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.CannotDeleteYourOwnAccount));
    }

    [Test]
    public void Should_Throw_When_User_Not_Found()
    {
        var command = new DeleteUserCommand(Guid.NewGuid(), Guid.NewGuid());

        _userManagerMock.Setup(x => x.FindByIdAsync(command.UserId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.UserNotFound));
    }

    [Test]
    public void Should_Throw_When_User_Already_Deleted()
    {
        var user = CreateUser(Guid.NewGuid(), EntityStatus.Deleted);
        var command = new DeleteUserCommand(user.Id, Guid.NewGuid());

        _userManagerMock.Setup(x => x.FindByIdAsync(command.UserId.ToString()))
            .ReturnsAsync(user);

        var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.UserAlreadyDeleted));
    }

    [Test]
    public void Should_Throw_When_User_Is_Not_Inactive()
    {
        var user = CreateUser(Guid.NewGuid(), EntityStatus.Active);
        var command = new DeleteUserCommand(user.Id, Guid.NewGuid());

        _userManagerMock.Setup(x => x.FindByIdAsync(command.UserId.ToString()))
            .ReturnsAsync(user);

        var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.UserMustBeLockedBeforeDelete));
    }

    [Test]
    public void Should_Throw_When_Update_Fails()
    {
        var user = CreateUser(Guid.NewGuid(), EntityStatus.Inactive);
        var command = new DeleteUserCommand(user.Id, Guid.NewGuid());

        _userManagerMock.Setup(x => x.FindByIdAsync(command.UserId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Failed" }));

        var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.SystemError));
    }

    [Test]
    public async Task Should_Delete_User_Successfully()
    {
        var user = CreateUser(Guid.NewGuid(), EntityStatus.Inactive);
        var command = new DeleteUserCommand(user.Id, Guid.NewGuid());

        _userManagerMock.Setup(x => x.FindByIdAsync(command.UserId.ToString()))
            .ReturnsAsync(user);

        _userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(user.Status, Is.EqualTo(EntityStatus.Deleted));
            Assert.That(user.LockoutEnabled, Is.True);
            Assert.That(user.LockoutEnd, Is.EqualTo(DateTimeOffset.MaxValue));
        });

        _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
        Assert.That(result, Is.EqualTo(Unit.Value));
    }

    #endregion

}