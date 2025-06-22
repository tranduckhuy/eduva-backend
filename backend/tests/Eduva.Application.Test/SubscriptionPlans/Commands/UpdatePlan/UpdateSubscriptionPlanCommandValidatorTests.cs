using Eduva.Application.Features.SubscriptionPlans.Commands.UpdatePlan;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using FluentValidation.TestHelper;
using Moq;
using System.Linq.Expressions;

namespace Eduva.Application.Test.SubscriptionPlans.Commands.UpdatePlan
{
    [TestFixture]
    public class UpdateSubscriptionPlanCommandValidatorTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = default!;
        private Mock<IGenericRepository<SubscriptionPlan, int>> _repoMock = default!;
        private UpdateSubscriptionPlanCommandValidator _validator = default!;

        #region UpdateSubscriptionPlanCommandValidatorTests Setup

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _repoMock = new Mock<IGenericRepository<SubscriptionPlan, int>>();

            _unitOfWorkMock
                .Setup(x => x.GetRepository<SubscriptionPlan, int>())
                .Returns(_repoMock.Object);

            _validator = new UpdateSubscriptionPlanCommandValidator(_unitOfWorkMock.Object);
        }

        #endregion

        #region UpdateSubscriptionPlanCommandValidator Tests

        [Test]
        public async Task Should_Pass_When_Valid_And_Name_Unique()
        {
            var command = new UpdateSubscriptionPlanCommand
            {
                Id = 1,
                Name = "Premium",
                MaxUsers = 10,
                StorageLimitGB = 5,
                PriceMonthly = 200000,
                PricePerYear = 1900000
            };

            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<SubscriptionPlan, bool>>>()))
                     .ReturnsAsync(false);

            var result = await _validator.TestValidateAsync(command);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public async Task Should_Fail_When_Name_Is_Duplicated()
        {
            var command = new UpdateSubscriptionPlanCommand
            {
                Id = 1,
                Name = "DuplicateName",
                MaxUsers = 10,
                StorageLimitGB = 5,
                PriceMonthly = 200000,
                PricePerYear = 1900000
            };

            _repoMock.Setup(r => r.ExistsAsync(It.Is<Expression<Func<SubscriptionPlan, bool>>>(
                expr => expr.Compile().Invoke(new SubscriptionPlan { Name = "DuplicateName", Id = 2 })
            ))).ReturnsAsync(true);

            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(c => c.Name).WithErrorMessage("Plan name already exists.");
        }

        [Test]
        public async Task Should_Fail_When_Name_Is_Empty()
        {
            var command = new UpdateSubscriptionPlanCommand
            {
                Id = 1,
                Name = "",
                MaxUsers = 10,
                StorageLimitGB = 5,
                PriceMonthly = 200000,
                PricePerYear = 1900000
            };

            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(c => c.Name);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public async Task Should_Fail_When_MaxUsers_Invalid(int maxUsers)
        {
            var command = new UpdateSubscriptionPlanCommand
            {
                Id = 1,
                Name = "ValidName",
                MaxUsers = maxUsers,
                StorageLimitGB = 5,
                PriceMonthly = 200000,
                PricePerYear = 1900000
            };

            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<SubscriptionPlan, bool>>>()))
                     .ReturnsAsync(false);

            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(c => c.MaxUsers);
        }

        [TestCase(-5)]
        public async Task Should_Fail_When_StorageLimit_Is_Negative(int storage)
        {
            var command = new UpdateSubscriptionPlanCommand
            {
                Id = 1,
                Name = "Test",
                MaxUsers = 10,
                StorageLimitGB = storage,
                PriceMonthly = 200000,
                PricePerYear = 1900000
            };

            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<SubscriptionPlan, bool>>>()))
                     .ReturnsAsync(false);

            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(c => c.StorageLimitGB);
        }

        [TestCase(-1)]
        public async Task Should_Fail_When_PriceMonthly_Is_Negative(decimal priceMonthly)
        {
            var command = new UpdateSubscriptionPlanCommand
            {
                Id = 1,
                Name = "Test",
                MaxUsers = 10,
                StorageLimitGB = 10,
                PriceMonthly = priceMonthly,
                PricePerYear = 1900000
            };

            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<SubscriptionPlan, bool>>>()))
                     .ReturnsAsync(false);

            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(c => c.PriceMonthly);
        }

        [TestCase(-1)]
        public async Task Should_Fail_When_PricePerYear_Is_Negative(decimal pricePerYear)
        {
            var command = new UpdateSubscriptionPlanCommand
            {
                Id = 1,
                Name = "Test",
                MaxUsers = 10,
                StorageLimitGB = 10,
                PriceMonthly = 200000,
                PricePerYear = pricePerYear
            };

            _repoMock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<SubscriptionPlan, bool>>>()))
                     .ReturnsAsync(false);

            var result = await _validator.TestValidateAsync(command);
            result.ShouldHaveValidationErrorFor(c => c.PricePerYear);
        }

        #endregion

    }
}