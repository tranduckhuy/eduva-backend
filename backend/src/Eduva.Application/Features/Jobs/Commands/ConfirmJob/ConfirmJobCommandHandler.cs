using Eduva.Application.Features.Jobs.Services;
using Eduva.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Eduva.Application.Features.Jobs.Commands.ConfirmJob;

public class ConfirmJobCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
    public AIServiceType Type { get; set; } // User's choice: audio or video
    public VoiceConfigDto VoiceConfig { get; set; } = default!;
}

public class VoiceConfigDto
{
    [JsonPropertyName("language_code")]
    [DefaultValue("vi-VN")]
    public string LanguageCode { get; set; } = default!;

    [JsonPropertyName("name")]
    [DefaultValue("vi-VN-Chirp3-HD-Enceladus")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("speaking_rate")]
    [DefaultValue(1.0f)]
    public float SpeakingRate { get; set; }
}

public class ConfirmJobCommandHandler : IRequestHandler<ConfirmJobCommand, Unit>
{
    private readonly IJobConfirmationService _jobConfirmationService;
    private readonly ILogger<ConfirmJobCommandHandler> _logger;

    public ConfirmJobCommandHandler(
        ILogger<ConfirmJobCommandHandler> logger, IJobConfirmationService jobConfirmationService)
    {
        _logger = logger;
        _jobConfirmationService = jobConfirmationService;
    }

    public async Task<Unit> Handle(ConfirmJobCommand request, CancellationToken cancellationToken)
    {
        await _jobConfirmationService.ConfirmJobAsync(request, cancellationToken);

        _logger.LogInformation("Job {JobId} confirmed successfully.", request.Id);

        return Unit.Value;
    }
}
