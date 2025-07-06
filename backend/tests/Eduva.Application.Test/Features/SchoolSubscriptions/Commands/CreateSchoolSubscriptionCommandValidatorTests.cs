using Eduva.Application.Features.Payments.Commands;
using Eduva.Application.Features.SchoolSubscriptions.Commands;
using Eduva.Domain.Enums;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.SchoolSubscriptions.Commands;

[TestFixture]
public class CreateSchoolSubscriptionCommandValidatorTests
{
    private CreateSchoolSubscriptionCommandValidator _validator = default!;

    #region CreateSchoolSubscriptionCommandValidator Setup

    [SetUp]
    public void Setup()
    {
        _validator = new CreateSchoolSubscriptionCommandValidator();
    }

    #endregion

    #region CreateSchoolSubscriptionCommandValidator Tests

    [Test]
    public void Should_Pass_When_Valid_Command()
    {
        var command = new CreateSchoolSubscriptionCommand
        {
            PlanId = 1,
            SchoolId = 2,
            BillingCycle = BillingCycle.Monthly
        };

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Should_Fail_When_PlanId_Is_Zero()
    {
        var command = new CreateSchoolSubscriptionCommand
        {
            PlanId = 0,
            SchoolId = 2,
            BillingCycle = BillingCycle.Monthly
        };

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PlanId)
              .WithErrorMessage("PlanId must be greater than 0.");
    }

    [Test]
    public void Should_Fail_When_BillingCycle_Is_Invalid()
    {
        var command = new CreateSchoolSubscriptionCommand
        {
            PlanId = 1,
            SchoolId = 1,
            BillingCycle = (BillingCycle)999
        };

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.BillingCycle)
              .WithErrorMessage("BillingCycle is invalid.");
    }

    #endregion

}