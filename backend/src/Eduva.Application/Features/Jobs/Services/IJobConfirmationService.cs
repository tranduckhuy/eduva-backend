using Eduva.Application.Features.Jobs.Commands.ConfirmJob;

namespace Eduva.Application.Features.Jobs.Services
{
    public interface IJobConfirmationService
    {
        Task ConfirmJobAsync(ConfirmJobCommand request, CancellationToken cancellationToken);
    }
}
