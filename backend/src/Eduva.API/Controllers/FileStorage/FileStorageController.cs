using Eduva.API.Attributes;
using Eduva.API.Controllers.Base;
using Eduva.API.Models.FileStorage;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eduva.API.Controllers.FileStorage
{
    [Route("api/file-storage")]
    public class FileStorageController : BaseController<FileStorageController>
    {
        private const string SCHOOL_ID_CLAIM = "SchoolId";
        private readonly IStorageService _storageService;
        private readonly IStorageQuotaService _storageQuotaService;

        public FileStorageController(ILogger<FileStorageController> logger, IStorageService storageService, IStorageQuotaService storageQuotaService) : base(logger)
        {
            _storageService = storageService;
            _storageQuotaService = storageQuotaService;
        }

        [HttpPost("upload-tokens")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)}")]
        public async Task<IActionResult> GenerateUploadSasToken([FromBody] List<string> blobNames)
        {
            return await HandleRequestAsync(async () =>
            {
                var sasTokens = await _storageService.GenerateUploadSasTokens(blobNames);

                return (CustomCode.Success, new { UploadTokens = sasTokens });
            });
        }

        [HttpPost("upload-tokens-with-quota")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadWrite)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)}")]
        public async Task<IActionResult> GenerateUploadSasTokenWithQuota([FromBody] GenerateUploadTokensRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Respond(CustomCode.UserIdNotFound);
            }

            var schoolIdClaim = User.FindFirstValue(SCHOOL_ID_CLAIM);
            if (string.IsNullOrEmpty(schoolIdClaim) || !int.TryParse(schoolIdClaim, out int schoolId))
            {
                return Respond(CustomCode.SchoolNotFound);
            }

            return await HandleRequestAsync(async () =>
            {
                var blobNames = request.Files.Select(f => f.BlobName).ToList();
                var fileSizes = request.Files.Select(f => f.FileSize).ToList();

                var sasTokens = await _storageService.GenerateUploadSasTokensWithQuotaCheck(blobNames, fileSizes, schoolId);

                return (CustomCode.Success, new { UploadTokens = sasTokens });
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

        [HttpGet("storage-quota")]
        [SubscriptionAccess(SubscriptionAccessLevel.ReadOnly)]
        [Authorize(Roles = $"{nameof(Role.SystemAdmin)},{nameof(Role.SchoolAdmin)}, {nameof(Role.Teacher)}")]
        public async Task<IActionResult> GetStorageQuota()
        {
            var schoolIdClaim = User.FindFirstValue(SCHOOL_ID_CLAIM);
            if (string.IsNullOrEmpty(schoolIdClaim) || !int.TryParse(schoolIdClaim, out int schoolId))
            {
                return Respond(CustomCode.SchoolNotFound);
            }

            return await HandleRequestAsync(async () =>
            {
                var quotaDetails = await _storageQuotaService.GetStorageQuotaDetailsAsync(schoolId);
                return (CustomCode.Success, quotaDetails);
            });
        }
    }
}
