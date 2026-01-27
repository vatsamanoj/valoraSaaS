using Microsoft.AspNetCore.Identity;
namespace Lab360.Application.Common.Security
{
    /// <summary>
    /// Centralized password hashing utility.
    /// Uses ASP.NET Core Identity's PBKDF2 implementation.
    /// </summary>
    public static class PasswordHasher
    {
        // We don't persist the user object here.
        // The string "system" is used only as a hashing context.
        private static readonly PasswordHasher<string> _hasher = new();

        public static string Hash(string password)
        {
            return _hasher.HashPassword("system", password);
        }

        public static bool Verify(string hashedPassword, string providedPassword)
        {
            var result = _hasher.VerifyHashedPassword(
                "system",
                hashedPassword,
                providedPassword);

            return result == PasswordVerificationResult.Success;
        }
    }
}