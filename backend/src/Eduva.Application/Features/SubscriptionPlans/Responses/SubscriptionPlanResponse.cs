using Eduva.Domain.Enums;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.SubscriptionPlans.Responses
{
    public class SubscriptionPlanResponse
    {
        [JsonPropertyOrder(1)]
        public int Id { get; set; }
        [JsonPropertyOrder(2)]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyOrder(3)]
        public string? Description { get; set; }
        [JsonPropertyOrder(4)]
        public int MaxUsers { get; set; }
        [JsonPropertyOrder(5)]
        public decimal StorageLimitGB { get; set; }
        [JsonPropertyOrder(6)]
        public decimal PriceMonthly { get; set; }
        [JsonPropertyOrder(7)]
        public decimal PricePerYear { get; set; }
        [JsonPropertyOrder(8)]
        public bool IsRecommended { get; set; } = false;
        [JsonPropertyOrder(9)]
        public EntityStatus Status { get; set; }
        [JsonPropertyOrder(10)]
        public DateTimeOffset CreatedAt { get; set; }
        [JsonPropertyOrder(11)]
        public DateTimeOffset? LastModifiedAt { get; set; }
    }
}