using Eduva.Application.Common.Exceptions;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Features.Users.Commands;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Eduva.Application.Test.Features.Users.Commands
{
    [TestFixture]
    public class CreateUserByAdminCommandHandlerTests
    {
        private Mock<UserManager<ApplicationUser>> _userManagerMock = default!;

        #region CreateUserByAdminCommandHandlerTests Setup

        [SetUp]
        public void Setup()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object,
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<ApplicationUser>>(),
            new IUserValidator<ApplicationUser>[0],
            new IPasswordValidator<ApplicationUser>[0],
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<ApplicationUser>>>());
        }

        #endregion

        #region CreateUserByAdminCommandHandler Tests

        private CreateUserByAdminCommandHandler CreateHandler() =>
            new CreateUserByAdminCommandHandler(_userManagerMock.Object);

        [TestCase(Role.SystemAdmin)]
        [TestCase(Role.SchoolAdmin)]
        public void Handle_ShouldThrow_WhenRoleIsRestricted(Role role)
        {
            var handler = CreateHandler();
            var command = new CreateUserByAdminCommand { Role = role };

            Assert.ThrowsAsync<InvalidRestrictedRoleException>(() => handler.Handle(command, default));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Handle_ShouldThrow_WhenPasswordInvalid(string? password)
        {
            var handler = CreateHandler();
            var command = new CreateUserByAdminCommand { Role = Role.Student, InitialPassword = password! };

            var ex = Assert.ThrowsAsync<AppException>(() => handler.Handle(command, default));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ProvidedInformationIsInValid));
        }

        [Test]
        public void Handle_ShouldThrow_WhenEmailAlreadyExists()
        {
            _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(new ApplicationUser());

            var handler = CreateHandler();
            var command = new CreateUserByAdminCommand
            {
                Email = "test@example.com",
                Role = Role.Student,
                InitialPassword = "Abc@123",
                CreatorId = Guid.NewGuid()
            };

            Assert.ThrowsAsync<EmailAlreadyExistsException>(() => handler.Handle(command, default));
        }

        [Test]
        public void Handle_ShouldThrow_WhenCreatorNotExists()
        {
            _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

            var handler = CreateHandler();
            var command = new CreateUserByAdminCommand
            {
                Email = "new@example.com",
                Role = Role.Teacher,
                InitialPassword = "Abc@123",
                CreatorId = Guid.NewGuid()
            };

            Assert.ThrowsAsync<UserNotExistsException>(() => handler.Handle(command, default));
        }

        [Test]
        public void Handle_ShouldThrow_WhenCreatorHasNoSchool()
        {
            _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(new ApplicationUser { SchoolId = null });

            var handler = CreateHandler();
            var command = new CreateUserByAdminCommand
            {
                Email = "new@example.com",
                Role = Role.Teacher,
                InitialPassword = "Abc@123",
                CreatorId = Guid.NewGuid()
            };

            Assert.ThrowsAsync<UserNotPartOfSchoolException>(() => handler.Handle(command, default));
        }

        [Test]
        public void Handle_ShouldThrow_WhenCreateUserFails()
        {
            _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(new ApplicationUser { SchoolId = 1 });
            _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

            var handler = CreateHandler();

            var command = new CreateUserByAdminCommand
            {
                Email = "new@example.com",
                Role = Role.Teacher,
                InitialPassword = "Abc@123",
                CreatorId = Guid.NewGuid()
            };

            var ex = Assert.ThrowsAsync<AppException>(() => handler.Handle(command, default));
            Assert.That(ex!.StatusCode, Is.EqualTo(CustomCode.ProvidedInformationIsInValid));
            Assert.That(ex.Errors, Contains.Item("Password too weak"));
        }

        [Test]
        public async Task Handle_ShouldCreateUserSuccessfully()
        {
            _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(new ApplicationUser { SchoolId = 1 });
            _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            var handler = CreateHandler();

            var command = new CreateUserByAdminCommand
            {
                Email = "new@example.com",
                Role = Role.Teacher,
                InitialPassword = "Abc@123",
                CreatorId = Guid.NewGuid()
            };

            var result = await handler.Handle(command, default);

            Assert.That(result, Is.EqualTo(Unit.Value));
        }

        #endregion

    }
}