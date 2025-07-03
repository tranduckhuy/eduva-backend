using Eduva.Application.Features.AICreditPacks.Commands.CreateCreditPacks;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.AICreditPacks.Commands.CreatePack;

[TestFixture]
public class CreateAICreditPackCommandValidatorTests
{
    private CreateAICreditPackCommandValidator _validator = default!;

    #region Setup

    [SetUp]
    public void SetUp()
    {
        _validator = new CreateAICreditPackCommandValidator();
    }

    #endregion

    #region Tests

    [Test]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        var command = new CreateAICreditPackCommand { Name = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name is required.");
    }

    [Test]
    public void Should_Have_Error_When_Name_Too_Long()
    {
        var command = new CreateAICreditPackCommand { Name = new string('A', 101) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name must not exceed 100 characters.");
    }

    [Test]
    public void Should_Have_Error_When_Price_Is_Zero()
    {
        var command = new CreateAICreditPackCommand { Price = 0 };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Price)
            .WithErrorMessage("Price must be greater than 0.");
    }

    [Test]
    public void Should_Have_Error_When_Credits_Is_Zero()
    {
        var command = new CreateAICreditPackCommand { Credits = 0 };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Credits)
            .WithErrorMessage("Credits must be greater than 0.");
    }

    [Test]
    public void Should_Have_Error_When_BonusCredits_Is_Negative()
    {
        var command = new CreateAICreditPackCommand { BonusCredits = -1 };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.BonusCredits)
            .WithErrorMessage("Bonus credits must be 0 or more.");
    }

    [Test]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new CreateAICreditPackCommand
        {
            Name = "Valid Pack",
            Price = 10000,
            Credits = 50,
            BonusCredits = 10
        };

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

}