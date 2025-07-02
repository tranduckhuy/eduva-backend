using Eduva.Application.Common.Exceptions;
using Eduva.Shared.Enums;

namespace Eduva.Application.Exceptions.LessonMaterial
{
    public class LessonMaterialNotFountException : AppException
    {
        public LessonMaterialNotFountException(Guid id) :
            base(CustomCode.LessonMaterialNotFound, [$"Lesson material with ID {id} not found."])
        {
        }
    }
}
