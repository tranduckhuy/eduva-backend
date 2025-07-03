using MediatR;

namespace Eduva.Application.Features.AICreditPacks.Commands.CreateCreditPacks
{
    public class CreateAICreditPackCommand : IRequest<Unit>
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Credits { get; set; }
        public int BonusCredits { get; set; } = 0;
    }
}