using Eduva.Application.Common.Models;
using Eduva.Application.Features.SchoolSubscriptions.Queries;
using Eduva.Application.Features.SchoolSubscriptions.Specifications;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Moq;

namespace Eduva.Application.Test.Features.SchoolSubscriptions.Queries
{
    [TestFixture]
    public class GetShoolSubscriptionQueryHandlerTests
    {
        private Mock<ISchoolSubscriptionRepository> _repositoryMock = default!;
        private GetShoolSubscriptionQueryHandler _handler = default!;

        #region GetShoolSubscriptionQueryHandlerTests Setup

        [SetUp]
        public void Setup()
        {
            _repositoryMock = new Mock<ISchoolSubscriptionRepository>();
            _handler = new GetShoolSubscriptionQueryHandler(_repositoryMock.Object);
        }

        #endregion

        #region GetShoolSubscriptionQueryHandler Tests

        [Test]
        public async Task Handle_ShouldReturnPaginatedResponse()
        {
            // Arrange
            var param = new SchoolSubscriptionSpecParam
            {
                PageIndex = 1,
                PageSize = 10
            };

            var query = new GetSchoolSubscriptionQuery(param);

            var subscriptionList = new List<SchoolSubscription>
            {
                new() { Id = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow },
                new() { Id = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) }
            };

            var mockPagination = new Pagination<SchoolSubscription>(
                pageIndex: 1,
                pageSize: 10,
                count: 2,
                data: subscriptionList);

            _repositoryMock
                .Setup(r => r.GetWithSpecAsync(It.IsAny<SchoolSubscriptionSpecification>()))
                .ReturnsAsync(mockPagination);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Data, Has.Count.EqualTo(2));
                Assert.That(result.PageIndex, Is.EqualTo(1));
                Assert.That(result.PageSize, Is.EqualTo(10));
            });
        }

        #endregion 

    }
}