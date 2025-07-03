using Eduva.Application.Exceptions.AICreditPack;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;

namespace Eduva.Application.Features.AICreditPacks.Commands.ArchiveCreditPacks
{
    public class ArchiveAICreditPackCommandHandler : IRequestHandler<ArchiveAICreditPackCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ArchiveAICreditPackCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(ArchiveAICreditPackCommand request, CancellationToken cancellationToken)
        {
            var creditPackRepo = _unitOfWork.GetRepository<AICreditPack, int>();

            var pack = await creditPackRepo.GetByIdAsync(request.Id) ?? throw new AICreditPackNotFoundException();

            if (pack.Status == EntityStatus.Archived)
            {
                throw new AICreditPackAlreadyArchivedException();
            }

            pack.Status = EntityStatus.Archived;
            pack.LastModifiedAt = DateTimeOffset.UtcNow;

            creditPackRepo.Update(pack);
            await _unitOfWork.CommitAsync();

            return Unit.Value;
        }
    }
}