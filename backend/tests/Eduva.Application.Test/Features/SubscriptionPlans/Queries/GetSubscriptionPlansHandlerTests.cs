using Eduva.Application.Common.Models;
using Eduva.Application.Features.SubscriptionPlans.Queries;
using Eduva.Application.Features.SubscriptionPlans.Specifications;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Moq;

namespace Eduva.Application.Test.Features.SubscriptionPlans.Queries
{
    [TestFixture]
    public class GetSubscriptionPlansHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = default!;
        private Mock<IGenericRepository<SubscriptionPlan, int>> _subscriptionRepoMock = default!;
        private GetSubscriptionPlansQueryHandler _handler = default!;

        #region GetSubscriptionPlansHandler Setup

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _subscriptionRepoMock = new Mock<IGenericRepository<SubscriptionPlan, int>>();

            _unitOfWorkMock.Setup(x => x.GetRepository<SubscriptionPlan, int>())
                .Returns(_subscriptionRepoMock.Object);

            _handler = new GetSubscriptionPlansQueryHandler(_unitOfWorkMock.Object);
        }

        #endregion

        #region GetSubscriptionPlansHandler Tests

        [Test]
        public async Task Handle_ShouldReturnMappedPagination_WhenValidRequest()
        {
            // Arrange
            var param = new SubscriptionPlanSpecParam
            {
                PageIndex = 1,
                PageSize = 10,
                ActiveOnly = true,
                SortBy = "name",
                SortDirection = "asc"
            };

            var query = new GetSubscriptionPlansQuery(param);

            var data = new List<SubscriptionPlan>
            {
                new SubscriptionPlan { Id = 1, Name = "Basic", PriceMonthly = 100000 },
                new SubscriptionPlan { Id = 2, Name = "Pro", PriceMonthly = 200000 }
            };

            var paginated = new Pagination<SubscriptionPlan>
            {
                PageIndex = 1,
                PageSize = 10,
                Count = 2,
                Data = data
            };

            _subscriptionRepoMock
                .Setup(r => r.GetWithSpecAsync(It.IsAny<SubscriptionPlanSpecification>()))
                .ReturnsAsync(paginated);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data, Has.Count.EqualTo(2));
            Assert.That(result.Data.Any(r => r.Name == "Basic"), Is.True);
        }

        #endregion

    }
}