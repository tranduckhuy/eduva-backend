using Eduva.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Eduva.Application.Features.Users.DTOs
{
    public class CreateUserByAdminRequestDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = default!;

        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(100, ErrorMessage = "Full name must be less than 100 characters")]
        public string FullName { get; set; } = default!;

        [Required(ErrorMessage = "Role is required")]
        [EnumDataType(typeof(Role), ErrorMessage = "Invalid role")]
        public Role Role { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(255, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
        public string InitialPassword { get; set; } = default!;
    }
}