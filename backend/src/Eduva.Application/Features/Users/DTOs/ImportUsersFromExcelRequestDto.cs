using Microsoft.AspNetCore.Http;

namespace Eduva.Application.Features.Users.DTOs
{
    public class ImportUsersFromExcelRequestDto
    {
        public IFormFile File { get; set; } = default!;
    }

}
