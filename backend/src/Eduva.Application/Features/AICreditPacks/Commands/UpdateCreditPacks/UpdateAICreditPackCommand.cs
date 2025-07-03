using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.AICreditPacks.Commands.UpdateCreditPacks
{
    public class UpdateAICreditPackCommand : IRequest<Unit>
    {
        [JsonIgnore]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Credits { get; set; }
        public int BonusCredits { get; set; }
    }
}