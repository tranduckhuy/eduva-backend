using Eduva.Application.Exceptions.School;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using MediatR;

namespace Eduva.Application.Features.Schools.Commands.UpdateSchool
{
    public class UpdateSchoolCommandHandler : IRequestHandler<UpdateSchoolCommand, Unit>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdateSchoolCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(UpdateSchoolCommand request, CancellationToken cancellationToken)
        {
            var repo = _unitOfWork.GetRepository<School, int>();

            var school = await repo.GetByIdAsync(request.Id) ?? throw new SchoolNotFoundException();

            school.Name = request.Name;
            school.ContactEmail = request.ContactEmail;
            school.ContactPhone = request.ContactPhone;
            school.Address = request.Address;
            school.WebsiteUrl = request.WebsiteUrl;

            repo.Update(school);
            await _unitOfWork.CommitAsync();

            return Unit.Value;
        }
    }
}