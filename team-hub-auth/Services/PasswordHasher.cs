using Isopoh.Cryptography.Argon2;

namespace team_hub_auth.Services;

public static class PasswordHasher
{
    public static string Hash(string password) => Argon2.Hash(password);

    public static bool Verify(string password, string hash) => Argon2.Verify(hash, password);
}
