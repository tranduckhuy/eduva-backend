using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.LessonMaterial
{
    public class LessonMaterialNotFoundException : AppException
    {
        public LessonMaterialNotFoundException(Guid id) :
            base(CustomCode.LessonMaterialNotFound, [$"Lesson material with ID {id} not found."])
        {
        }

        public LessonMaterialNotFoundException(IEnumerable<Guid> ids)
            : base(CustomCode.LessonMaterialNotFound,
                   ids.Select(id => $"Lesson material with ID {id} not found."))
        {
        }
    }
}
