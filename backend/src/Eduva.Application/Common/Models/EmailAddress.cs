namespace Eduva.Application.Common.Models
{
    public class EmailAddress
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public EmailAddress() { }

        public EmailAddress(string email, string? displayName = null)
        {
            Email = email;
            DisplayName = displayName ?? email;
        }
    }
}
