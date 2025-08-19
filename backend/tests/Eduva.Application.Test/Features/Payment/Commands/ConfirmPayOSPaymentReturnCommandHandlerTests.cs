using Eduva.Application.Exceptions.AICreditPack;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Exceptions.CreditTransaction;
using Eduva.Application.Exceptions.PaymentTransaction;
using Eduva.Application.Exceptions.School;
using Eduva.Application.Exceptions.SchoolSubscription;
using Eduva.Application.Exceptions.SubscriptionPlan;
using Eduva.Application.Features.Payments.Commands;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Moq;
using Net.payOS.Types;

namespace Eduva.Application.Test.Features.Payment.Commands;

[TestFixture]
public class ConfirmPayOSPaymentReturnCommandHandlerTests
{
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<IPayOSService> _payOSServiceMock = default!;
    private Mock<IPaymentTransactionRepository> _transactionRepoMock = default!;
    private Mock<ISchoolRepository> _schoolRepoMock = default!;
    private Mock<ISubscriptionPlanRepository> _planRepoMock = default!;
    private Mock<ISchoolSubscriptionRepository> _subRepoMock = default!;
    private Mock<IAICreditPackRepository> _creditPackRepoMock = default!;
    private Mock<IGenericRepository<UserCreditTransaction, Guid>> _creditTransactionRepoMock = default!;
    private Mock<IGenericRepository<ApplicationUser, Guid>> _userRepoMock = default!;
    private Mock<IGenericRepository<AICreditPack, int>> _creditPackGenericRepoMock = default!;

    private ConfirmPayOSPaymentReturnCommandHandler _handler = default!;
    private Guid _userId = Guid.NewGuid();
    private Guid _transactionId = Guid.NewGuid();

    #region Setup

    [SetUp]
    public void Setup()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _payOSServiceMock = new Mock<IPayOSService>();
        _transactionRepoMock = new Mock<IPaymentTransactionRepository>();
        _schoolRepoMock = new Mock<ISchoolRepository>();
        _planRepoMock = new Mock<ISubscriptionPlanRepository>();
        _subRepoMock = new Mock<ISchoolSubscriptionRepository>();
        _creditPackRepoMock = new Mock<IAICreditPackRepository>();
        _creditTransactionRepoMock = new Mock<IGenericRepository<UserCreditTransaction, Guid>>();
        _userRepoMock = new Mock<IGenericRepository<ApplicationUser, Guid>>();
        _creditPackGenericRepoMock = new Mock<IGenericRepository<AICreditPack, int>>();

        _unitOfWorkMock.Setup(u => u.GetCustomRepository<IPaymentTransactionRepository>()).Returns(_transactionRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetCustomRepository<ISchoolRepository>()).Returns(_schoolRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetCustomRepository<ISubscriptionPlanRepository>()).Returns(_planRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetCustomRepository<ISchoolSubscriptionRepository>()).Returns(_subRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetCustomRepository<IAICreditPackRepository>()).Returns(_creditPackRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<UserCreditTransaction, Guid>()).Returns(_creditTransactionRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<ApplicationUser, Guid>()).Returns(_userRepoMock.Object);
        _unitOfWorkMock.Setup(x => x.GetRepository<AICreditPack, int>()).Returns(_creditPackGenericRepoMock.Object);

