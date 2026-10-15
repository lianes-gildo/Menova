using Menova.Domain.Common;

namespace Menova.Domain.Identity;

public class Permissao
{
    public string Codigo { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;

    public ICollection<PerfilPermissao> PerfisPermissoes { get; set; } = new List<PerfilPermissao>();
}

public class Perfil : SoftDeletableEntity
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public bool ESistema { get; set; } = false;
    public bool Ativo { get; set; } = true;

    public ICollection<PerfilPermissao> PerfisPermissoes { get; set; } = new List<PerfilPermissao>();
    public ICollection<UtilizadorPerfil> UtilizadoresPerfis { get; set; } = new List<UtilizadorPerfil>();
}

public class PerfilPermissao
{
    public Guid PerfilId { get; set; }
    public Perfil Perfil { get; set; } = null!;

    public string PermissaoCodigo { get; set; } = string.Empty;
    public Permissao Permissao { get; set; } = null!;
}
