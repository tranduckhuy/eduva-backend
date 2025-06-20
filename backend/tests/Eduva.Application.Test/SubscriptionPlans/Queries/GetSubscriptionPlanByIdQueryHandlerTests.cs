using Eduva.Application.Exceptions.SubscriptionPlan;
using Eduva.Application.Features.SubscriptionPlans.Queries;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Moq;

namespace Eduva.Application.Test.SubscriptionPlans.Queries
{
    [TestFixture]
    public class GetSubscriptionPlanByIdQueryHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IGenericRepository<SubscriptionPlan, int>> _repoMock = null!;
        private GetSubscriptionPlanByIdQueryHandler _handler = null!;
        private CancellationToken _cancellationToken = CancellationToken.None;

        #region GetSubscriptionPlanByIdQueryHandlerTests Setup

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _repoMock = new Mock<IGenericRepository<SubscriptionPlan, int>>();

            _unitOfWorkMock.Setup(u => u.GetRepository<SubscriptionPlan, int>())
                           .Returns(_repoMock.Object);

            _handler = new GetSubscriptionPlanByIdQueryHandler(_unitOfWorkMock.Object);
        }

        #endregion

        #region GetSubscriptionPlanByIdQueryHandler Tests

        [Test]
        public async Task Handle_ShouldReturnMappedPlanResponse_WhenPlanExists()
        {
            // Arrange
            var query = new GetSubscriptionPlanByIdQuery(1);
            var plan = new SubscriptionPlan
            {
                Id = 1,
                Name = "Basic",
                MaxUsers = 10,
                StorageLimitGB = 5,
                MaxMinutesPerMonth = 100,
                PriceMonthly = 100000,
                PricePerYear = 990000,
                Status = EntityStatus.Active
            };

            _repoMock.Setup(r => r.GetByIdAsync(query.Id)).ReturnsAsync(plan);

            // Act
            var result = await _handler.Handle(query, _cancellationToken);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(plan.Id));
                Assert.That(result.Name, Is.EqualTo(plan.Name));
                Assert.That(result.MaxUsers, Is.EqualTo(plan.MaxUsers));
                Assert.That(result.StorageLimitGB, Is.EqualTo(plan.StorageLimitGB));
                Assert.That(result.Status, Is.EqualTo(plan.Status));
            });
        }

        [Test]
        public void Handle_ShouldThrowPlanNotFoundException_WhenPlanNotFound()
        {
            // Arrange
            var query = new GetSubscriptionPlanByIdQuery(999);
            _repoMock.Setup(r => r.GetByIdAsync(query.Id)).ReturnsAsync((SubscriptionPlan?)null);

            // Act & Assert
            Assert.ThrowsAsync<PlanNotFoundException>(() =>
                _handler.Handle(query, _cancellationToken));
        }

        #endregion

    }
}