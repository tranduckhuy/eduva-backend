namespace Eduva.Infrastructure.Configurations
{
    public class AzureBlobStorageOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;
        public string StorageAccountName { get; set; } = string.Empty;
        public string StorageAccountKey { get; set; } = string.Empty;
    }
}
