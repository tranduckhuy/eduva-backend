using Microsoft.AspNetCore.Http;

namespace Eduva.Infrastructure.Services.Interface
{
    public interface IExcelService
    {
        Task<byte[]> ExportImportErrorsAsync(IFormFile originalFile, Dictionary<int, string> rowErrors);
    }
}