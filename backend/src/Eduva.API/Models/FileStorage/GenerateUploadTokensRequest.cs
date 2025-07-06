using System.ComponentModel.DataAnnotations;

namespace Eduva.API.Models.FileStorage
{
    public class GenerateUploadTokensRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least one file must be specified")]
        [MaxLength(10, ErrorMessage = "Cannot upload more than 10 files at once")]
        public List<FileUploadInfo> Files { get; set; } = new();
    }

    public class FileUploadInfo
    {
        [Required]
        [StringLength(255, ErrorMessage = "Blob name cannot exceed 255 characters")]
        public string BlobName { get; set; } = string.Empty;

        [Required]
        [Range(1, long.MaxValue, ErrorMessage = "File size must be greater than 0")]
        public long FileSize { get; set; }
    }
}
