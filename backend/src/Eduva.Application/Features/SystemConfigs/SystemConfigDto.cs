namespace Eduva.Application.Features.SystemConfigs
{
    public class SystemConfigDto
    {
        public int Id { get; set; }
        public string Key { get; set; } = default!;
        public string Value { get; set; } = default!;
        public string? Description { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastModifiedAt { get; set; }
    }
}
