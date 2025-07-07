using Eduva.Application.Common.Mappings;
using Eduva.Application.Exceptions.SubscriptionPlan;
using Eduva.Application.Features.SubscriptionPlans.Commands.UpdatePlan;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Moq;

namespace Eduva.Application.Test.Features.SubscriptionPlans.Commands.UpdatePlan
{
    [TestFixture]
    public class UpdateSubscriptionPlanCommandHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = default!;
        private Mock<IGenericRepository<SubscriptionPlan, int>> _repoMock = default!;
        private UpdateSubscriptionPlanCommandHandler _handler = default!;

        #region UpdateSubscriptionPlanCommandHandlerTests Setup

        [SetUp]
        public void Setup()
        {
            _repoMock = new Mock<IGenericRepository<SubscriptionPlan, int>>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _unitOfWorkMock
                .Setup(u => u.GetRepository<SubscriptionPlan, int>())
                .Returns(_repoMock.Object);

            _handler = new UpdateSubscriptionPlanCommandHandler(_unitOfWorkMock.Object);

            _ = AppMapper<AppMappingProfile>.Mapper; // Force lazy mapper init
        }

        #endregion

        #region UpdateSubscriptionPlanCommandHandler Tests 

        [Test]
        public async Task Handle_ShouldUpdatePlanAndReturnResponse()
        {
            // Arrange
            var command = new UpdateSubscriptionPlanCommand
            {
                Id = 1,
                Name = "Pro Updated",
                Description = "Updated Description",
                MaxUsers = 150,
                StorageLimitGB = 100,
                PriceMonthly = 299000,
                PricePerYear = 2800000
            };

            var existingPlan = new SubscriptionPlan
            {
                Id = 1,
                Name = "Old Pro",
                Description = "Old Description",
                MaxUsers = 100,
                StorageLimitGB = 50,
                PriceMonthly = 199000,
                PricePerYear = 1900000,
                Status = EntityStatus.Active
            };

            _repoMock.Setup(r => r.GetByIdAsync(command.Id))
                     .ReturnsAsync(existingPlan);

            _unitOfWorkMock.Setup(u => u.CommitAsync()).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Name, Is.EqualTo(command.Name));
                Assert.That(result.Description, Is.EqualTo(command.Description));
                Assert.That(result.MaxUsers, Is.EqualTo(command.MaxUsers));
                Assert.That(result.StorageLimitGB, Is.EqualTo(command.StorageLimitGB));
                Assert.That(result.PriceMonthly, Is.EqualTo(command.PriceMonthly));
                Assert.That(result.PricePerYear, Is.EqualTo(command.PricePerYear));
            });

            _repoMock.Verify(r => r.Update(It.Is<SubscriptionPlan>(p => p.Id == command.Id)), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Test]
        public void Handle_ShouldThrowPlanNotFoundException_WhenPlanDoesNotExist()
        {
            // Arrange
            var command = new UpdateSubscriptionPlanCommand { Id = 999 };

            _repoMock.Setup(r => r.GetByIdAsync(command.Id))
                     .ReturnsAsync((SubscriptionPlan?)null);

            // Act & Assert
            Assert.ThrowsAsync<PlanNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        }

        #endregion

    }
}