using Eduva.Domain.Enums;

namespace Eduva.Application.Features.Users.Requests
{
    public class ExportUsersRequest
    {
        public Role? Role { get; set; }
        public EntityStatus? Status { get; set; }
        public string? SearchTerm { get; set; }
        public string SortBy { get; set; } = "fullname";
        public string SortDirection { get; set; } = "asc";
    }
}