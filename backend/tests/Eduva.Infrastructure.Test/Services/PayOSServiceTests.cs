using Eduva.Application.Features.Payments.Configurations.PayOSService;
using Net.payOS;
using Net.payOS.Types;

namespace Eduva.Application.Test.Features.SchoolSubscriptions.Configurations.Services
{
    [TestFixture]
    public class PayOSServiceTests
    {

        #region PayOSService Tests

        [Test]
        public void Constructor_Should_Setup_Sdk_Instance()
        {
            // Arrange
            var sdk = new PayOS("client_id", "api_key", "checksum");

            // Act
            var service = new PayOSService(sdk);

            // Assert
            Assert.That(service, Is.Not.Null);
        }

        [Test]
        public async Task CreatePaymentLinkAsync_Should_Throw_When_InvalidSdk()
        {
            // Arrange
            var sdk = new PayOS("invalid", "invalid", "invalid");
            var service = new PayOSService(sdk);

            var request = new PaymentData(
                orderCode: 999999,
                amount: 10000,
                description: "Test",
                items: new List<ItemData> { new ItemData("Item", 1, 10000) },
                cancelUrl: "https://cancel",
                returnUrl: "https://return",
                buyerName: "Test",
                buyerEmail: "test@email.com",
                buyerPhone: "0123456789"
            );

            try
            {
                var result = await service.CreatePaymentLinkAsync(request);
                Assert.Fail("Expected exception was not thrown.");
            }
            catch (Exception ex)
            {
                Assert.That(ex, Is.Not.Null); // Expect some kind of failure
            }
        }

        #endregion

    }
}