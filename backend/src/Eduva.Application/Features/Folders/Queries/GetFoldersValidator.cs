using Eduva.Application.Common.Specifications;
using FluentValidation;

namespace Eduva.Application.Features.Folders.Queries
{
    public class GetFoldersValidator : AbstractValidator<GetFoldersQuery>
    {
        public GetFoldersValidator()
        {
            RuleFor(x => x.FolderSpecParam.PageIndex)
                .GreaterThanOrEqualTo(1)
                .WithMessage("PageIndex must be greater than or equal to 1");

            RuleFor(x => x.FolderSpecParam.PageSize)
                .GreaterThanOrEqualTo(1)
                .WithMessage("PageSize must be greater than or equal to 1")
                .LessThanOrEqualTo(BaseSpecParam.MaxPageSize)
                .WithMessage($"PageSize must be less than or equal to {BaseSpecParam.MaxPageSize}");
        }
    }
}
