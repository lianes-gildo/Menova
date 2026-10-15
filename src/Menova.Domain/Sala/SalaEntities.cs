using Menova.Domain.Common;

namespace Menova.Domain.Sala;

public enum EstadoMesa
{
    Livre = 1,
    Ocupada = 2,
    ContaPedida = 3,
    Reservada = 4,
    Bloqueada = 5
}

public class Zona : SoftDeletableEntity
{
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; } = 0;
    public bool Ativa { get; set; } = true;

    public ICollection<Mesa> Mesas { get; set; } = new List<Mesa>();
}

public class Mesa : SoftDeletableEntity
{
    public Guid ZonaId { get; set; }
    public Zona Zona { get; set; } = null!;

    public string Numero { get; set; } = string.Empty;
    public int Lugares { get; set; } = 4;
    public int PosicaoX { get; set; } = 0;
    public int PosicaoY { get; set; } = 0;
    public EstadoMesa Estado { get; set; } = EstadoMesa.Livre;
    public bool Ativa { get; set; } = true;

    public ICollection<ContaMesa> ContasMesas { get; set; } = new List<ContaMesa>();
}

public class ContaMesa
{
    public Guid ContaId { get; set; }
    public Guid MesaId { get; set; }
    public Mesa Mesa { get; set; } = null!;
    public DateTime AdicionadaEm { get; set; } = DateTime.UtcNow;
}
