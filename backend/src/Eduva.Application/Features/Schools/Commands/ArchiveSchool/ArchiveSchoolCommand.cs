using MediatR;

namespace Eduva.Application.Features.Schools.Commands.ArchiveSchool
{
    public class ArchiveSchoolCommand : IRequest<Unit>
    {
        public int SchoolId { get; set; }

        public ArchiveSchoolCommand(int schoolId)
        {
            SchoolId = schoolId;
        }
    }
}