using System.Security.Cryptography;

namespace RB_Website_API.Auth;

public static class PasswordHasher
{
    public static (byte[] hash, byte[] salt) Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Password is required.");

        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations: 100_000,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: 32);

        return (hash, salt);
    }

    public static bool Verify(string password, byte[] salt, byte[] expectedHash)
    {
        if (string.IsNullOrEmpty(password)) return false;
        if (salt is null || expectedHash is null) return false;
        if (salt.Length == 0 || expectedHash.Length == 0) return false;

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations: 100_000,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expectedHash);
    }
}

