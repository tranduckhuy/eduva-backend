using Eduva.Application.Exceptions.AICreditPack;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;

namespace Eduva.Application.Features.AICreditPacks.Commands.DeleteCreditPacks
{
    public class DeleteAICreditPackCommandHandler : IRequestHandler<DeleteAICreditPackCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeleteAICreditPackCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(DeleteAICreditPackCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<AICreditPack, int>();
            var pack = await repo.GetByIdAsync(request.Id)
                       ?? throw new AICreditPackNotFoundException();

            if (pack.Status != EntityStatus.Archived)
            {
                throw new AICreditPackMustBeArchivedException();
            }

            repo.Remove(pack);
            await _unitOfWork.CommitAsync();

            return Unit.Value;
        }
    }
}