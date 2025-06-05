namespace Eduva.Domain.Entities
{
    public class SchoolConfig
    {
        public int Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string? Address { get; private set; }
        public string LogoUrl { get; private set; } = string.Empty;
        public string? Website { get; private set; }
        public string? Description { get; private set; }
        public string ContactEmail { get; private set; } = string.Empty;
    }
}
