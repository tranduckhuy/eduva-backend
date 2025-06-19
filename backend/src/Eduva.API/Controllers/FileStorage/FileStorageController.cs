using Eduva.API.Controllers.Base;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eduva.API.Controllers.FileStorage
{
    [Route("api/file-storage")]
    public class FileStorageController : BaseController<FileStorageController>
    {
        private readonly IStorageService _storageService;

        public FileStorageController(ILogger<FileStorageController> logger, IStorageService storageService) : base(logger)
        {
            _storageService = storageService;
        }

        [HttpGet("sas-token/upload/{blobName}")]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)}")]
        public async Task<IActionResult> GenerateUploadSasToken(string blobName)
        {
            return await HandleRequestAsync(async () =>
            {
                var expiresOn = DateTimeOffset.UtcNow.AddHours(1);
                var sasToken = await _storageService.GenerateUploadSasToken(blobName, expiresOn);

                return (CustomCode.Success, new { SasToken = sasToken, ExpiresOn = expiresOn });
            });
        }

        [HttpGet("readable-url")]
        [Authorize]
        public IActionResult GetReadableUrl([FromQuery] string blobName)
        {
            if (string.IsNullOrWhiteSpace(blobName))
            {
                return Respond(CustomCode.InvalidBlobUrl);
            }

            var url = _storageService.GetReadableUrl(blobName);
            return Respond(CustomCode.Success, new { ReadableUrl = url });
        }

        [HttpDelete("{blobName}")]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}")]
        public async Task<IActionResult> DeleteFileAsync(string blobName)
        {
            return await HandleRequestAsync(async () => await _storageService.DeleteFileAsync(blobName));
        }
    }
}
