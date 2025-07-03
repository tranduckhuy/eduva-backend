using MediatR;

namespace Eduva.Application.Features.AICreditPacks.Commands.DeleteCreditPacks
{
    public class DeleteAICreditPackCommand : IRequest<Unit>
    {
        public int Id { get; set; }

        public DeleteAICreditPackCommand(int id)
        {
            Id = id;
        }
    }
}