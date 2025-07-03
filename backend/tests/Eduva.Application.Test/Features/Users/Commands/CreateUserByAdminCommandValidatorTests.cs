using Eduva.Application.Features.Users.Commands;
using Eduva.Domain.Enums;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.Users.Commands
{
    [TestFixture]
    public class CreateUserByAdminCommandValidatorTests
    {
        private CreateUserByAdminCommandValidator _validator = default!;

        #region CreateUserByAdminCommandValidatorTests Setup

        [SetUp]
        public void Setup()
        {
            _validator = new CreateUserByAdminCommandValidator();
        }

        #endregion

        #region CreateUserByAdminCommandValidator Tests

        [Test]
        public void Should_HaveError_When_EmailIsEmpty()
        {
            var model = new CreateUserByAdminCommand { Email = "" };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Test]
        public void Should_HaveError_When_EmailIsInvalid()
        {
            var model = new CreateUserByAdminCommand { Email = "not-an-email" };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Test]
        public void Should_HaveError_When_FullNameIsEmpty()
        {
            var model = new CreateUserByAdminCommand { FullName = "" };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.FullName);
        }

        [Test]
        public void Should_HaveError_When_FullNameTooLong()
        {
            var model = new CreateUserByAdminCommand { FullName = new string('A', 101) };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.FullName);
        }

        [TestCase(Role.SystemAdmin)]
        [TestCase(Role.SchoolAdmin)]
        public void Should_HaveError_When_RoleIsRestricted(Role role)
        {
            var model = new CreateUserByAdminCommand { Role = role };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Role);
        }

        [Test]
        public void Should_HaveError_When_InitialPasswordIsEmpty()
        {
            var model = new CreateUserByAdminCommand { InitialPassword = "" };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.InitialPassword);
        }

        [Test]
        public void Should_HaveError_When_InitialPasswordTooShort()
        {
            var model = new CreateUserByAdminCommand { InitialPassword = "short" };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.InitialPassword);
        }

        [Test]
        public void Should_HaveError_When_InitialPasswordTooLong()
        {
            var model = new CreateUserByAdminCommand { InitialPassword = new string('A', 256) };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.InitialPassword);
        }

        [Test]
        public void Should_NotHaveAnyErrors_When_RequestIsValid()
        {
            var model = new CreateUserByAdminCommand
            {
                Email = "user@example.com",
                FullName = "Valid User",
                Role = Role.Teacher,
                InitialPassword = "StrongPass123!"
            };

            var result = _validator.TestValidate(model);
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion

    }
}