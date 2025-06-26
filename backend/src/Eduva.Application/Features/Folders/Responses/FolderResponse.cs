using Eduva.Domain.Enums;

namespace Eduva.Application.Features.Folders.Responses
{
    public class FolderResponse
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public Guid? ClassId { get; set; }
        public string OwnerName { get; set; } = string.Empty; // Username or Class name
        public OwnerType OwnerType { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset LastModifiedAt { get; set; }
    }
}
