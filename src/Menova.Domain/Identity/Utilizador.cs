using Menova.Domain.Common;

namespace Menova.Domain.Identity;

public class Utilizador : SoftDeletableEntity
{
    public string NomeCompleto { get; set; } = string.Empty;
    public string NomeUtilizador { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string HashPalavraPasse { get; set; } = string.Empty;
    public string? HashPin { get; set; }
    public string Foto { get; set; } = "default.jpg";
    public bool Ativo { get; set; } = true;
    public DateTime? UltimoLoginEm { get; set; }
    public int TentativasFalhadas { get; set; } = 0;
    public DateTime? BloqueadoAte { get; set; }
    public bool DeveAlterarPalavraPasse { get; set; } = false;
    public string SeloSeguranca { get; set; } = Guid.NewGuid().ToString("N");

    public ICollection<UtilizadorPerfil> UtilizadoresPerfis { get; set; } = new List<UtilizadorPerfil>();
    public ICollection<SessaoUtilizador> Sessoes { get; set; } = new List<SessaoUtilizador>();
    public ICollection<TentativaLogin> TentativasLogin { get; set; } = new List<TentativaLogin>();

    public bool EstaBloqueado()
    {
        return BloqueadoAte.HasValue && BloqueadoAte.Value > DateTime.UtcNow;
    }

    public void RegistarSucessoLogin()
    {
        TentativasFalhadas = 0;
        BloqueadoAte = null;
        UltimoLoginEm = DateTime.UtcNow;
    }

    public void RegistarFalhaLogin(int maxTentativas = 5, int minutosBloqueio = 15)
    {
        TentativasFalhadas++;
        if (TentativasFalhadas >= maxTentativas)
        {
            BloqueadoAte = DateTime.UtcNow.AddMinutes(minutosBloqueio);
        }
    }
}

public class UtilizadorPerfil
{
    public Guid UtilizadorId { get; set; }
    public Utilizador Utilizador { get; set; } = null!;

    public Guid PerfilId { get; set; }
    public Perfil Perfil { get; set; } = null!;
}
