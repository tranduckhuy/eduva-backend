using Eduva.Application.Features.Classes.Responses;
using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Classes.Commands.CreateClass
{
    public class CreateClassCommand : IRequest<ClassResponse>
    {
        [JsonIgnore]
        public Guid TeacherId { get; set; }

        public int SchoolId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
