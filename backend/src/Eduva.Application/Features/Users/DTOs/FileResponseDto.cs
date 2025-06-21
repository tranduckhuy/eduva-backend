namespace Eduva.Application.Features.Users.DTOs
{
    public class FileResponseDto
    {
        public string FileName { get; set; } = default!;
        public byte[] Content { get; set; } = default!;
    }

}
