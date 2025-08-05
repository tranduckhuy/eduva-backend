using Eduva.Application.Features.Jobs.Commands.ConfirmJob;
using Eduva.Domain.Enums;

namespace Eduva.Application.Features.Jobs.DTOs;

public class JobMessage
{
    public Guid JobId { get; set; }
    public TaskType TaskType { get; set; }
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

public enum TaskType
{
    GenerateContent = 0,
    CreateProduct = 1,
}

