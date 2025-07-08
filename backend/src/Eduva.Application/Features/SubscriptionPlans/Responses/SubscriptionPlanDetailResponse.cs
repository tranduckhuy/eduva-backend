using System.Text.Json.Serialization;

namespace Eduva.Application.Features.SubscriptionPlans.Responses
{
    public class SubscriptionPlanDetailResponse : SubscriptionPlanResponse
    {
        [JsonPropertyOrder(12)]
        public int NumberOfSchoolsUsing { get; set; }
    }
}