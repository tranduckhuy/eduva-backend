using Eduva.Application.Common.Mappings;
using Eduva.Application.Features.SubscriptionPlans.Commands.CreatePlan;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Moq;

namespace Eduva.Application.Test.Features.SubscriptionPlans.Commands.CreatePlan
{
    [TestFixture]
    public class CreateSubscriptionPlanCommandHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = default!;
        private Mock<IGenericRepository<SubscriptionPlan, int>> _repoMock = default!;
        private CreateSubscriptionPlanCommandHandler _handler = default!;

        #region CreateSubscriptionPlanCommandHandlerTests Setup

        [SetUp]
        public void Setup()
        {
            _repoMock = new Mock<IGenericRepository<SubscriptionPlan, int>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _unitOfWorkMock
                .Setup(u => u.GetRepository<SubscriptionPlan, int>())
                .Returns(_repoMock.Object);

            _handler = new CreateSubscriptionPlanCommandHandler(_unitOfWorkMock.Object);

            _ = AppMapper.Mapper;
        }

        #endregion

        #region CreateSubscriptionPlanCommandHandler Tests

        [Test]
        public async Task Handle_ShouldCreatePlanAndReturnResponse()
        {
            // Arrange
            var command = new CreateSubscriptionPlanCommand
            {
                Name = "Pro",
                Description = "Premium plan",
                MaxUsers = 100,
                StorageLimitGB = 50,
                PriceMonthly = 199000,
                PricePerYear = 1900000
            };

            SubscriptionPlan? capturedPlan = null;
            _repoMock
                .Setup(r => r.AddAsync(It.IsAny<SubscriptionPlan>()))
                .Callback<SubscriptionPlan>(p => capturedPlan = p)
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(u => u.CommitAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Name, Is.EqualTo(command.Name));
                Assert.That(result.MaxUsers, Is.EqualTo(command.MaxUsers));
                Assert.That(result.StorageLimitGB, Is.EqualTo(command.StorageLimitGB));
                Assert.That(result.PriceMonthly, Is.EqualTo(command.PriceMonthly));
                Assert.That(result.PricePerYear, Is.EqualTo(command.PricePerYear));
                Assert.That(result.Status, Is.EqualTo(EntityStatus.Active));
                Assert.That(result.Description, Is.EqualTo(command.Description));
                Assert.That(capturedPlan, Is.Not.Null);
            });
            Assert.That(capturedPlan!.Status, Is.EqualTo(EntityStatus.Active));
        }

        #endregion

    }
}