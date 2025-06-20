using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Schools.Commands.CreateSchool
{
    public class CreateSchoolCommand : IRequest<Unit>
    {
        [JsonIgnore]
        public Guid SchoolAdminId { get; set; }

        public string Name { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? WebsiteUrl { get; set; }
    }
}