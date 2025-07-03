using MediatR;

namespace Eduva.Application.Features.Schools.Commands.ActivateSchool
{
    public class ActivateSchoolCommand : IRequest<Unit>
    {
        public int SchoolId { get; set; }

        public ActivateSchoolCommand(int schoolId)
        {
            SchoolId = schoolId;
        }
    }
}