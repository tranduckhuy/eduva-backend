using Eduva.Application.Exceptions.PaymentTransaction;
using Eduva.Application.Exceptions.SchoolSubscription;
using Eduva.Application.Features.Payments.Commands;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Moq;

namespace Eduva.Application.Test.Features.Payment.Commands;

[TestFixture]
public class ConfirmPayOSPaymentReturnCommandHandlerTests
{
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<IPaymentTransactionRepository> _transactionRepoMock = default!;
    private ConfirmPayOSPaymentReturnCommandHandler _handler = default!;
    private Guid _userId = Guid.NewGuid();

    #region Setup

    [SetUp]
    public void Setup()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _transactionRepoMock = new Mock<IPaymentTransactionRepository>();

        _unitOfWorkMock
            .Setup(u => u.GetCustomRepository<IPaymentTransactionRepository>())
            .Returns(_transactionRepoMock.Object);

        _handler = new ConfirmPayOSPaymentReturnCommandHandler(_unitOfWorkMock.Object);
    }

    #endregion

    #region Tests

    [Test]
    public void Handle_ShouldThrow_WhenCodeNot00OrStatusNotPaid()
    {
        var request = new ConfirmPayOSPaymentReturnCommand
        {
            Code = "01",
            Status = "FAILED",
            OrderCode = 123,
            Id = "some-id"
        };

        Assert.ThrowsAsync<PaymentFailedException>(() => _handler.Handle(request, CancellationToken.None));
    }

    [Test]
    public void Handle_ShouldThrow_WhenTransactionNotFound()
    {
        var request = new ConfirmPayOSPaymentReturnCommand
        {
            Code = "00",
            Status = "PAID",
            OrderCode = 123,
            Id = "abc123"
        };

        _transactionRepoMock
            .Setup(r => r.GetByTransactionCodeAsync("123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentTransaction?)null);

        Assert.ThrowsAsync<PaymentTransactionNotFoundException>(() =>
            _handler.Handle(request, CancellationToken.None));
    }

    [Test]
    public void Handle_ShouldThrow_WhenAlreadyConfirmed()
    {
        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentStatus = PaymentStatus.Paid,
            PaymentPurpose = PaymentPurpose.SchoolSubscription,
            UserId = _userId,
            PaymentItemId = 1
        };

        var request = new ConfirmPayOSPaymentReturnCommand
        {
            Code = "00",
            Status = "PAID",
            OrderCode = 999,
            Id = "zzz"
        };

        _transactionRepoMock
            .Setup(r => r.GetByTransactionCodeAsync("999", It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        Assert.ThrowsAsync<PaymentAlreadyConfirmedException>(() =>
            _handler.Handle(request, CancellationToken.None));
    }

    #endregion

}