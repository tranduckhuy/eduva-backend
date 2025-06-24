using Eduva.Application.Common.Exceptions;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Features.Users.Commands;
using Eduva.Application.Interfaces.Services;
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
        private Mock<ISchoolValidationService> _schoolValidationServiceMock = default!;
        private CreateUserByAdminCommandHandler _handler = default!;

        #region CreateUserByAdminCommandHandlerTests Setup

        [SetUp]
        public void Setup()
        {
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
                Mock.Of<ILogger<UserManager<ApplicationUser>>>())
            ;

            _schoolValidationServiceMock = new Mock<ISchoolValidationService>();

            _handler = new CreateUserByAdminCommandHandler(
                _userManagerMock.Object,
                _schoolValidationServiceMock.Object);
        }

        #endregion

        #region CreateUserByAdminCommandHandler Tests

        [Test]
        public async Task Should_Throw_When_Role_Is_SystemAdmin_Or_SchoolAdmin()
        {
            var command = new CreateUserByAdminCommand
            {
                Role = Role.SystemAdmin,
                InitialPassword = "dummy"
            };

            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected InvalidRestrictedRoleException was not thrown");
            }
            catch (InvalidRestrictedRoleException ex)
            {
                Assert.That(ex, Is.Not.Null);
            }
        }

        [Test]
        public async Task Should_Throw_When_InitialPassword_Is_Null()
        {
            var command = new CreateUserByAdminCommand
            {
                Role = Role.Teacher,
                InitialPassword = string.Empty
            };

            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected AppException was not thrown");
            }
            catch (AppException ex)
            {
                Assert.That(ex, Is.Not.Null);
            }
        }

        [Test]
        public async Task Should_Throw_When_Email_Already_Exists()
        {
            var command = new CreateUserByAdminCommand
            {
                Role = Role.Teacher,
                InitialPassword = "Password123!",
                Email = "test@email.com",
                CreatorId = Guid.NewGuid()
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email))
                .ReturnsAsync(new ApplicationUser());

            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected EmailAlreadyExistsException was not thrown");
            }
            catch (EmailAlreadyExistsException ex)
            {
                Assert.That(ex, Is.Not.Null);
            }
        }

        [Test]
        public async Task Should_Throw_When_Creator_Does_Not_Exist()
        {
            var command = new CreateUserByAdminCommand
            {
                Role = Role.Teacher,
                InitialPassword = "Password123!",
                Email = "test@email.com",
                CreatorId = Guid.NewGuid()
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(x => x.FindByIdAsync(command.CreatorId.ToString()))
                .ReturnsAsync((ApplicationUser?)null);

            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected UserNotExistsException was not thrown");
            }
            catch (UserNotExistsException ex)
            {
                Assert.That(ex, Is.Not.Null);
            }
        }

        [Test]
        public async Task Should_Throw_When_Creator_Not_Assigned_To_School()
        {
            var command = new CreateUserByAdminCommand
            {
                Role = Role.Teacher,
                InitialPassword = "Password123!",
                Email = "test@email.com",
                CreatorId = Guid.NewGuid()
            };

            var creator = new ApplicationUser { Id = command.CreatorId, SchoolId = null };

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(x => x.FindByIdAsync(command.CreatorId.ToString()))
                .ReturnsAsync(creator);

            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected UserNotPartOfSchoolException was not thrown");
            }
            catch (UserNotPartOfSchoolException ex)
            {
                Assert.That(ex, Is.Not.Null);
            }
        }

        [Test]
        public async Task Should_Throw_When_Cannot_Add_More_Users()
        {
            var command = new CreateUserByAdminCommand
            {
                Role = Role.Teacher,
                InitialPassword = "Password123!",
                Email = "test@email.com",
                CreatorId = Guid.NewGuid()
            };

            var creator = new ApplicationUser { Id = command.CreatorId, SchoolId = 1 };

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(x => x.FindByIdAsync(command.CreatorId.ToString())).ReturnsAsync(creator);
            _schoolValidationServiceMock.Setup(x => x.ValidateCanAddUsersAsync(1, 1, default))
                .ThrowsAsync(new AppException(CustomCode.ExceedUserLimit));

            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected AppException was not thrown");
            }
            catch (AppException ex)
            {
                Assert.That(ex, Is.Not.Null);
            }
        }

        [Test]
        public async Task Should_Throw_When_CreateAsync_Fails()
        {
            var command = new CreateUserByAdminCommand
            {
                Role = Role.Teacher,
                InitialPassword = "Password123!",
                Email = "test@email.com",
                FullName = "Test User",
                CreatorId = Guid.NewGuid()
            };

            var creator = new ApplicationUser { Id = command.CreatorId, SchoolId = 2 };

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(x => x.FindByIdAsync(command.CreatorId.ToString())).ReturnsAsync(creator);
            _schoolValidationServiceMock.Setup(x => x.ValidateCanAddUsersAsync(2, 1, default)).Returns(Task.CompletedTask);
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), command.InitialPassword))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Error" }));

            try
            {
                await _handler.Handle(command, default);
                Assert.Fail("Expected AppException was not thrown");
            }
            catch (AppException ex)
            {
                Assert.That(ex.Message, Does.Contain("Provided information is invalid"));
            }
        }

        [Test]
        public async Task Should_Create_User_Successfully()
        {
            var command = new CreateUserByAdminCommand
            {
                Role = Role.Teacher,
                InitialPassword = "Password123!",
                Email = "test@email.com",
                FullName = "Test User",
                CreatorId = Guid.NewGuid()
            };

            var creator = new ApplicationUser { Id = command.CreatorId, SchoolId = 1 };

            _userManagerMock.Setup(x => x.FindByEmailAsync(command.Email)).ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(x => x.FindByIdAsync(command.CreatorId.ToString())).ReturnsAsync(creator);
            _schoolValidationServiceMock.Setup(x => x.ValidateCanAddUsersAsync(1, 1, default)).Returns(Task.CompletedTask);
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), command.InitialPassword))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), Role.Teacher.ToString()))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _handler.Handle(command, default);
            Assert.That(result, Is.EqualTo(Unit.Value));
        }

        #endregion

    }
}