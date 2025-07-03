using Eduva.Application.Common.Models;
using Eduva.Application.Features.Payments.Queries;
using Eduva.Application.Features.Payments.Specifications;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Moq;

namespace Eduva.Application.Test.Features.Payment.Queries
{
    [TestFixture]
    public class GetPaymentTransactionsQueryHandlerTests
    {
        private Mock<IPaymentTransactionRepository> _repositoryMock = default!;
        private GetPaymentTransactionsQueryHandler _handler = default!;

        #region GetPaymentTransactionsQueryHandlerTests Setup

        [SetUp]
        public void SetUp()
        {
            _repositoryMock = new Mock<IPaymentTransactionRepository>();
            _handler = new GetPaymentTransactionsQueryHandler(_repositoryMock.Object);
        }

        #endregion

        #region GetPaymentTransactionsQueryHandler Tests

        [Test]
        public async Task Handle_ShouldReturnMappedPagination_WhenSpecMatches()
        {
            // Arrange
            var param = new PaymentSpecParam
            {
                PageIndex = 1,
                PageSize = 10
            };
            var query = new GetPaymentTransactionsQuery(param);

            var domainResult = new Pagination<PaymentTransaction>
            {
                PageIndex = 1,
                PageSize = 10,
                Count = 2,
                Data = new List<PaymentTransaction>
                {
                    new() { Id = Guid.NewGuid(), Amount = 100_000, TransactionCode = "T001" },
                    new() { Id = Guid.NewGuid(), Amount = 200_000, TransactionCode = "T002" }
                }
            };

            _repositoryMock
                .Setup(r => r.GetWithSpecAsync(It.IsAny<PaymentSpecification>()))
                .ReturnsAsync(domainResult);

            // Act
            var result = await _handler.Handle(query, default);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Count, Is.EqualTo(2));
                Assert.That(result.Data, Has.Count.EqualTo(2));
                Assert.That(result.Data.First().TransactionCode, Is.EqualTo("T001"));
            });

            _repositoryMock.Verify(r => r.GetWithSpecAsync(It.IsAny<PaymentSpecification>()), Times.Once);
        }

        #endregion

    }
}