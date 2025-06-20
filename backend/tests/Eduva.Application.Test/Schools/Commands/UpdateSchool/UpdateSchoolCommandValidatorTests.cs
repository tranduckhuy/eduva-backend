using Eduva.Application.Features.Schools.Commands.UpdateSchool;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using FluentValidation.TestHelper;
using Moq;
using System.Linq.Expressions;

namespace Eduva.Application.Test.Schools.Commands.UpdateSchool
{
    [TestFixture]
    public class UpdateSchoolCommandValidatorTests
    {
        private UpdateSchoolCommandValidator _validator = null!;
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IGenericRepository<School, int>> _schoolRepoMock = null!;

        #region UpdateSchoolCommandValidatorTests Setup

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _schoolRepoMock = new Mock<IGenericRepository<School, int>>();

            _unitOfWorkMock.Setup(u => u.GetRepository<School, int>())
                .Returns(_schoolRepoMock.Object);

            _validator = new UpdateSchoolCommandValidator(_unitOfWorkMock.Object);
        }

        #endregion

        #region UpdateSchoolCommandValidator Tests

        [Test]
        public async Task Should_Pass_When_ValidData()
        {
            var command = new UpdateSchoolCommand
            {
                Id = 1,
                Name = "Eduva",
                ContactEmail = "valid@eduva.vn",
                ContactPhone = "0909123456",
                Address = "HCM",
                WebsiteUrl = "https://eduva.vn"
            };

            _schoolRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<School, bool>>>()))
                .ReturnsAsync(false);

            var result = await _validator.TestValidateAsync(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public async Task Should_Fail_When_Email_AlreadyExists()
        {
            var command = new UpdateSchoolCommand
            {
                Id = 1,
                Name = "Eduva",
                ContactEmail = "duplicate@eduva.vn",
                ContactPhone = "0909123456",
                Address = "HCM",
                WebsiteUrl = "https://eduva.vn"
            };

            _schoolRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<School, bool>>>()))
                .ReturnsAsync(true);

            var result = await _validator.TestValidateAsync(command);

            result.ShouldHaveValidationErrorFor(x => x.ContactEmail)
                .WithErrorMessage("Email already exists.");
        }

        [Test]
        public async Task Should_Fail_When_Name_Empty()
        {
            var command = new UpdateSchoolCommand { Id = 1, Name = "" };

            _schoolRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<School, bool>>>()))
                .ReturnsAsync(false);

            var result = await _validator.TestValidateAsync(command);

            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("School name is required.");
        }

        [Test]
        public async Task Should_Fail_When_Email_InvalidFormat()
        {
            var command = new UpdateSchoolCommand { Id = 1, ContactEmail = "invalid-email" };

            _schoolRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<School, bool>>>()))
                .ReturnsAsync(false);

            var result = await _validator.TestValidateAsync(command);

            result.ShouldHaveValidationErrorFor(x => x.ContactEmail)
                .WithErrorMessage("Invalid email format.");
        }

        [Test]
        public async Task Should_Fail_When_Phone_Invalid()
        {
            var command = new UpdateSchoolCommand { Id = 1, ContactPhone = "123456" };

            _schoolRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<School, bool>>>()))
                .ReturnsAsync(false);

            var result = await _validator.TestValidateAsync(command);

            result.ShouldHaveValidationErrorFor(x => x.ContactPhone)
                .WithErrorMessage("Invalid phone number format");
        }

        [Test]
        public async Task Should_Fail_When_Website_InvalidUrl()
        {
            var command = new UpdateSchoolCommand { Id = 1, WebsiteUrl = "not-a-url" };

            _schoolRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<School, bool>>>()))
                .ReturnsAsync(false);

            var result = await _validator.TestValidateAsync(command);

            result.ShouldHaveValidationErrorFor(x => x.WebsiteUrl)
                .WithErrorMessage("Website URL must be a valid absolute URL.");
        }

        [Test]
        public async Task Should_Pass_When_WebsiteUrl_IsEmpty()
        {
            var command = new UpdateSchoolCommand { Id = 1, WebsiteUrl = "" };

            _schoolRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<School, bool>>>()))
                .ReturnsAsync(false);

            var result = await _validator.TestValidateAsync(command);

            result.ShouldNotHaveValidationErrorFor(x => x.WebsiteUrl);
        }

        #endregion

    }
}