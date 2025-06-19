using Eduva.Application.Features.Classes.Responses;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Classes.Commands
{    public class DeleteClassCommand : IRequest<ClassResponse>
    {
        [JsonIgnore]
        public Guid Id { get; set; }
        
        [JsonIgnore]
        public Guid TeacherId { get; set; }
    }
}
