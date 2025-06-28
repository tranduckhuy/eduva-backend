using Eduva.Application.Common.Models;
using Eduva.Application.Features.Payments.Queries;
using Eduva.Application.Features.Payments.Specifications;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Moq;

namespace Eduva.Application.Test.Features.Payment.Queries;

[TestFixture]
public class GetMyPaymentsQueryHandlerTests
{
    private Mock<IPaymentTransactionRepository> _repoMock = null!;
    private GetMyPaymentsQueryHandler _handler = null!;

    #region GetMyPaymentsQueryHandlerTests Setup

    [SetUp]
    public void SetUp()
    {
        _repoMock = new Mock<IPaymentTransactionRepository>();
        _handler = new GetMyPaymentsQueryHandler(_repoMock.Object);
    }

    #endregion

    #region GetMyPaymentsQueryHandler Tests

    [Test]
    public async Task Handle_ShouldReturnMappedPaginationResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var specParam = new MyPaymentSpecParam
        {
            UserId = userId,
            PageIndex = 1,
            PageSize = 10
        };

        var request = new GetMyPaymentsQuery(specParam);

        var transactions = new List<PaymentTransaction>
        {
            new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Amount = 500_000,
                PaymentPurpose = PaymentPurpose.SchoolSubscription,
                PaymentStatus = PaymentStatus.Paid,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var pagination = new Pagination<PaymentTransaction>(
            pageIndex: 1,
            pageSize: 10,
            count: 1,
            data: transactions
        );

        _repoMock.Setup(r => r.GetWithSpecAsync(It.IsAny<MyPaymentSpecification>()))
                 .ReturnsAsync(pagination);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Data, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(result.Data.First().Amount, Is.EqualTo(500_000));
            Assert.That(result.Count, Is.EqualTo(1));
        });
        Assert.Multiple(() =>
        {
            Assert.That(result.PageIndex, Is.EqualTo(1));
            Assert.That(result.PageSize, Is.EqualTo(10));
        });
    }

    [Test]
    public async Task Handle_ShouldReturnEmpty_WhenNoMatchingTransactions()
    {
        var request = new GetMyPaymentsQuery(new MyPaymentSpecParam
        {
            UserId = Guid.NewGuid()
        });

        var emptyPagination = new Pagination<PaymentTransaction>(1, 10, 0, []);

        _repoMock.Setup(x => x.GetWithSpecAsync(It.IsAny<MyPaymentSpecification>()))
                 .ReturnsAsync(emptyPagination);

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Data, Is.Empty);
            Assert.That(result.Count, Is.EqualTo(0));
        });
        Assert.Multiple(() =>
        {
            Assert.That(result.PageIndex, Is.EqualTo(1));
            Assert.That(result.PageSize, Is.EqualTo(10));
        });
    }

    #endregion

}