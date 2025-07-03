using Eduva.Application.Features.Payments.Configurations;

namespace Eduva.Application.Test.Features.Payment.Configurations
{
    [TestFixture]
    public class PayOSConfigTests
    {

        #region PayOSConfig Tests

        [Test]
        public void Should_Access_ConfigName_Static_Property()
        {
            var configName = PayOSConfig.ConfigName;
            Assert.That(configName, Is.EqualTo("PayOS"));
        }

        [Test]
        public void Should_Set_And_Get_Config_Properties()
        {
            // Arrange
            var config = new PayOSConfig
            {
                PAYOS_CLIENT_ID = "client-id",
                PAYOS_API_KEY = "api-key",
                PAYOS_CHECKSUM_KEY = "checksum-key",
                ReturnUrl = "https://return.url",
                CancelUrl = "https://cancel.url"
            };

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(config.PAYOS_CLIENT_ID, Is.EqualTo("client-id"));
                Assert.That(config.PAYOS_API_KEY, Is.EqualTo("api-key"));
                Assert.That(config.PAYOS_CHECKSUM_KEY, Is.EqualTo("checksum-key"));
                Assert.That(config.ReturnUrl, Is.EqualTo("https://return.url"));
                Assert.That(config.CancelUrl, Is.EqualTo("https://cancel.url"));
            });
        }

        #endregion

    }
}