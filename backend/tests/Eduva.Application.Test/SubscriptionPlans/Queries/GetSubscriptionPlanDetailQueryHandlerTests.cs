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
    public class GetSubscriptionPlanDetailQueryHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = default!;
        private Mock<IGenericRepository<SubscriptionPlan, int>> _planRepoMock = default!;
        private Mock<ISchoolSubscriptionRepository> _schoolSubRepoMock = default!;
        private GetSubscriptionPlanDetailQueryHandler _handler = default!;

        #region GetSubscriptionPlanDetailQueryHandlerTests Setup

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _planRepoMock = new Mock<IGenericRepository<SubscriptionPlan, int>>();
            _schoolSubRepoMock = new Mock<ISchoolSubscriptionRepository>();

            _unitOfWorkMock.Setup(u => u.GetRepository<SubscriptionPlan, int>())
                .Returns(_planRepoMock.Object);

            _unitOfWorkMock.Setup(u => u.GetCustomRepository<ISchoolSubscriptionRepository>())
                .Returns(_schoolSubRepoMock.Object);

            _handler = new GetSubscriptionPlanDetailQueryHandler(_unitOfWorkMock.Object);
        }

        #endregion

        #region GetSubscriptionPlanDetailQueryHandler Tests

        [Test]
        public async Task Handle_ShouldReturnDetail_WhenPlanExists()
        {
            // Arrange
            var query = new GetSubscriptionPlanDetailQuery(1);
            var subscriptionPlan = new SubscriptionPlan
            {
                Id = 1,
                Name = "Plus",
                Description = "Advanced features",
                MaxUsers = 100,
                StorageLimitGB = 50,
                MaxMinutesPerMonth = 1000,
                PriceMonthly = 149000,
                PricePerYear = 990000,
                Status = EntityStatus.Active
            };

            _planRepoMock.Setup(r => r.GetByIdAsync(query.Id))
                .ReturnsAsync(subscriptionPlan);

            _schoolSubRepoMock.Setup(r => r.CountSchoolsUsingPlanAsync(query.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(5);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(1));
                Assert.That(result.Name, Is.EqualTo("Plus"));
                Assert.That(result.Description, Is.EqualTo("Advanced features"));
                Assert.That(result.MaxUsers, Is.EqualTo(100));
                Assert.That(result.StorageLimitGB, Is.EqualTo(50));
                Assert.That(result.MaxMinutesPerMonth, Is.EqualTo(1000));
                Assert.That(result.PriceMonthly, Is.EqualTo(149000));
                Assert.That(result.PricePerYear, Is.EqualTo(990000));
                Assert.That(result.Status, Is.EqualTo(EntityStatus.Active));
                Assert.That(result.NumberOfSchoolsUsing, Is.EqualTo(5));
            });
        }

        [Test]
        public void Handle_ShouldThrowPlanNotFoundException_WhenPlanNotExists()
        {
            // Arrange
            var query = new GetSubscriptionPlanDetailQuery(999);

            _planRepoMock.Setup(r => r.GetByIdAsync(query.Id))
                .ReturnsAsync((SubscriptionPlan?)null);

            // Act & Assert
            Assert.ThrowsAsync<PlanNotFoundException>(() =>
                _handler.Handle(query, CancellationToken.None));
        }

        #endregion

    }
}