namespace Eduva.Application.Interfaces.Services
{
    public interface ISystemConfigHelper
    {
        Task<string> GetValueAsync(string key, string defaultValue = "");
        Task<string> GetDefaultAvatarUrlAsync();
        Task<string> GetImportUsersTemplateAsync();
        Task<string> GetPayosReturnUrlPlanAsync();
        Task<string> GetPayosReturnUrlPackAsync();
    }
}