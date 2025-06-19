using Eduva.Application.Features.Schools.Commands;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using FluentValidation.TestHelper;
using Moq;
using System.Linq.Expressions;

namespace Eduva.Application.Test.Schools;

[TestFixture]
public class CreateSchoolCommandValidatorTests
{
    private CreateSchoolCommandValidator _validator = default!;
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<IGenericRepository<School, int>> _schoolRepoMock = default!;

    [SetUp]
    public void Setup()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _schoolRepoMock = new Mock<IGenericRepository<School, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<School, int>())
            .Returns(_schoolRepoMock.Object);

        _validator = new CreateSchoolCommandValidator(_unitOfWorkMock.Object);
    }

    [Test]
    public async Task Should_HaveError_When_NameIsEmpty()
    {
        var command = new CreateSchoolCommand
        {
            Name = "",
            ContactEmail = "valid@email.com",
            ContactPhone = "0909123456"
        };

        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Test]
    public async Task Should_HaveError_When_EmailFormatInvalid()
    {
        var command = new CreateSchoolCommand
        {
            Name = "School A",
            ContactEmail = "invalid-email",
            ContactPhone = "0909123456"
        };

        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.ContactEmail);
    }

    [Test]
    public async Task Should_HaveError_When_EmailAlreadyExists()
    {
        _schoolRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<School, bool>>>()))
            .ReturnsAsync(true);

        var command = new CreateSchoolCommand
        {
            Name = "School A",
            ContactEmail = "duplicate@email.com",
            ContactPhone = "0909123456"
        };

        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.ContactEmail)
              .WithErrorMessage("Email already exists.");
    }

    [Test]
    public async Task Should_HaveError_When_PhoneNumberInvalid()
    {
        var command = new CreateSchoolCommand
        {
            Name = "School A",
            ContactEmail = "valid@email.com",
            ContactPhone = "123456"
        };

        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.ContactPhone);
    }

    [Test]
    public async Task Should_HaveError_When_WebsiteUrlInvalid()
    {
        var command = new CreateSchoolCommand
        {
            Name = "School A",
            ContactEmail = "valid@email.com",
            ContactPhone = "0909123456",
            WebsiteUrl = "not-a-url"
        };

        var result = await _validator.TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(c => c.WebsiteUrl);
    }

    [Test]
    public async Task Should_NotHaveErrors_When_ValidData()
    {
        _schoolRepoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<School, bool>>>()))
            .ReturnsAsync(false);

        var command = new CreateSchoolCommand
        {
            Name = "School A",
            ContactEmail = "school@email.com",
            ContactPhone = "0909123456",
            WebsiteUrl = "https://eduva.vn"
        };

        var result = await _validator.TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}