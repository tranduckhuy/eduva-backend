using System.Security.Cryptography;

namespace Eduva.Application.Features.Classes.Utilities
{
    public static class ClassCodeGenerator
    {
        private static readonly char[] ClassCodeChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

        public static string GenerateClassCode()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[8];
            rng.GetBytes(bytes);

            var result = new char[8];
            for (int i = 0; i < 8; i++)
            {
                result[i] = ClassCodeChars[bytes[i] % ClassCodeChars.Length];
            }

            return new string(result);
        }
    }
}
