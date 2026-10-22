namespace Menova.Application.Interfaces;

public interface IPinService
{
    string HashPin(string pin, string salt);
    bool VerificarPin(string pin, string salt, string hashArmazenado);
    string GerarSalt();
    Task<bool> ValidarPinSupervisorAsync(string pin, CancellationToken cancellationToken = default);
}

public record ResultadoLogin(
    bool Sucesso,
    string? MensagemErro = null,
    bool DeveAlterarPalavraPasse = false,
    bool Bloqueado = false,
    DateTime? BloqueadoAte = null,
    Guid? UtilizadorId = null,
    string? NomeCompleto = null,
    string? NomeUtilizador = null,
    string? PerfilCodigo = null,
    string? SeloSeguranca = null);

public interface IAuthService
{
    Task<ResultadoLogin> AutenticarPainelAsync(string nomeUtilizador, string palavraPasse, string ip, string? userAgent, CancellationToken cancellationToken = default);
    Task<ResultadoLogin> AutenticarPinAsync(string pin, string tokenDispositivo, string ip, string? userAgent, CancellationToken cancellationToken = default);
    Task<ResultadoLogin> AutenticarBiometriaAsync(string credencialId, string ip, string? userAgent, CancellationToken cancellationToken = default);
    Task<(bool Sucesso, string? MensagemErro)> AlterarPalavraPasseAsync(Guid utilizadorId, string palavraPasseAtual, string novaPalavraPasse, CancellationToken cancellationToken = default);
    Task TerminarSessaoAsync(Guid utilizadorId, Guid? dispositivoId = null, CancellationToken cancellationToken = default);
}
