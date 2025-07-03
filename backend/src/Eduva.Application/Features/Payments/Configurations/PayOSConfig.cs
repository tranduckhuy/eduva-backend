namespace Eduva.Application.Features.Payments.Configurations
{
    public class PayOSConfig
    {
        public static string ConfigName => "PayOS";
        public string PAYOS_CLIENT_ID { get; set; } = string.Empty;
        public string PAYOS_API_KEY { get; set; } = string.Empty;
        public string PAYOS_CHECKSUM_KEY { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
    }
}