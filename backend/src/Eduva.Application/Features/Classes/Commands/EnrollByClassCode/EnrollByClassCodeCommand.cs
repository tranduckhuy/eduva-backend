using Eduva.Application.Features.Classes.Responses;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Classes.Commands.EnrollByClassCode
{
    public class EnrollByClassCodeCommand : IRequest<StudentClassResponse>
    {
        public string ClassCode { get; set; } = string.Empty;

        [JsonIgnore]
        public Guid StudentId { get; set; }
    }
}
