using Eduva.Application.Common.Exceptions;
using Eduva.Application.Exceptions.Auth;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Eduva.Shared.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Eduva.Application.Features.Users.Commands
{
    public class CreateUserByAdminCommandHandler : IRequestHandler<CreateUserByAdminCommand, Unit>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateUserByAdminCommandHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<Unit> Handle(CreateUserByAdminCommand request, CancellationToken cancellationToken)
        {
            if (request.Role is Role.SystemAdmin or Role.SchoolAdmin)
            {
                throw new InvalidRestrictedRoleException();
            }

            if (string.IsNullOrWhiteSpace(request.InitialPassword))
            {
                throw new AppException(CustomCode.ProvidedInformationIsInValid);
            }

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new EmailAlreadyExistsException();
            }

            var creator = await _userManager.FindByIdAsync(request.CreatorId.ToString()) ?? throw new UserNotExistsException();

            if (creator.SchoolId == null)
            {
                throw new UserNotPartOfSchoolException();
            }

            var newUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                UserName = request.Email,
                FullName = request.FullName,
                SchoolId = creator.SchoolId,
            };

            var result = await _userManager.CreateAsync(newUser, request.InitialPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                throw new AppException(CustomCode.ProvidedInformationIsInValid, errors);
            }

            await _userManager.AddToRoleAsync(newUser, request.Role.ToString());

            return Unit.Value;
        }
    }
}