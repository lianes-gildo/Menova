using Menova.Domain.Common;
using Menova.Domain.Estacoes;
using Menova.Domain.Menu;
using Menova.Domain.Sala;

namespace Menova.Domain.Pedidos;

public enum EstadoConta
{
    Aberta = 1,
    Fechada = 2,
    Anulada = 3
}

public enum EstadoPedido
{
    Pendente = 1,
    EmPreparacao = 2,
    Pronto = 3,
    Entregue = 4,
    Cancelado = 5
}

public enum EstadoItemPedido
{
    Pendente = 1,
    EmPreparacao = 2,
    Pronto = 3,
    Entregue = 4,
    Anulado = 5
}

public class Conta : AuditableEntity
{
    public string Numero { get; set; } = string.Empty;
    public Guid? MesaId { get; set; }
    public Mesa? Mesa { get; set; }
    public EstadoConta Estado { get; set; } = EstadoConta.Aberta;

    public decimal TotalBruto { get; set; } = 0.00m;
    public decimal TotalDesconto { get; set; } = 0.00m;
    public decimal TotalIva { get; set; } = 0.00m;
    public decimal TotalLiquido { get; set; } = 0.00m;

    public string? Observacoes { get; set; }
    public DateTime? FechadaEm { get; set; }
    public string? FechadaPor { get; set; }

    public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
}

public class Pedido : AuditableEntity
{
    public Guid ContaId { get; set; }
    public Conta Conta { get; set; } = null!;

    public int NumeroSequencial { get; set; } = 1;
    public EstadoPedido Estado { get; set; } = EstadoPedido.Pendente;
    public string? Observacoes { get; set; }

    public ICollection<ItemPedido> Itens { get; set; } = new List<ItemPedido>();
}

public class ItemPedido : AuditableEntity
{
    public Guid PedidoId { get; set; }
    public Pedido Pedido { get; set; } = null!;

    public Guid ArtigoId { get; set; }
    public Artigo Artigo { get; set; } = null!;

    public string NomeArtigo { get; set; } = string.Empty;
    public decimal Quantidade { get; set; } = 1.000m;
    public decimal PrecoUnitario { get; set; } = 0.00m;
    public decimal TaxaIva { get; set; } = 16.00m; // Padrão Moçambique: 16%

    public decimal Subtotal { get; set; } = 0.00m;
    public decimal TotalIva { get; set; } = 0.00m;
    public decimal TotalLiquido { get; set; } = 0.00m;

    public Guid? EstacaoId { get; set; }
    public EstacaoPreparacao? Estacao { get; set; }

    public EstadoItemPedido Estado { get; set; } = EstadoItemPedido.Pendente;
    public string? Notas { get; set; }

    // Campos de Anulação
    public bool Anulado { get; set; } = false;
    public string? AnuladoMotivo { get; set; }
    public string? AnuladoPor { get; set; }
    public DateTime? AnuladoEm { get; set; }

    public ICollection<ItemPedidoOpcao> OpcoesSelecionadas { get; set; } = new List<ItemPedidoOpcao>();
}

public class ItemPedidoOpcao : BaseEntity
{
    public Guid ItemPedidoId { get; set; }
    public ItemPedido ItemPedido { get; set; } = null!;

    public Guid OpcaoId { get; set; }
    public Opcao Opcao { get; set; } = null!;

    public string NomeOpcao { get; set; } = string.Empty;
    public decimal DeltaPreco { get; set; } = 0.00m;
}

public class ChaveIdempotencia
{
    public string Chave { get; set; } = string.Empty;
    public string ResultadoJson { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime ExpiraEm { get; set; } = DateTime.UtcNow.AddHours(24);
}
