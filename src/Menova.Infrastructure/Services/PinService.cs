using System.Security.Cryptography;
using System.Text;
using Menova.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Menova.Infrastructure.Services;

public class PinService : IPinService
{
    private readonly IConfiguration _configuration;
    private readonly IAppDbContext? _context;
    private readonly string _pepper;

    public PinService(IConfiguration configuration, IAppDbContext? context = null)
    {
        _configuration = configuration;
        _context = context;
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

    public async Task<bool> ValidarPinSupervisorAsync(string pin, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pin) || _context == null) return false;

        var supervisores = await _context.Utilizadores
            .AsNoTracking()
            .Include(u => u.UtilizadoresPerfis).ThenInclude(up => up.Perfil)
            .Where(u => u.Ativo && u.HashPin != null)
            .ToListAsync(cancellationToken);

        foreach (var u in supervisores)
        {
            var eSupervisor = u.UtilizadoresPerfis.Any(up => up.Perfil.Codigo == "ADMIN" || up.Perfil.Codigo == "GERENTE");
            if (!eSupervisor) continue;

            string[] partes = u.HashPin!.Split(':');
            if (partes.Length == 2)
            {
                var salt = partes[0];
                if (VerificarPin(pin, salt, u.HashPin!))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
