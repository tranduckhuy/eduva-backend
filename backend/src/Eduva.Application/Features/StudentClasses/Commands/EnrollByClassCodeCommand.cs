using Eduva.Application.Features.StudentClasses.Responses;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.StudentClasses.Commands
{
    public class EnrollByClassCodeCommand : IRequest<StudentClassResponse>
    {
        public string ClassCode { get; set; } = string.Empty;
        
        [JsonIgnore]
        public Guid StudentId { get; set; }
    }
}
