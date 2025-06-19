using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.FileStorage
{
    public class BlobNotFoundException : AppException
    {
        public BlobNotFoundException() : base(CustomCode.BlobNotFound) { }
    }
}
