using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Eduva.Application.Features.Users.Requests
{
    public class ImportUsersFromExcelRequest
    {
        [Required]
        public IFormFile File { get; set; } = default!;
    }
}