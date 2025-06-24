using Eduva.Application.Exceptions.AICreditPack;
using Eduva.Application.Exceptions.CreditTransaction;
using Eduva.Application.Features.CreditTransactions.Commands;
using Eduva.Application.Features.Payments.Configurations;
using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.Extensions.Options;
using Moq;
using Net.payOS.Types;

namespace Eduva.Application.Test.Features.CreditTransactions.Commands;

[TestFixture]
public class CreateCreditPackPaymentLinkCommandHandlerTests
{
    private Mock<IUnitOfWork> _unitOfWorkMock = default!;
    private Mock<IPayOSService> _payOsServiceMock = default!;
    private PayOSConfig _payOSConfig = default!;
    private CreateCreditPackPaymentLinkCommandHandler _handler = default!;

    #region Setup

    [SetUp]
    public void SetUp()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _payOsServiceMock = new Mock<IPayOSService>();
        _payOSConfig = new PayOSConfig
        {
            CancelUrl = "https://cancel",
            ReturnUrl = "https://return"
        };

        var options = Options.Create(_payOSConfig);
        _handler = new CreateCreditPackPaymentLinkCommandHandler(
            _unitOfWorkMock.Object,
            _payOsServiceMock.Object,
            options
        );
    }

    #endregion

    #region Tests

    [Test]
    public async Task Handle_ShouldReturnPaymentLink_WhenValid()
    {
        // Arrange
        var creditPack = new AICreditPack
        {
            Id = 1,
            Name = "Pro Pack",
            Price = 50000,
            Status = EntityStatus.Active,
            Credits = 100,
            BonusCredits = 10
        };

        var repoMock = new Mock<IGenericRepository<AICreditPack, int>>();
        repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(creditPack);
        _unitOfWorkMock.Setup(u => u.GetRepository<AICreditPack, int>()).Returns(repoMock.Object);

        var transRepo = new Mock<IGenericRepository<PaymentTransaction, Guid>>();
        _unitOfWorkMock.Setup(u => u.GetRepository<PaymentTransaction, Guid>()).Returns(transRepo.Object);

        _payOsServiceMock
            .Setup(s => s.CreatePaymentLinkAsync(It.IsAny<PaymentData>()))
            .ReturnsAsync(new CreatePaymentResult(
                bin: "9704",
                accountNumber: "123456789",
                amount: 50000,
                description: "desc",
                orderCode: 123456789,
                currency: "VND",
                paymentLinkId: "paylink123",
                status: "PENDING",
                expiredAt: null,
                checkoutUrl: "https://checkout",
                qrCode: "qrcode123"
            ));

        var command = new CreateCreditPackPaymentLinkCommand
        {
            CreditPackId = 1,
            UserId = Guid.NewGuid()
        };

        // Act
        var (code, response) = await _handler.Handle(command, default);

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(code, Is.EqualTo(CustomCode.Success));
            Assert.That(response.CheckoutUrl, Is.EqualTo("https://checkout"));
            Assert.That(response.PaymentLinkId, Is.EqualTo("paylink123"));
            Assert.That(response.Amount, Is.EqualTo(50000));
            Assert.That(response.TransactionCode, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public void Handle_ShouldThrow_WhenCreditPackNotFound()
    {
        var repoMock = new Mock<IGenericRepository<AICreditPack, int>>();
        repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((AICreditPack?)null);
        _unitOfWorkMock.Setup(u => u.GetRepository<AICreditPack, int>()).Returns(repoMock.Object);

        var command = new CreateCreditPackPaymentLinkCommand
        {
            CreditPackId = 1,
            UserId = Guid.NewGuid()
        };

        Assert.ThrowsAsync<AICreditPackNotFoundException>(() => _handler.Handle(command, default));
    }

    [Test]
    public void Handle_ShouldThrow_WhenCreditPackInactive()
    {
        var creditPack = new AICreditPack
        {
            Id = 1,
            Name = "Pro",
            Price = 50000,
            Status = EntityStatus.Inactive
        };

        var repoMock = new Mock<IGenericRepository<AICreditPack, int>>();
        repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(creditPack);
        _unitOfWorkMock.Setup(u => u.GetRepository<AICreditPack, int>()).Returns(repoMock.Object);

        var command = new CreateCreditPackPaymentLinkCommand
        {
            CreditPackId = 1,
            UserId = Guid.NewGuid()
        };

        Assert.ThrowsAsync<AICreditPackNotActiveException>(() => _handler.Handle(command, default));
    }

    #endregion

}