namespace Eduva.Application.Features.StorageQuota
{
    public class StorageQuotaDetailsResponse
    {
        public long UsedBytes { get; set; }
        public long LimitBytes { get; set; }
        public long RemainingBytes { get; set; }
        public double UsedGB { get; set; }
        public double LimitGB { get; set; }
        public double RemainingGB { get; set; }
        public double UsagePercentage { get; set; }
    }
}
