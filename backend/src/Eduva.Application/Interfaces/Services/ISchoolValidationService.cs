namespace Eduva.Application.Interfaces.Services
{
    public interface ISchoolValidationService
    {
        Task ValidateCanAddUsersAsync(int schoolId, int additionalUsers = 1, CancellationToken cancellationToken = default);
    }
}
