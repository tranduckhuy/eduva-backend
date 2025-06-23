using MediatR;

namespace Eduva.Application.Features.Users.Commands
{
    public class UnlockAccountCommand : IRequest<Unit>
    {
        public Guid UserId { get; set; }
        public Guid ExecutorId { get; set; }

        public UnlockAccountCommand(Guid userId, Guid executorId)
        {
            UserId = userId;
            ExecutorId = executorId;
        }
    }
}