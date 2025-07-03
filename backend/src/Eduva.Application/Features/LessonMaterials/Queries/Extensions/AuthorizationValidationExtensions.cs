using Eduva.Domain.Enums;

namespace Eduva.Application.Features.LessonMaterials.Queries.Extensions
{
    public static class AuthorizationValidationExtensions
    {
        public static bool HasSystemAdminRole(this IList<string> roles)
        {
            return roles.Contains(nameof(Role.SystemAdmin));
        }

        public static bool HasSchoolAdminRole(this IList<string> roles)
        {
            return roles.Contains(nameof(Role.SchoolAdmin));
        }

        public static bool HasContentModeratorRole(this IList<string> roles)
        {
            return roles.Contains(nameof(Role.ContentModerator));
        }

        public static bool HasTeacherRole(this IList<string> roles)
        {
            return roles.Contains(nameof(Role.Teacher));
        }

        public static bool HasStudentRole(this IList<string> roles)
        {
            return roles.Contains(nameof(Role.Student));
        }

        public static bool HasSchoolManagementRoles(this IList<string> roles)
        {
            return roles.HasSchoolAdminRole() || roles.HasContentModeratorRole();
        }

        public static bool CanAccessAllSchoolContent(this IList<string> roles)
        {
            return roles.HasSystemAdminRole() || roles.HasSchoolManagementRoles();
        }

        public static bool RequiresSchoolIdForAuthorization(this IList<string> roles, bool hasClassOrFolderParam)
        {
            // SystemAdmin doesn't need schoolId
            if (roles.HasSystemAdminRole()) return false;

            // Others need schoolId when accessing specific class/folder
            return hasClassOrFolderParam;
        }
    }
}
