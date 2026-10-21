using Menova.Application.Interfaces;
using Menova.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Menova.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IAppDbContext _context;
    private readonly IAuditoriaService _auditoriaService;
    private readonly IPasswordHasher<Utilizador> _passwordHasher;
    private readonly IPinService _pinService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IAppDbContext context,
        IAuditoriaService auditoriaService,
        IPasswordHasher<Utilizador> passwordHasher,
        IPinService pinService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _auditoriaService = auditoriaService;
        _passwordHasher = passwordHasher;
        _pinService = pinService;
        _logger = logger;
    }

    public async Task<ResultadoLogin> AutenticarPainelAsync(
        string nomeUtilizador,
        string palavraPasse,
        string ip,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var nomeNormalizado = nomeUtilizador.Trim().ToLowerInvariant();

        var utilizador = await _context.Utilizadores
            .Include(u => u.UtilizadoresPerfis)
                .ThenInclude(up => up.Perfil)
            .FirstOrDefaultAsync(u => u.NomeUtilizador.ToLower() == nomeNormalizado, cancellationToken);

        if (utilizador == null)
        {
            RegistarTentativa(null, nomeUtilizador, ip, null, false, "Utilizador inexistente");
            return new ResultadoLogin(false, "Nome de utilizador ou palavra-passe incorretos.");
        }

        if (utilizador.EstaBloqueado())
        {
            var minutosRestantes = (int)Math.Ceiling((utilizador.BloqueadoAte!.Value - DateTime.UtcNow).TotalMinutes);
            RegistarTentativa(utilizador.Id, nomeUtilizador, ip, null, false, "Conta bloqueada");
            return new ResultadoLogin(false, $"Conta temporariamente bloqueada. Tente novamente dentro de {minutosRestantes} minuto(s).", Bloqueado: true, BloqueadoAte: utilizador.BloqueadoAte);
        }

        if (!utilizador.Ativo)
        {
            RegistarTentativa(utilizador.Id, nomeUtilizador, ip, null, false, "Conta inativa");
            return new ResultadoLogin(false, "Esta conta encontra-se inativa. Contacte o administrador.");
        }

        var verificacao = _passwordHasher.VerifyHashedPassword(utilizador, utilizador.HashPalavraPasse, palavraPasse);
        if (verificacao == PasswordVerificationResult.Failed)
        {
            utilizador.RegistarFalhaLogin();
            RegistarTentativa(utilizador.Id, nomeUtilizador, ip, null, false, "Palavra-passe incorreta");
            await _context.SaveChangesAsync(cancellationToken);

            if (utilizador.EstaBloqueado())
            {
                await _auditoriaService.RegistarAsync(
                    "utilizador.bloqueado",
                    "utilizadores",
                    utilizador.Id.ToString(),
                    valoresDepois: new { utilizador.TentativasFalhadas, utilizador.BloqueadoAte },
                    motivo: "Excesso de tentativas falhadas de autenticação",
                    cancellationToken: cancellationToken);

                return new ResultadoLogin(false, "Conta bloqueada por 15 minutos devido a 5 tentativas falhadas consecutivas.", Bloqueado: true, BloqueadoAte: utilizador.BloqueadoAte);
            }

            return new ResultadoLogin(false, "Nome de utilizador ou palavra-passe incorretos.");
        }

        // Sucesso na autenticação
        utilizador.RegistarSucessoLogin();

        var sessao = new SessaoUtilizador
        {
            UtilizadorId = utilizador.Id,
            Inicio = DateTime.UtcNow,
            UltimoAcesso = DateTime.UtcNow,
            Ip = ip,
            UserAgent = userAgent,
            Revogada = false
        };
        _context.SessoesUtilizador.Add(sessao);

        RegistarTentativa(utilizador.Id, nomeUtilizador, ip, null, true, "Autenticação bem-sucedida");
        await _context.SaveChangesAsync(cancellationToken);

        var perfilPrincipal = utilizador.UtilizadoresPerfis.FirstOrDefault()?.Perfil.Codigo;

        await _auditoriaService.RegistarAsync(
            "login.sucesso",
            "utilizadores",
            utilizador.Id.ToString(),
            valoresDepois: new { utilizador.NomeUtilizador, ip },
            cancellationToken: cancellationToken);

        return new ResultadoLogin(
            Sucesso: true,
            DeveAlterarPalavraPasse: utilizador.DeveAlterarPalavraPasse,
            UtilizadorId: utilizador.Id,
            NomeCompleto: utilizador.NomeCompleto,
            NomeUtilizador: utilizador.NomeUtilizador,
            PerfilCodigo: perfilPrincipal);
    }

    public async Task<ResultadoLogin> AutenticarPinAsync(
        string pin,
        string tokenDispositivo,
        string ip,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pin) || pin.Length < 4)
        {
            RegistarTentativa(null, "PIN_INVALIDO", ip, null, false, "PIN fornecido com formato inválido");
            await _context.SaveChangesAsync(cancellationToken);
            return new ResultadoLogin(false, "O PIN deve conter pelo menos 4 dígitos.");
        }

        Guid? dispositivoId = null;
        if (!string.IsNullOrWhiteSpace(tokenDispositivo))
        {
            var dispositivo = await _context.Dispositivos
                .FirstOrDefaultAsync(d => d.TokenHash == tokenDispositivo && d.EliminadoEm == null, cancellationToken);

            if (dispositivo != null)
            {
                if (dispositivo.Estado == EstadoDispositivo.Revogado)
                {
                    RegistarTentativa(null, "DISPOSITIVO_REVOGADO", ip, dispositivo.Id, false, "Dispositivo revogado");
                    await _context.SaveChangesAsync(cancellationToken);
                    return new ResultadoLogin(false, "Este dispositivo móvel foi revogado pelo administrador.");
                }

                dispositivoId = dispositivo.Id;
                dispositivo.UltimoAcessoEm = DateTime.UtcNow;
                dispositivo.UltimoIp = ip;
            }
        }

        // Localizar utilizador ativo cujo HashPin coincide
        var utilizadoresComPin = await _context.Utilizadores
            .Include(u => u.UtilizadoresPerfis).ThenInclude(up => up.Perfil)
            .Where(u => u.Ativo && u.HashPin != null)
            .ToListAsync(cancellationToken);

        Utilizador? utilizador = null;
        foreach (var u in utilizadoresComPin)
        {
            var partes = u.HashPin!.Split(':');
            if (partes.Length == 2 && _pinService.VerificarPin(pin, partes[0], u.HashPin))
            {
                utilizador = u;
                break;
            }
        }

        if (utilizador == null)
        {
            RegistarTentativa(null, "PIN_DESCONHECIDO", ip, dispositivoId, false, "PIN incorreto");
            await _context.SaveChangesAsync(cancellationToken);
            return new ResultadoLogin(false, "PIN incorreto ou colaborador não encontrado.");
        }

        if (utilizador.EstaBloqueado())
        {
            var minutosRestantes = (int)Math.Ceiling((utilizador.BloqueadoAte!.Value - DateTime.UtcNow).TotalMinutes);
            RegistarTentativa(utilizador.Id, utilizador.NomeUtilizador, ip, dispositivoId, false, "Conta bloqueada");
            await _context.SaveChangesAsync(cancellationToken);
            return new ResultadoLogin(false, $"Conta temporariamente bloqueada. Tente dentro de {minutosRestantes} minuto(s).", Bloqueado: true, BloqueadoAte: utilizador.BloqueadoAte);
        }

        // Sucesso na autenticação por PIN
        utilizador.RegistarSucessoLogin();

        var sessao = new SessaoUtilizador
        {
            UtilizadorId = utilizador.Id,
            DispositivoId = dispositivoId,
            Inicio = DateTime.UtcNow,
            UltimoAcesso = DateTime.UtcNow,
            Ip = ip,
            UserAgent = userAgent,
            Revogada = false
        };
        _context.SessoesUtilizador.Add(sessao);

        RegistarTentativa(utilizador.Id, utilizador.NomeUtilizador, ip, dispositivoId, true, "Autenticação por PIN bem-sucedida");
        await _context.SaveChangesAsync(cancellationToken);

        var perfilPrincipal = utilizador.UtilizadoresPerfis.FirstOrDefault()?.Perfil.Codigo;

        await _auditoriaService.RegistarAsync(
            "login.pin.sucesso",
            "utilizadores",
            utilizador.Id.ToString(),
            valoresDepois: new { utilizador.NomeUtilizador, ip, dispositivoId },
            cancellationToken: cancellationToken);

        return new ResultadoLogin(
            Sucesso: true,
            DeveAlterarPalavraPasse: false,
            UtilizadorId: utilizador.Id,
            NomeCompleto: utilizador.NomeCompleto,
            NomeUtilizador: utilizador.NomeUtilizador,
            PerfilCodigo: perfilPrincipal);
    }

    public async Task<(bool Sucesso, string? MensagemErro)> AlterarPalavraPasseAsync(
        Guid utilizadorId,
        string palavraPasseAtual,
        string novaPalavraPasse,
        CancellationToken cancellationToken = default)
    {
        var utilizador = await _context.Utilizadores.FindAsync([utilizadorId], cancellationToken);
        if (utilizador == null)
        {
            return (false, "Utilizador não encontrado.");
        }

        var verificacao = _passwordHasher.VerifyHashedPassword(utilizador, utilizador.HashPalavraPasse, palavraPasseAtual);
        if (verificacao == PasswordVerificationResult.Failed)
        {
            return (false, "A palavra-passe atual está incorreta.");
        }

        utilizador.HashPalavraPasse = _passwordHasher.HashPassword(utilizador, novaPalavraPasse);
        utilizador.DeveAlterarPalavraPasse = false;
        utilizador.SeloSeguranca = Guid.NewGuid().ToString("N");

        await _context.SaveChangesAsync(cancellationToken);

        await _auditoriaService.RegistarAsync(
            "palavra_passe.alterada",
            "utilizadores",
            utilizador.Id.ToString(),
            motivo: "Alteração de palavra-passe pelo próprio utilizador",
            cancellationToken: cancellationToken);

        return (true, null);
    }

    public async Task TerminarSessaoAsync(Guid utilizadorId, Guid? dispositivoId = null, CancellationToken cancellationToken = default)
    {
        var sessoes = await _context.SessoesUtilizador
            .Where(s => s.UtilizadorId == utilizadorId && !s.Revogada)
            .ToListAsync(cancellationToken);

        foreach (var sessao in sessoes)
        {
            sessao.Revogada = true;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditoriaService.RegistarAsync(
            "logout.sucesso",
            "utilizadores",
            utilizadorId.ToString(),
            cancellationToken: cancellationToken);
    }

    private void RegistarTentativa(
        Guid? utilizadorId,
        string nomeTentado,
        string ip,
        Guid? dispositivoId,
        bool sucesso,
        string motivo)
    {
        var tentativa = new TentativaLogin
        {
            UtilizadorId = utilizadorId,
            NomeTentado = nomeTentado,
            Ip = ip,
            DispositivoId = dispositivoId,
            Sucesso = sucesso,
            Motivo = motivo,
            DataHora = DateTime.UtcNow
        };

        _context.TentativasLogin.Add(tentativa);
    }
}
