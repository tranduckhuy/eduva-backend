using MediatR;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Classes.Commands.RemoveStudentFromClass
{
    public class RemoveStudentFromClassCommand : IRequest<Unit>
    {
        [JsonIgnore]
        public Guid ClassId { get; set; }
        
        [JsonIgnore]
        public Guid StudentId { get; set; }
        
        [JsonIgnore]
        public Guid RequestUserId { get; set; }
        
        [JsonIgnore]
        public bool IsTeacher { get; set; }
        
        [JsonIgnore]
        public bool IsSchoolAdmin { get; set; }
        
        [JsonIgnore]
        public bool IsSystemAdmin { get; set; }
    }
}
