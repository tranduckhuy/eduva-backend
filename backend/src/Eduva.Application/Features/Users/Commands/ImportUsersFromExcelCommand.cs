using Eduva.Application.Features.Users.DTOs;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Eduva.Application.Features.Users.Commands
{
    public class ImportUsersFromExcelCommand : IRequest<(CustomCode, FileResponseDto?)>
    {
        public IFormFile File { get; set; } = default!;
        public Guid CreatorId { get; set; }
    }
}