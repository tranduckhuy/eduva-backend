using Eduva.Domain.Constants;

namespace Eduva.Application.Common.Specifications
{
    public abstract class BaseSpecParam
    {
        public const int MaxPageSize = AppConstants.MAX_PAGE_SIZE;
        public int PageIndex { get; set; } = 1;
        private int _pageSize = AppConstants.DEFAULT_PAGE_SIZE;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }
    }
}
