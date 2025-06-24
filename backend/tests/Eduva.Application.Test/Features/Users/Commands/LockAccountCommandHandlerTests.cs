using Eduva.Application.Common.Exceptions;
using Eduva.Application.Exceptions.Auth;
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
public class LockAccountCommandHandlerTests
{
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<UserManager<ApplicationUser>> _userManagerMock = default!;
    private LockAccountCommandHandler _handler = default!;

    #region Helper Methods

    private static ApplicationUser CreateUser(Guid id, EntityStatus status = EntityStatus.Active)
        => new ApplicationUser { Id = id, Status = status };

    #endregion

    #region LockAccountCommandHandlerTests Setup

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

        _handler = new LockAccountCommandHandler(_unitOfWorkMock.Object, _userManagerMock.Object);
    }

    #endregion

    #region LockAccountCommandHandler Tests 

    [Test]
    public void Should_Throw_When_SelfLocking()
    {
        var userId = Guid.NewGuid();

        var command = new LockAccountCommand(userId, userId);

        var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.CannotLockSelf));
    }

    [Test]
    public void Should_Throw_When_TargetUser_NotFound()
    {
        var command = new LockAccountCommand(Guid.NewGuid(), Guid.NewGuid());

        _userManagerMock.Setup(x => x.FindByIdAsync(command.UserId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        Assert.ThrowsAsync<UserNotExistsException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Test]
    public void Should_Throw_When_ExecutorUser_NotFound()
    {
        var targetUser = CreateUser(Guid.NewGuid());

        var command = new LockAccountCommand(targetUser.Id, Guid.NewGuid());

        _userManagerMock.Setup(x => x.FindByIdAsync(command.UserId.ToString()))
            .ReturnsAsync(targetUser);

        _userManagerMock.Setup(x => x.FindByIdAsync(command.ExecutorId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        Assert.ThrowsAsync<UserNotExistsException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Test]
    public void Should_Throw_When_SchoolAdmin_Tries_To_Lock_SystemAdmin()
    {
        var targetUser = CreateUser(Guid.NewGuid());
        var executorUser = CreateUser(Guid.NewGuid());

        var command = new LockAccountCommand(targetUser.Id, executorUser.Id);

        _userManagerMock.Setup(x => x.FindByIdAsync(command.UserId.ToString())).ReturnsAsync(targetUser);
        _userManagerMock.Setup(x => x.FindByIdAsync(command.ExecutorId.ToString())).ReturnsAsync(executorUser);

        _userManagerMock.Setup(x => x.GetRolesAsync(targetUser))
            .ReturnsAsync([Role.SystemAdmin.ToString()]);

        _userManagerMock.Setup(x => x.GetRolesAsync(executorUser))
            .ReturnsAsync([Role.SchoolAdmin.ToString()]);

        var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.Forbidden));
    }

    [Test]
    public void Should_Throw_When_User_Already_Locked()
    {
        var targetUser = CreateUser(Guid.NewGuid());
        var executorUser = CreateUser(Guid.NewGuid());

        targetUser.LockoutEnd = DateTimeOffset.UtcNow.AddDays(1);

        var command = new LockAccountCommand(targetUser.Id, executorUser.Id);

        _userManagerMock.Setup(x => x.FindByIdAsync(command.UserId.ToString())).ReturnsAsync(targetUser);
        _userManagerMock.Setup(x => x.FindByIdAsync(command.ExecutorId.ToString())).ReturnsAsync(executorUser);

        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync([Role.Teacher.ToString()]);

        var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.UserAlreadyLocked));
    }

    [Test]
    public async Task Should_Lock_User_Successfully()
    {
        var targetUser = CreateUser(Guid.NewGuid());
        var executorUser = CreateUser(Guid.NewGuid());

        var command = new LockAccountCommand(targetUser.Id, executorUser.Id);

        _userManagerMock.Setup(x => x.FindByIdAsync(command.UserId.ToString())).ReturnsAsync(targetUser);
        _userManagerMock.Setup(x => x.FindByIdAsync(command.ExecutorId.ToString())).ReturnsAsync(executorUser);

        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync([Role.Teacher.ToString()]);

        _userManagerMock.Setup(x => x.UpdateAsync(targetUser))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _handler.Handle(command, CancellationToken.None);

        _userManagerMock.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(
            u => u.LockoutEnabled && u.LockoutEnd == DateTimeOffset.MaxValue && u.Status == EntityStatus.Inactive)), Times.Once);

        _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);

        Assert.That(result, Is.EqualTo(Unit.Value));
    }

    [Test]
    public void Should_Throw_When_Update_Fails()
    {
        var targetUser = CreateUser(Guid.NewGuid());
        var executorUser = CreateUser(Guid.NewGuid());

        var command = new LockAccountCommand(targetUser.Id, executorUser.Id);

        _userManagerMock.Setup(x => x.FindByIdAsync(command.UserId.ToString())).ReturnsAsync(targetUser);
        _userManagerMock.Setup(x => x.FindByIdAsync(command.ExecutorId.ToString())).ReturnsAsync(executorUser);

        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync([Role.Teacher.ToString()]);

        _userManagerMock.Setup(x => x.UpdateAsync(targetUser))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Error" }));

        var ex = Assert.ThrowsAsync<AppException>(() => _handler.Handle(command, CancellationToken.None));
        Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.SystemError));
    }

    #endregion

}