        _handler = new ConfirmPayOSPaymentReturnCommandHandler(_unitOfWorkMock.Object, _payOSServiceMock.Object);
    }

    private PaymentLinkInformation CreateMockPaymentInfo(string status)
    {
        return new PaymentLinkInformation(
            id: "test-id",
            orderCode: 123456,
            amount: 100000,
            amountPaid: 100000,
            amountRemaining: 0,
            status: status,
            createdAt: DateTime.UtcNow.ToString(),
            transactions: new List<Transaction>(),
            cancellationReason: null,
            canceledAt: null
        );
    }

    #endregion

    #region Tests

    [Test]
    public async Task Handle_Should_Handle_YearlySubscription()
    {
        var request = new ConfirmPayOSPaymentReturnCommand
        {
            Code = "00",
            Status = "PAID",
            OrderCode = 22,
            Id = "txn"
        };

        var transaction = new PaymentTransaction
        {
            Id = _transactionId,
            UserId = _userId,
            TransactionCode = "22",
            PaymentStatus = PaymentStatus.Pending,
            PaymentPurpose = PaymentPurpose.SchoolSubscription,
            PaymentItemId = 1,
            Amount = 999999,
            BillingCycle = BillingCycle.Yearly
        };

        var school = new School { Id = 1, Status = EntityStatus.Active };
        var plan = new SubscriptionPlan { Id = 1, PriceMonthly = 100000 };
        var oldSub = new SchoolSubscription { Id = Guid.NewGuid(), SubscriptionStatus = SubscriptionStatus.Expired };

        var paymentInfo = CreateMockPaymentInfo("PAID");
        _payOSServiceMock.Setup(x => x.GetPaymentLinkInformationAsync(22)).ReturnsAsync(paymentInfo);

        _transactionRepoMock.Setup(x => x.GetByTransactionCodeAsync("22", It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        _schoolRepoMock.Setup(x => x.GetByUserIdAsync(_userId)).ReturnsAsync(school);
        _planRepoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(plan);
        _subRepoMock.Setup(x => x.GetLatestPaidBySchoolIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(oldSub);
        _subRepoMock.Setup(x => x.AddAsync(It.IsAny<SchoolSubscription>())).Returns(Task.CompletedTask);

        await _handler.Handle(request, CancellationToken.None);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Exactly(2));
        _payOSServiceMock.Verify(x => x.GetPaymentLinkInformationAsync(22), Times.Once);
    }

    [Test]
    public void Handle_ShouldThrow_WhenTransactionNotFound()
    {
        var request = new ConfirmPayOSPaymentReturnCommand
        {
            Code = "00",
            Status = "PAID",
            OrderCode = 1234,
            Id = "txn"
        };

        _transactionRepoMock
            .Setup(x => x.GetByTransactionCodeAsync("1234", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentTransaction?)null);

        Assert.ThrowsAsync<PaymentTransactionNotFoundException>(() => _handler.Handle(request, CancellationToken.None));
    }

    [Test]
    public void Handle_ShouldThrow_WhenPaymentAlreadyConfirmed()
    {
        var request = new ConfirmPayOSPaymentReturnCommand
        {
            Code = "00",
            Status = "PAID",
            OrderCode = 1,
            Id = "txn"
        };

        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentStatus = PaymentStatus.Paid,
            PaymentPurpose = PaymentPurpose.SchoolSubscription,
            UserId = _userId
        };

        _transactionRepoMock.Setup(r => r.GetByTransactionCodeAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(transaction);

        Assert.ThrowsAsync<PaymentAlreadyConfirmedException>(() => _handler.Handle(request, CancellationToken.None));
    }

    [Test]
    public void Handle_ShouldThrow_WhenPayOSStatusNotPaid()
    {
        var request = new ConfirmPayOSPaymentReturnCommand
        {
            Code = "00",
            Status = "PAID",
            OrderCode = 1,
            Id = "txn"
        };

        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentStatus = PaymentStatus.Pending,
            PaymentPurpose = PaymentPurpose.CreditPackage,
            UserId = _userId,
            TransactionCode = "1"
        };

        var paymentInfo = CreateMockPaymentInfo("FAILED");
        _payOSServiceMock.Setup(x => x.GetPaymentLinkInformationAsync(1)).ReturnsAsync(paymentInfo);

        _transactionRepoMock
            .Setup(x => x.GetByTransactionCodeAsync("1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        Assert.ThrowsAsync<PaymentFailedException>(() => _handler.Handle(request, CancellationToken.None));

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Test]
    public void Handle_ShouldThrow_WhenPayOSApiCallFails()
    {
        var request = new ConfirmPayOSPaymentReturnCommand
        {
            Code = "00",
            Status = "PAID",
            OrderCode = 1,
            Id = "txn"
        };

        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentStatus = PaymentStatus.Pending,
            PaymentPurpose = PaymentPurpose.CreditPackage,
            UserId = _userId,
            TransactionCode = "1"
        };

        _payOSServiceMock.Setup(x => x.GetPaymentLinkInformationAsync(1))
            .ThrowsAsync(new Exception("PayOS API error"));

        _transactionRepoMock
            .Setup(x => x.GetByTransactionCodeAsync("1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        Assert.ThrowsAsync<PaymentFailedException>(() => _handler.Handle(request, CancellationToken.None));

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_Should_Handle_SchoolSubscription()
    {
        var request = new ConfirmPayOSPaymentReturnCommand
        {
            Code = "00",
            Status = "PAID",
            OrderCode = 1,
            Id = "txn1"
        };

        var transaction = new PaymentTransaction
        {
            Id = _transactionId,
            UserId = _userId,
            TransactionCode = "1",
            PaymentStatus = PaymentStatus.Pending,
            PaymentPurpose = PaymentPurpose.SchoolSubscription,
            PaymentItemId = 1,
            BillingCycle = BillingCycle.Monthly
        };

        var school = new School { Id = 1, Status = EntityStatus.Inactive };
        var plan = new SubscriptionPlan { Id = 1, PriceMonthly = 100000 };
        var oldSub = new SchoolSubscription { Id = new Guid("1a1a1a1a-1a1a-1a1a-1a1a-1a1a1a1a1a1a"), SubscriptionStatus = SubscriptionStatus.Active };

        var paymentInfo = CreateMockPaymentInfo("PAID");
        _payOSServiceMock.Setup(x => x.GetPaymentLinkInformationAsync(1)).ReturnsAsync(paymentInfo);

        _transactionRepoMock.Setup(x => x.GetByTransactionCodeAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        _schoolRepoMock.Setup(x => x.GetByUserIdAsync(_userId)).ReturnsAsync(school);
        _planRepoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(plan);
        _subRepoMock.Setup(x => x.GetLatestPaidBySchoolIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(oldSub);
        _subRepoMock.Setup(x => x.AddAsync(It.IsAny<SchoolSubscription>())).Returns(Task.CompletedTask);

        await _handler.Handle(request, CancellationToken.None);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Exactly(2));
        _payOSServiceMock.Verify(x => x.GetPaymentLinkInformationAsync(1), Times.Once);
    }

    [Test]
    public async Task Handle_Should_Handle_CreditPackage()
    {
        var request = new ConfirmPayOSPaymentReturnCommand
        {
            Code = "00",
            Status = "PAID",
            OrderCode = 9,
            Id = "txn2"
        };

        var transaction = new PaymentTransaction
        {
            Id = _transactionId,
            UserId = _userId,
            TransactionCode = "9",
            PaymentStatus = PaymentStatus.Pending,
            PaymentPurpose = PaymentPurpose.CreditPackage,
            PaymentItemId = 10
        };

        var pack = new AICreditPack { Id = 10, Credits = 100, BonusCredits = 50 };
        var user = new ApplicationUser { Id = _userId, TotalCredits = 0 };

        var paymentInfo = CreateMockPaymentInfo("PAID");
        _payOSServiceMock.Setup(x => x.GetPaymentLinkInformationAsync(9)).ReturnsAsync(paymentInfo);

        _transactionRepoMock.Setup(x => x.GetByTransactionCodeAsync("9", It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        _creditPackRepoMock.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(pack);
        _creditTransactionRepoMock.Setup(x => x.AddAsync(It.IsAny<UserCreditTransaction>())).Returns(Task.CompletedTask);
        _userRepoMock.Setup(x => x.GetByIdAsync(_userId)).ReturnsAsync(user);
        _creditPackGenericRepoMock.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(pack);

        await _handler.Handle(request, CancellationToken.None);

        Assert.That(user.TotalCredits, Is.EqualTo(150));
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Exactly(3));
        _payOSServiceMock.Verify(x => x.GetPaymentLinkInformationAsync(9), Times.Once);
    }

    [Test]
    public void Handle_ShouldThrow_WhenPurposeIsUnknown()
    {
        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentStatus = PaymentStatus.Pending,
            PaymentPurpose = (PaymentPurpose)999,
            UserId = _userId
        };

        var request = new ConfirmPayOSPaymentReturnCommand
        {
            Code = "00",
            Status = "PAID",
            OrderCode = 888,
            Id = "invalid"
        };

        var paymentInfo = CreateMockPaymentInfo("PAID");
        _payOSServiceMock.Setup(x => x.GetPaymentLinkInformationAsync(888)).ReturnsAsync(paymentInfo);

        _transactionRepoMock
            .Setup(x => x.GetByTransactionCodeAsync("888", It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        Assert.ThrowsAsync<InvalidPaymentPurposeException>(() => _handler.Handle(request, CancellationToken.None));
    }

    [Test]
    public void Handle_ShouldThrow_WhenSchoolNotFound()
    {
        var request = new ConfirmPayOSPaymentReturnCommand
        {
            Code = "00",
            Status = "PAID",
            OrderCode = 9999,
            Id = "txn"
        };

        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentStatus = PaymentStatus.Pending,
            PaymentPurpose = PaymentPurpose.SchoolSubscription,
            UserId = _userId,
            PaymentItemId = 1,
            BillingCycle = BillingCycle.Monthly
        };

        var paymentInfo = CreateMockPaymentInfo("PAID");
        _payOSServiceMock.Setup(x => x.GetPaymentLinkInformationAsync(9999)).ReturnsAsync(paymentInfo);

        _transactionRepoMock.Setup(x => x.GetByTransactionCodeAsync("9999", It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        _schoolRepoMock.Setup(x => x.GetByUserIdAsync(_userId)).ReturnsAsync((School?)null);

        Assert.ThrowsAsync<SchoolNotFoundException>(() => _handler.Handle(request, CancellationToken.None));
    }

    [Test]
    public void Handle_ShouldThrow_WhenPlanNotFound()
    {
        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentStatus = PaymentStatus.Pending,
            PaymentPurpose = PaymentPurpose.SchoolSubscription,
            UserId = _userId,
            PaymentItemId = 999,
            BillingCycle = BillingCycle.Monthly
        };

        var request = new ConfirmPayOSPaymentReturnCommand
        {
            Code = "00",
            Status = "PAID",
            OrderCode = 888,
            Id = "txn"
        };

        var paymentInfo = CreateMockPaymentInfo("PAID");
        _payOSServiceMock.Setup(x => x.GetPaymentLinkInformationAsync(888)).ReturnsAsync(paymentInfo);

        _transactionRepoMock.Setup(r => r.GetByTransactionCodeAsync("888", It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        _schoolRepoMock.Setup(s => s.GetByUserIdAsync(_userId)).ReturnsAsync(new School { Id = 1 });
        _planRepoMock.Setup(p => p.GetByIdAsync(999)).ReturnsAsync((SubscriptionPlan?)null);

        Assert.ThrowsAsync<PlanNotFoundException>(() => _handler.Handle(request, CancellationToken.None));
    }

    [Test]
    public void Handle_ShouldThrow_WhenCreditPackNotFound()
    {
        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentStatus = PaymentStatus.Pending,
            PaymentPurpose = PaymentPurpose.CreditPackage,
            UserId = _userId,
            PaymentItemId = 123
        };

        var request = new ConfirmPayOSPaymentReturnCommand
        {
            Code = "00",
            Status = "PAID",
            OrderCode = 111,
            Id = "txn"
        };

        var paymentInfo = CreateMockPaymentInfo("PAID");
        _payOSServiceMock.Setup(x => x.GetPaymentLinkInformationAsync(111)).ReturnsAsync(paymentInfo);

        _transactionRepoMock.Setup(r => r.GetByTransactionCodeAsync("111", It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        _creditPackGenericRepoMock.Setup(x => x.GetByIdAsync(123)).ReturnsAsync((AICreditPack?)null);

        Assert.ThrowsAsync<AICreditPackNotFoundException>(() => _handler.Handle(request, CancellationToken.None));
    }

    [Test]
    public void Handle_ShouldThrow_WhenUserNotFound()
    {
        var request = new ConfirmPayOSPaymentReturnCommand
        {
            Code = "00",
            Status = "PAID",
            OrderCode = 9,
            Id = "txn-9"
        };

        var transaction = new PaymentTransaction
        {
            Id = _transactionId,
            UserId = _userId,
            TransactionCode = "9",
            PaymentStatus = PaymentStatus.Pending,
            PaymentPurpose = PaymentPurpose.CreditPackage,
            PaymentItemId = 10,
            Amount = 100000
        };

        var paymentInfo = CreateMockPaymentInfo("PAID");
        _payOSServiceMock.Setup(x => x.GetPaymentLinkInformationAsync(9)).ReturnsAsync(paymentInfo);

        _transactionRepoMock
            .Setup(x => x.GetByTransactionCodeAsync("9", It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        _creditPackGenericRepoMock
            .Setup(x => x.GetByIdAsync(10))
            .ReturnsAsync(new AICreditPack
            {
                Id = 10,
                Credits = 100,
                BonusCredits = 0
            });

        _userRepoMock
            .Setup(x => x.GetByIdAsync(_userId))
            .ReturnsAsync((ApplicationUser?)null);

        Assert.ThrowsAsync<UserNotExistsException>(() =>
            _handler.Handle(request, CancellationToken.None));
    }

    #endregion
}