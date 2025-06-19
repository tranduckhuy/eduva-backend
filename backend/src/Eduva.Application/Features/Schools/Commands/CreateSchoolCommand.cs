using Eduva.Application.Features.Schools.Reponses;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Schools.Commands
{
    public class CreateSchoolCommand : IRequest<SchoolResponse>
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