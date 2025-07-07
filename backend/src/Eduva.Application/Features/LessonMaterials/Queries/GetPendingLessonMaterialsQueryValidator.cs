using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.LessonMaterials.Queries
{
    public class GetPendingLessonMaterialsQueryValidator : AbstractValidator<GetPendingLessonMaterialsQuery>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public GetPendingLessonMaterialsQueryValidator(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required.");

            RuleFor(x => x.SchoolId)
                .GreaterThan(0).WithMessage("Valid School ID is required.");

            RuleFor(x => x.UserRoles)
                .NotEmpty().WithMessage("User roles are required.")
                .Must(roles => roles.Any(role => role == nameof(Role.SchoolAdmin) ||
                                               role == nameof(Role.ContentModerator) ||
                                               role == nameof(Role.Teacher)))
                .WithMessage("User must have SchoolAdmin, ContentModerator, or Teacher role to access pending materials.");

            RuleFor(x => x.LessonMaterialSpecParam.SearchTerm)
                .MaximumLength(255).WithMessage("Search term must not exceed 255 characters.");

            RuleFor(x => x.LessonMaterialSpecParam.Tag)
                .MaximumLength(100).WithMessage("Tag must not exceed 100 characters.");

            RuleFor(x => x.LessonMaterialSpecParam.ContentType)
                .IsInEnum().When(x => x.LessonMaterialSpecParam.ContentType.HasValue)
                .WithMessage("Invalid content type specified.");

            RuleFor(x => x)
                .MustAsync(ValidateUserAccess)
                .WithMessage("User does not have access to view pending lesson materials.");
        }

        private async Task<bool> ValidateUserAccess(GetPendingLessonMaterialsQuery query, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(query.UserId.ToString());
            if (user == null || user.SchoolId != query.SchoolId)
            {
                return false;
            }

            // School admins and content moderators can see all pending materials in their school
            if (query.UserRoles.Contains(nameof(Role.SchoolAdmin)) ||
                query.UserRoles.Contains(nameof(Role.ContentModerator)))
            {
                return true;
            }

            // Teachers can only see their own pending materials unless they also have admin/moderator roles
            if (query.UserRoles.Contains(nameof(Role.Teacher)))
            {
                return true; // Logic is handled in the handler
            }

            return false;
        }
    }
}
