using Menova.Domain.Common;

namespace Menova.Domain.Identity;

public class Dispositivo : SoftDeletableEntity
{
    public string Nome { get; set; } = string.Empty;
    public TipoDispositivo Tipo { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public EstadoDispositivo Estado { get; set; } = EstadoDispositivo.Pendente;
    public string? AutorizadoPor { get; set; }
    public DateTime? AutorizadoEm { get; set; }
    public DateTime? UltimoAcessoEm { get; set; }
    public string? UltimoIp { get; set; }
    public string? UserAgent { get; set; }

    public ICollection<SessaoUtilizador> Sessoes { get; set; } = new List<SessaoUtilizador>();
}

public class SessaoUtilizador : BaseEntity
{
    public Guid UtilizadorId { get; set; }
    public Utilizador Utilizador { get; set; } = null!;

    public Guid? DispositivoId { get; set; }
    public Dispositivo? Dispositivo { get; set; }

    public DateTime Inicio { get; set; } = DateTime.UtcNow;
    public DateTime UltimoAcesso { get; set; } = DateTime.UtcNow;
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public bool Revogada { get; set; } = false;
}

public class TentativaLogin
{
    public long Id { get; set; }
    public Guid? UtilizadorId { get; set; }
    public Utilizador? Utilizador { get; set; }

    public string NomeTentado { get; set; } = string.Empty;
    public string? Ip { get; set; }
    public Guid? DispositivoId { get; set; }
    public Dispositivo? Dispositivo { get; set; }
    public bool Sucesso { get; set; }
    public string? Motivo { get; set; }
    public DateTime DataHora { get; set; } = DateTime.UtcNow;
}
