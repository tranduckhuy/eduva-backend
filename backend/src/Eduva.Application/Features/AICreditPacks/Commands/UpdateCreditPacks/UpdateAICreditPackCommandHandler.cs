using Eduva.Application.Exceptions.AICreditPack;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using MediatR;

namespace Eduva.Application.Features.AICreditPacks.Commands.UpdateCreditPacks
{
    public class UpdateAICreditPackCommandHandler : IRequestHandler<UpdateAICreditPackCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateAICreditPackCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(UpdateAICreditPackCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<AICreditPack, int>();
            var pack = await repo.GetByIdAsync(request.Id) ?? throw new AICreditPackNotFoundException();

            pack.Name = request.Name;
            pack.Price = request.Price;
            pack.Credits = request.Credits;
            pack.BonusCredits = request.BonusCredits;
            pack.LastModifiedAt = DateTimeOffset.UtcNow;

            await _unitOfWork.CommitAsync();
            return Unit.Value;
        }
    }
}