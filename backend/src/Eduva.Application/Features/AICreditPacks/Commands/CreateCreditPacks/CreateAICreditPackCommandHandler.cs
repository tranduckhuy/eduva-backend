using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using MediatR;

namespace Eduva.Application.Features.AICreditPacks.Commands.CreateCreditPacks
{
    public class CreateAICreditPackCommandHandler : IRequestHandler<CreateAICreditPackCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateAICreditPackCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(CreateAICreditPackCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<AICreditPack, int>();

            var pack = new AICreditPack
            {
                Name = request.Name,
                Price = request.Price,
                Credits = request.Credits,
                BonusCredits = request.BonusCredits,
                Status = EntityStatus.Active,
                LastModifiedAt = DateTimeOffset.UtcNow,
            };

            await repo.AddAsync(pack);
            await _unitOfWork.CommitAsync();

            return Unit.Value;
        }
    }
}