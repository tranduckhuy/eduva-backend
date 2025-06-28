using Eduva.Application.Common.Specifications;

namespace Eduva.Application.Features.Classes.Specifications
{
    public class ClassSpecParam : BaseSpecParam
    {
        public int? SchoolId { get; set; }
        public Guid? TeacherId { get; set; }
        public Guid? ClassId { get; set; }
    }
}
