using MediatR;

namespace Eduva.Application.Features.Users.Commands
{
    public class LockAccountCommand : IRequest<Unit>
    {
        public Guid UserId { get; set; }
        public Guid ExecutorId { get; set; }

        public LockAccountCommand(Guid userId, Guid executorId)
        {
            UserId = userId;
            ExecutorId = executorId;
        }
    }
}