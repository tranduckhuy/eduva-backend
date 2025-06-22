//using Eduva.Application.Exceptions.School;
//using Eduva.Application.Exceptions.SchoolSubscription;
//using Eduva.Application.Features.SchoolSubscriptions.Commands;
//using Eduva.Application.Interfaces;
//using Eduva.Application.Interfaces.Repositories;
//using Eduva.Domain.Entities;
//using Eduva.Domain.Enums;
//using MediatR;
//using Moq;

//namespace Eduva.Application.Test.SchoolSubscriptions.Commands
//{
//    [TestFixture]
//    public class ConfirmPayOSPaymentReturnCommandHandlerTests
//    {
//        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
//        private Mock<ISchoolSubscriptionRepository> _subRepoMock = null!;
//        private Mock<IGenericRepository<School, int>> _schoolRepoMock = null!;
//        private ConfirmPayOSPaymentReturnCommandHandler _handler = null!;

//        #region ConfirmPayOSPaymentReturnCommandHandlerTests Setup

//        [SetUp]
//        public void Setup()
//        {
//            _unitOfWorkMock = new Mock<IUnitOfWork>();
//            _subRepoMock = new Mock<ISchoolSubscriptionRepository>();
//            _schoolRepoMock = new Mock<IGenericRepository<School, int>>();

//            _unitOfWorkMock.Setup(x => x.GetCustomRepository<ISchoolSubscriptionRepository>())
//                           .Returns(_subRepoMock.Object);

//            _unitOfWorkMock.Setup(x => x.GetRepository<School, int>())
//                           .Returns(_schoolRepoMock.Object);

//            _handler = new ConfirmPayOSPaymentReturnCommandHandler(_unitOfWorkMock.Object);
//        }

//        #endregion

//        #region ConfirmPayOSPaymentReturnCommandHandler Tests

//        [Test]
//        public async Task Should_Activate_Subscription_Without_Changing_School_Status_When_Already_Active()
//        {
//            var cmd = new ConfirmPayOSPaymentReturnCommand { Code = "00", Status = "PAID", OrderCode = 456 };

//            var newSub = new SchoolSubscription
//            {
//                Id = 3,
//                SchoolId = 20,
//                PaymentStatus = PaymentStatus.Pending,
//                SubscriptionStatus = SubscriptionStatus.Peding
//            };

//            var school = new School
//            {
//                Id = 20,
//                Status = EntityStatus.Active
//            };

//            _subRepoMock.Setup(x => x.FindByTransactionIdAsync("456")).ReturnsAsync(newSub);
//            _subRepoMock.Setup(x => x.GetActiveSubscriptionBySchoolIdAsync(20)).ReturnsAsync((SchoolSubscription?)null);
//            _schoolRepoMock.Setup(x => x.GetByIdAsync(20)).ReturnsAsync(school);

//            var result = await _handler.Handle(cmd, CancellationToken.None);

//            Assert.Multiple(() =>
//            {
//                Assert.That(result, Is.EqualTo(Unit.Value));
//                Assert.That(newSub.SubscriptionStatus, Is.EqualTo(SubscriptionStatus.Active));
//                Assert.That(newSub.PaymentStatus, Is.EqualTo(PaymentStatus.Paid));
//                Assert.That(school.Status, Is.EqualTo(EntityStatus.Active));
//            });

//            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
//        }

//        [Test]
//        public void Should_Throw_PaymentFailed_When_CodeNot00_Or_StatusNotPaid()
//        {
//            var cmd = new ConfirmPayOSPaymentReturnCommand { Code = "01", Status = "FAILED", OrderCode = 123 };

//            Assert.ThrowsAsync<PaymentFailedException>(() => _handler.Handle(cmd, CancellationToken.None));
//        }

//        [Test]
//        public void Should_Throw_NotFound_When_SubscriptionDoesNotExist()
//        {
//            var cmd = new ConfirmPayOSPaymentReturnCommand { Code = "00", Status = "PAID", OrderCode = 123 };

