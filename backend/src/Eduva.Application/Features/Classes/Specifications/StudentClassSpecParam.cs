using Eduva.Application.Common.Specifications;
using Eduva.Domain.Enums;

namespace Eduva.Application.Features.Classes.Specifications
{
    public class StudentClassSpecParam : BaseSpecParam
    {
        public Guid StudentId { get; set; }
        public string? ClassName { get; set; }
        public string? TeacherName { get; set; }
        public string? SchoolName { get; set; }
        public string? ClassCode { get; set; }
        public EntityStatus? ClassStatus { get; set; }
    }
}
