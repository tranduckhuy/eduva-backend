using System.ComponentModel.DataAnnotations;

namespace Eduva.Application.Features.SystemConfigs
{
    public class CreateSystemConfigDto
    {
        [Required]
        [StringLength(100)]
        public string Key { get; set; } = default!;

        [Required]
        [StringLength(500)]
        public string Value { get; set; } = default!;

        [StringLength(255)]
        public string? Description { get; set; }
    }
}
