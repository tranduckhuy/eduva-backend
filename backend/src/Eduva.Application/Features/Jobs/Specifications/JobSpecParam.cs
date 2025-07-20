using Eduva.Application.Common.Specifications;

namespace Eduva.Application.Features.Jobs.Specifications
{
    public class JobSpecParam : BaseSpecParam
    {
        public Guid UserId { get; set; }
        public string? Topic { get; set; }
    }
}
