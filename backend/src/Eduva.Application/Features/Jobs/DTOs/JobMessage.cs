using Eduva.Application.Features.Jobs.Commands.ConfirmJob;
using Eduva.Domain.Enums;

namespace Eduva.Application.Features.Jobs.DTOs;

public class JobMessage
{
    public Guid JobId { get; set; }
    public string TaskType { get; set; } = string.Empty;
}

public class GenerateContentMessage : JobMessage
{
    public string Topic { get; set; } = string.Empty;
    public List<string> SourceBlobNames { get; set; } = [];
}

public class CreateProductMessage : JobMessage
{
    public AIServiceType JobType { get; set; }
    public string ContentBlobName { get; set; } = string.Empty;
    public VoiceConfigDto VoiceConfig { get; set; } = default!;
}

public static class TaskType
{
    public const string GenerateContent = "generate_content";
    public const string CreateProduct = "create_product";
}
