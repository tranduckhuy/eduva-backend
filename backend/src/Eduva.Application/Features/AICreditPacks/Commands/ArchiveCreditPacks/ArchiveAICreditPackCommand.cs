using MediatR;

namespace Eduva.Application.Features.AICreditPacks.Commands.ArchiveCreditPacks
{
    public class ArchiveAICreditPackCommand : IRequest<Unit>
    {
        public int Id { get; set; }

        public ArchiveAICreditPackCommand(int id)
        {
            Id = id;
        }
    }
}