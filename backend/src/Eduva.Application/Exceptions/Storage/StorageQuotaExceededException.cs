using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.Storage
{
    public class StorageQuotaExceededException : AppException
    {
        public long UsedBytes { get; }
        public long LimitBytes { get; }
        public long RequestedBytes { get; }
        public long RemainingBytes { get; }

        public StorageQuotaExceededException(long usedBytes, long limitBytes, long requestedBytes, long remainingBytes) 
            : base(CustomCode.StorageQuotaExceeded)
        {
            UsedBytes = usedBytes;
            LimitBytes = limitBytes;
            RequestedBytes = requestedBytes;
            RemainingBytes = remainingBytes;
        }

        public override string Message => 
            $"Storage quota exceeded. Used: {FormatBytes(UsedBytes)}, " +
            $"Limit: {FormatBytes(LimitBytes)}, " +
            $"Requested: {FormatBytes(RequestedBytes)}, " +
            $"Remaining: {FormatBytes(RemainingBytes)}";

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
