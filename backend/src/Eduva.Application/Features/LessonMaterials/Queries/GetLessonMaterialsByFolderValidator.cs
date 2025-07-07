using FluentValidation;

namespace Eduva.Application.Features.LessonMaterials.Queries
{
    public class GetLessonMaterialsByFolderValidator : AbstractValidator<GetLessonMaterialsByFolderQuery>
    {
        public GetLessonMaterialsByFolderValidator()
        {
            RuleFor(x => x.FolderId)
                .NotEmpty()
                .WithMessage("FolderId is required");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId is required");

            RuleFor(x => x.UserRoles)
                .NotNull()
                .NotEmpty()
                .WithMessage("User roles are required");
        }
    }
}
