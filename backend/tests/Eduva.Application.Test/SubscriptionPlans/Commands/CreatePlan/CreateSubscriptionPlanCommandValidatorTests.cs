using Eduva.Application.Features.SubscriptionPlans.Commands.CreatePlan;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using FluentValidation.TestHelper;
using Moq;
using System.Linq.Expressions;

namespace Eduva.Application.Test.SubscriptionPlans.Commands.CreatePlan
{
    [TestFixture]
    public class CreateSubscriptionPlanCommandValidatorTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = default!;
        private Mock<IGenericRepository<SubscriptionPlan, int>> _repoMock = default!;
        private CreateSubscriptionPlanCommandValidator _validator = default!;

        #region CreateSubscriptionPlanCommandValidatorTests Setup

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _repoMock = new Mock<IGenericRepository<SubscriptionPlan, int>>();

            _unitOfWorkMock
                .Setup(x => x.GetRepository<SubscriptionPlan, int>())
                .Returns(_repoMock.Object);

            _validator = new CreateSubscriptionPlanCommandValidator(_unitOfWorkMock.Object);
        }

        #endregion

        #region CreateSubscriptionPlanCommandValidator Tests

        [Test]
        public async Task Should_Pass_Validation_When_All_Fields_Are_Valid_And_Name_Is_Unique()
        {
            var command = new CreateSubscriptionPlanCommand
            {
                Name = "Standard",
                PriceMonthly = 100000,
                PricePerYear = 1000000,
                MaxUsers = 10,
                StorageLimitGB = 5,
            };

            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<SubscriptionPlan, bool>>>()))
                     .ReturnsAsync(false);

            var result = await _validator.TestValidateAsync(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public async Task Should_Fail_When_Name_Is_Empty_Or_Duplicate()
        {
            var command = new CreateSubscriptionPlanCommand
            {
                Name = "",
                PriceMonthly = 100000,
                PricePerYear = 1000000,
                MaxUsers = 10,
                StorageLimitGB = 5,
            };

            var result1 = await _validator.TestValidateAsync(command);
            result1.ShouldHaveValidationErrorFor(c => c.Name);

            command.Name = "ExistingPlan";
            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<SubscriptionPlan, bool>>>()))
                     .ReturnsAsync(true);

            var result2 = await _validator.TestValidateAsync(command);
            result2.ShouldHaveValidationErrorFor(c => c.Name).WithErrorMessage("Plan name already exists.");
        }

        [TestCase(-1, "Monthly price must be >= 0.")]
        [TestCase(-1000, "Monthly price must be >= 0.")]
        public async Task Should_Fail_When_MonthlyPrice_Is_Invalid(decimal price, string errorMessage)
        {
            var command = new CreateSubscriptionPlanCommand
            {
                Name = "Test",
                PriceMonthly = price,
                PricePerYear = 100000,
                MaxUsers = 10,
                StorageLimitGB = 5,
            };

            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<SubscriptionPlan, bool>>>()))
                     .ReturnsAsync(false);

            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(c => c.PriceMonthly).WithErrorMessage(errorMessage);
        }

        [Test]
        public async Task Should_Fail_When_MaxUsers_Is_Zero_Or_Less()
        {
            var command = new CreateSubscriptionPlanCommand
            {
                Name = "Test",
                PriceMonthly = 100000,
                PricePerYear = 1000000,
                MaxUsers = 0,
                StorageLimitGB = 5,
            };

            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<SubscriptionPlan, bool>>>()))
                     .ReturnsAsync(false);

            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(c => c.MaxUsers);
        }

        [Test]
        public async Task Should_Fail_When_StorageLimit_Is_Negative()
        {
            var command = new CreateSubscriptionPlanCommand
            {
                Name = "Test",
                PriceMonthly = 100000,
                PricePerYear = 1000000,
                MaxUsers = 10,
                StorageLimitGB = -5,
            };

            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<SubscriptionPlan, bool>>>()))
                     .ReturnsAsync(false);

            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(c => c.StorageLimitGB);
        }

        #endregion

    }
}