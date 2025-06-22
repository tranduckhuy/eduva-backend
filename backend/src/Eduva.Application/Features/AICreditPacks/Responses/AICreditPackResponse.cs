using Eduva.Domain.Enums;

namespace Eduva.Application.Features.AICreditPacks.Responses
{
    public class AICreditPackResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Credits { get; set; }
        public int BonusCredits { get; set; }
        public EntityStatus Status { get; set; }
    }
}