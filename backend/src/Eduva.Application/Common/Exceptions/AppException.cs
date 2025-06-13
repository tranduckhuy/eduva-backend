using Eduva.Shared.Constants;
using Eduva.Shared.Enums;

namespace Eduva.Application.Common.Exceptions
{
    public class AppException : Exception
    {
        public CustomCode StatusCode { get; }
        public IEnumerable<string>? Errors { get; }

        public AppException(CustomCode statusCode, IEnumerable<string>? errors = null)
            : base(ResponseMessages.Messages.GetValueOrDefault(statusCode)?.Message)
        {
            StatusCode = statusCode;
            Errors = errors;
        }
    }
}
