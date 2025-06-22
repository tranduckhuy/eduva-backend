using MediatR;
using Microsoft.AspNetCore.Http;

namespace Eduva.Application.Features.Users.Commands
{
    public class ImportUsersFromExcelCommand : IRequest<byte[]?>
    {
        public IFormFile File { get; set; } = default!;
        public Guid CreatorId { get; set; }
    }
}