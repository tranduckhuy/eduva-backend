using Eduva.Application.Common.Specifications;
using Eduva.Domain.Enums;

namespace Eduva.Application.Features.Folders.Specifications
{
    public class FolderSpecParam : BaseSpecParam
    {
        public string? Name { get; set; }
        public Guid? UserId { get; set; }
        public Guid? ClassId { get; set; }
        public OwnerType? OwnerType { get; set; }
    }
}
