using Eduva.Application.Exceptions.AICreditPack;
using Eduva.Application.Exceptions.Auth;
using Eduva.Application.Exceptions.CreditTransaction;
using Eduva.Application.Exceptions.School;
using Eduva.Application.Exceptions.SubscriptionPlan;
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
        _creditPackGenericRepoMock = new Mock<IGenericRepository<AICreditPack, int>>();
        _unitOfWorkMock.Setup(x => x.GetRepository<AICreditPack, int>()).Returns(_creditPackGenericRepoMock.Object);


        _handler = new ConfirmPayOSPaymentReturnCommandHandler(_unitOfWorkMock.Object);
    }

    #endregion

    #region Tests

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
            PaymentItemId = 1
        };

        var school = new School { Id = 1, Status = EntityStatus.Inactive };
        var plan = new SubscriptionPlan { Id = 1, PriceMonthly = 100000 };
        var oldSub = new SchoolSubscription { Id = new Guid("1a1a1a1a-1a1a-1a1a-1a1a-1a1a1a1a1a1a"), SubscriptionStatus = SubscriptionStatus.Active };

        _transactionRepoMock.Setup(x => x.GetByTransactionCodeAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        _schoolRepoMock.Setup(x => x.GetByUserIdAsync(_userId)).ReturnsAsync(school);
        _planRepoMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(plan);
        _subRepoMock.Setup(x => x.GetLatestPaidBySchoolIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(oldSub);
        _subRepoMock.Setup(x => x.AddAsync(It.IsAny<SchoolSubscription>())).Returns(Task.CompletedTask);


        await _handler.Handle(request, CancellationToken.None);

        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Exactly(2));
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

        _transactionRepoMock.Setup(x => x.GetByTransactionCodeAsync("9", It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        _creditPackRepoMock.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(pack);
        _creditTransactionRepoMock.Setup(x => x.AddAsync(It.IsAny<UserCreditTransaction>())).Returns(Task.CompletedTask);
        _userRepoMock.Setup(x => x.GetByIdAsync(_userId)).ReturnsAsync(user);
        _creditPackGenericRepoMock.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(pack);

        await _handler.Handle(request, CancellationToken.None);

        Assert.That(user.TotalCredits, Is.EqualTo(150));
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Exactly(3));
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
            PaymentItemId = 1
        };

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
            PaymentItemId = 999
        };

        var request = new ConfirmPayOSPaymentReturnCommand
        {
            Code = "00",
            Status = "PAID",
            OrderCode = 888,
            Id = "txn"
        };

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

        _transactionRepoMock.Setup(r => r.GetByTransactionCodeAsync("111", It.IsAny<CancellationToken>())).ReturnsAsync(transaction);
        _creditPackRepoMock.Setup(p => p.GetByIdAsync(123)).ReturnsAsync((AICreditPack?)null);
        _creditPackGenericRepoMock.Setup(x => x.GetByIdAsync(10)).ReturnsAsync((AICreditPack?)null);

        Assert.ThrowsAsync<AICreditPackNotFoundException>(() => _handler.Handle(request, CancellationToken.None));
    }

    [Test]
    public void Handle_ShouldThrow_WhenUserNotFound()
    {
        // Arrange
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

        // Act & Assert
        Assert.ThrowsAsync<UserNotExistsException>(() =>
            _handler.Handle(request, CancellationToken.None));
    }

    #endregion

}