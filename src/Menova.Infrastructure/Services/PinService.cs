using System.Security.Cryptography;
using System.Text;
using Menova.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Menova.Infrastructure.Services;

public class PinService : IPinService
{
    private readonly string _pepper;

    public PinService(IConfiguration configuration)
    {
        _pepper = configuration["Security:PinPepper"] ?? "Menova_Secure_Pin_Pepper_2026_Africa_Maputo";
    }

    public string GerarSalt()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public string HashPin(string pin, string salt)
    {
        var input = $"{pin}:{salt}:{_pepper}";
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(input),
            Encoding.UTF8.GetBytes(salt),
            iterations: 100_000,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: 32);

        return $"{salt}:{Convert.ToHexString(hashBytes).ToLowerInvariant()}";
    }

    public bool VerificarPin(string pin, string salt, string hashArmazenado)
    {
        var novoHash = HashPin(pin, salt);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(novoHash),
            Encoding.UTF8.GetBytes(hashArmazenado));
    }
}
