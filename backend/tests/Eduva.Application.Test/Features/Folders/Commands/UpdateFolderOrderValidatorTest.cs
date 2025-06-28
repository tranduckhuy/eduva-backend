using Eduva.Application.Features.Folders.Commands;
using FluentValidation.TestHelper;

namespace Eduva.Application.Test.Features.Folders.Commands
{
    [TestFixture]
    public class UpdateFolderOrderValidatorTest
    {
        #region Setup
        private UpdateFolderOrderValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new UpdateFolderOrderValidator();
        }
        #endregion

        #region Tests
        [Test]
        public void Should_Have_Error_When_Id_Is_Empty()
        {
            var model = new UpdateFolderOrderCommand { Id = Guid.Empty, Order = 1 };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Id);
        }

        [Test]
        public void Should_Have_Error_When_Order_Is_Negative()
        {
            var model = new UpdateFolderOrderCommand { Id = Guid.NewGuid(), Order = -1 };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Order);
        }

        [Test]
        public void Should_Not_Have_Error_When_Order_Is_Zero()
        {
            var model = new UpdateFolderOrderCommand { Id = Guid.NewGuid(), Order = 0 };
            var result = _validator.TestValidate(model);
            result.ShouldNotHaveValidationErrorFor(x => x.Order);
        }

        [Test]
        public void Should_Not_Have_Error_When_Valid()
        {
            var model = new UpdateFolderOrderCommand { Id = Guid.NewGuid(), Order = 2 };
            var result = _validator.TestValidate(model);
            result.ShouldNotHaveValidationErrorFor(x => x.Id);
            result.ShouldNotHaveValidationErrorFor(x => x.Order);
        }

        [Test]
        public void Should_Not_Have_Error_When_Order_Is_Large()
        {
            var model = new UpdateFolderOrderCommand { Id = Guid.NewGuid(), Order = int.MaxValue };
            var result = _validator.TestValidate(model);
            result.ShouldNotHaveValidationErrorFor(x => x.Order);
        }

        [Test]
        public void Should_Have_Multiple_Errors_When_Id_And_Order_Invalid()
        {
            var model = new UpdateFolderOrderCommand { Id = Guid.Empty, Order = -5 };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Id);
            result.ShouldHaveValidationErrorFor(x => x.Order);
        }

        [Test]
        public void Should_Not_Have_Error_For_Multiple_Valid_Orders()
        {
            var validOrders = new[] { 1, 10, 100, 9999 };
            foreach (var order in validOrders)
            {
                var model = new UpdateFolderOrderCommand { Id = Guid.NewGuid(), Order = order };
                var result = _validator.TestValidate(model);
                result.ShouldNotHaveValidationErrorFor(x => x.Order);
            }
        }
        #endregion
    }
}
