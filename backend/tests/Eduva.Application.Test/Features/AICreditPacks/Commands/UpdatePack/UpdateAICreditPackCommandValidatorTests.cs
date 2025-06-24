using Eduva.Application.Features.AICreditPacks.Commands.UpdateCreditPacks;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.AICreditPacks.Commands.UpdatePack;

[TestFixture]
public class UpdateAICreditPackCommandValidatorTests
{
    private UpdateAICreditPackCommandValidator _validator = default!;

    #region Setup

    [SetUp]
    public void SetUp()
    {
        _validator = new UpdateAICreditPackCommandValidator();
    }

    #endregion

    #region Tests

    [Test]
    public void Should_Pass_When_AllFieldsAreValid()
    {
        var command = new UpdateAICreditPackCommand
        {
            Id = 1,
            Name = "Premium Pack",
            Price = 100000,
            Credits = 500,
            BonusCredits = 100
        };

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestCase("")]
    [TestCase(null)]
    public void Should_Fail_When_NameIsEmpty(string? name)
    {
        var command = new UpdateAICreditPackCommand
        {
            Name = name!,
            Price = 50000,
            Credits = 200,
            BonusCredits = 50
        };

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Should_Fail_When_NameExceedsMaxLength()
    {
        var command = new UpdateAICreditPackCommand
        {
            Name = new string('A', 101),
            Price = 50000,
            Credits = 200,
            BonusCredits = 50
        };

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [TestCase(0)]
    [TestCase(-1000)]
    public void Should_Fail_When_PriceIsZeroOrNegative(decimal price)
    {
        var command = new UpdateAICreditPackCommand
        {
            Name = "Valid",
            Price = price,
            Credits = 200,
            BonusCredits = 50
        };

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [TestCase(0)]
    [TestCase(-50)]
    public void Should_Fail_When_CreditsIsZeroOrNegative(int credits)
    {
        var command = new UpdateAICreditPackCommand
        {
            Name = "Valid",
            Price = 50000,
            Credits = credits,
            BonusCredits = 50
        };

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Credits);
    }

    [TestCase(-1)]
    [TestCase(-100)]
    public void Should_Fail_When_BonusCreditsIsNegative(int bonusCredits)
    {
        var command = new UpdateAICreditPackCommand
        {
            Name = "Valid",
            Price = 50000,
            Credits = 200,
            BonusCredits = bonusCredits
        };

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.BonusCredits);
    }

    #endregion

}