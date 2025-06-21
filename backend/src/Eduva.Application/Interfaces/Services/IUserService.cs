using Eduva.Application.Features.Users.DTOs;
using Eduva.Shared.Enums;
using Microsoft.AspNetCore.Http;

namespace Eduva.Application.Interfaces.Services
{
    public interface IUserService
    {
        Task<CustomCode> CreateUserByAdminAsync(CreateUserByAdminRequestDto request, Guid creatorId);
        Task<(CustomCode, FileResponseDto?)> ImportUsersFromExcelAsync(IFormFile file, Guid creatorId);
    }
}