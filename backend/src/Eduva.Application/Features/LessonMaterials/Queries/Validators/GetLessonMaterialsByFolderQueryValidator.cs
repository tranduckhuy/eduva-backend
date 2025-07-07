using Eduva.Domain.Enums;
using FluentValidation;

namespace Eduva.Application.Features.LessonMaterials.Queries.Validators
{
    public class GetLessonMaterialsByFolderQueryValidator : AbstractValidator<GetLessonMaterialsByFolderQuery>
    {
        public GetLessonMaterialsByFolderQueryValidator()
        {
            RuleFor(x => x.FolderId)
                .NotEmpty()
                .WithMessage("FolderId is required");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId is required");

            RuleFor(x => x.UserRoles)
                .NotEmpty()
                .WithMessage("User roles are required");

            When(x => x.FilterOptions != null, () =>
            {
                RuleFor(x => x.FilterOptions!.SortDirection)
                    .Must(sortDirection => string.IsNullOrWhiteSpace(sortDirection) ||
                                         sortDirection.ToLower() == "asc" ||
                                         sortDirection.ToLower() == "desc")
                    .WithMessage("SortDirection must be 'asc' or 'desc'");

                RuleFor(x => x.FilterOptions!.SortBy)
                    .Must(sortBy => string.IsNullOrWhiteSpace(sortBy) ||
                                  new[] { "title", "createdat", "lastmodifiedat", "filesize", "duration", "lessonstatus" }
                                      .Contains(sortBy.ToLower()))
                    .WithMessage("SortBy must be one of: title, createdat, lastmodifiedat, filesize, duration, lessonstatus");

                RuleFor(x => x.FilterOptions!.LessonStatus)
                    .Must(status => !status.HasValue || Enum.IsDefined(typeof(LessonMaterialStatus), status.Value))
                    .WithMessage("Invalid LessonMaterialStatus value");

                RuleFor(x => x.FilterOptions!.Status)
                    .Must(status => !status.HasValue || Enum.IsDefined(typeof(EntityStatus), status.Value))
                    .WithMessage("Invalid EntityStatus value");
            });
        }
    }
}