//            _subRepoMock.Setup(x => x.FindByTransactionIdAsync("123"))
//                        .ReturnsAsync((SchoolSubscription?)null);

//            Assert.ThrowsAsync<SchoolSubscriptionNotFoundException>(() => _handler.Handle(cmd, CancellationToken.None));
//        }

//        [Test]
//        public void Should_Throw_When_SubscriptionAlreadyPaid()
//        {
//            var cmd = new ConfirmPayOSPaymentReturnCommand { Code = "00", Status = "PAID", OrderCode = 123 };
//            var paidSub = new SchoolSubscription
//            {
//                SchoolId = 1,
//                PaymentStatus = PaymentStatus.Paid
//            };

//            _subRepoMock.Setup(x => x.FindByTransactionIdAsync("123"))
//                        .ReturnsAsync(paidSub);

//            Assert.ThrowsAsync<PaymentAlreadyConfirmedException>(() => _handler.Handle(cmd, CancellationToken.None));
//        }

//        [Test]
//        public void Should_Throw_When_SchoolNotFound()
//        {
//            var cmd = new ConfirmPayOSPaymentReturnCommand { Code = "00", Status = "PAID", OrderCode = 123 };
//            var sub = new SchoolSubscription
//            {
//                Id = 1,
//                SchoolId = 2,
//                PaymentStatus = PaymentStatus.Pending,
//                SubscriptionStatus = SubscriptionStatus.Peding
//            };

//            _subRepoMock.Setup(x => x.FindByTransactionIdAsync("123")).ReturnsAsync(sub);
//            _subRepoMock.Setup(x => x.GetActiveSubscriptionBySchoolIdAsync(2)).ReturnsAsync((SchoolSubscription?)null);
//            _schoolRepoMock.Setup(x => x.GetByIdAsync(2)).ReturnsAsync((School?)null);

//            Assert.ThrowsAsync<SchoolNotFoundException>(() => _handler.Handle(cmd, CancellationToken.None));
//        }

//        [Test]
//        public async Task Should_Expire_Old_Subscription_And_Activate_New()
//        {
//            var cmd = new ConfirmPayOSPaymentReturnCommand { Code = "00", Status = "PAID", OrderCode = 123 };
//            var now = DateTimeOffset.UtcNow;

//            var oldSub = new SchoolSubscription
//            {
//                Id = 1,
//                SchoolId = 10,
//                PaymentStatus = PaymentStatus.Paid,
//                SubscriptionStatus = SubscriptionStatus.Active,
//                EndDate = now.AddDays(10)
//            };

//            var newSub = new SchoolSubscription
//            {
//                Id = 2,
//                SchoolId = 10,
//                PaymentStatus = PaymentStatus.Pending,
//                SubscriptionStatus = SubscriptionStatus.Peding
//            };

//            var school = new School
//            {
//                Id = 10,
//                Status = EntityStatus.Inactive
//            };

//            _subRepoMock.Setup(x => x.FindByTransactionIdAsync("123")).ReturnsAsync(newSub);
//            _subRepoMock.Setup(x => x.GetActiveSubscriptionBySchoolIdAsync(10)).ReturnsAsync(oldSub);
//            _schoolRepoMock.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(school);

//            // Act
//            var result = await _handler.Handle(cmd, CancellationToken.None);

//            Assert.Multiple(() =>
//            {
//                // Assert
//                Assert.That(result, Is.EqualTo(Unit.Value));
//                Assert.That(oldSub.SubscriptionStatus, Is.EqualTo(SubscriptionStatus.Expired));
//                Assert.That(newSub.SubscriptionStatus, Is.EqualTo(SubscriptionStatus.Active));
//                Assert.That(newSub.PaymentStatus, Is.EqualTo(PaymentStatus.Paid));
//                Assert.That(school.Status, Is.EqualTo(EntityStatus.Active));
//            });
//            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
//        }

//        #endregion

//    }
//}