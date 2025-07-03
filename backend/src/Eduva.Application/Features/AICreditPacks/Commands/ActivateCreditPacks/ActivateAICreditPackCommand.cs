using MediatR;

namespace Eduva.Application.Features.AICreditPacks.Commands.ActivateCreditPacks
{
    public class ActivateAICreditPackCommand : IRequest<Unit>
    {
        public int Id { get; set; }

        public ActivateAICreditPackCommand(int id)
        {
            Id = id;
        }
    }
}
