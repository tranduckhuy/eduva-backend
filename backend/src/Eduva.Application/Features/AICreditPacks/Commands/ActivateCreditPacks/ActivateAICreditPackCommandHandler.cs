using Eduva.Application.Exceptions.AICreditPack;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;

namespace Eduva.Application.Features.AICreditPacks.Commands.ActivateCreditPacks
{
    public class ActivateAICreditPackCommandHandler : IRequestHandler<ActivateAICreditPackCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ActivateAICreditPackCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(ActivateAICreditPackCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<AICreditPack, int>();
            var pack = await repo.GetByIdAsync(request.Id)
                       ?? throw new AICreditPackNotFoundException();

            if (pack.Status == EntityStatus.Active)
            {
                throw new AICreditPackAlreadyActiveException();
            }

            pack.Status = EntityStatus.Active;
            pack.LastModifiedAt = DateTimeOffset.UtcNow;

            repo.Update(pack);
            await _unitOfWork.CommitAsync();

            return Unit.Value;
        }
    }
}