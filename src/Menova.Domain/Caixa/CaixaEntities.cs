using Menova.Domain.Common;
using Menova.Domain.Identity;
using Menova.Domain.Pedidos;

namespace Menova.Domain.Caixa;

public enum EstadoSessaoCaixa
{
    Aberta = 1,
    Fechada = 2
}

public enum TipoMovimentoCaixa
{
    Entrada = 1,
    Saida = 2,
    Sangria = 3,
    Suprimento = 4
}

public enum MetodoPagamento
{
    Numerario = 1,
    MulticaixaPOS = 2,
    MPesaManual = 3,
    EMolaManual = 4,
    Transferencia = 5,
    Cheque = 6,
    Outro = 7
}

public class Caixa : SoftDeletableEntity
{
    public string Nome { get; set; } = string.Empty;
    public int Numero { get; set; } = 1;
    public bool Ativa { get; set; } = true;

    public ICollection<SessaoCaixa> Sessoes { get; set; } = new List<SessaoCaixa>();
}

public class SessaoCaixa : BaseEntity
{
    public Guid CaixaId { get; set; }
    public Caixa Caixa { get; set; } = null!;

    public Guid UtilizadorId { get; set; }
    public Utilizador Utilizador { get; set; } = null!;

    public DateTime AbertaEm { get; set; } = DateTime.UtcNow;
    public DateTime? FechadaEm { get; set; }

    public decimal SaldoInicial { get; set; } = 0.00m; // Fundo de caixa
    public decimal TotalVendas { get; set; } = 0.00m;
    public decimal TotalEntradas { get; set; } = 0.00m;
    public decimal TotalSaidas { get; set; } = 0.00m;
    public decimal SaldoEsperado { get; set; } = 0.00m;
    public decimal? SaldoContado { get; set; }
    public decimal? Diferenca { get; set; }

    public EstadoSessaoCaixa Estado { get; set; } = EstadoSessaoCaixa.Aberta;
    public string? Observacoes { get; set; }

    public ICollection<MovimentoCaixa> Movimentos { get; set; } = new List<MovimentoCaixa>();
    public ICollection<Pagamento> Pagamentos { get; set; } = new List<Pagamento>();
}

public class MovimentoCaixa : BaseEntity
{
    public Guid SessaoCaixaId { get; set; }
    public SessaoCaixa SessaoCaixa { get; set; } = null!;

    public TipoMovimentoCaixa Tipo { get; set; } = TipoMovimentoCaixa.Entrada;
    public decimal Valor { get; set; } = 0.00m;
    public string Motivo { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public string CriadoPor { get; set; } = string.Empty;
}

public class Pagamento : BaseEntity
{
    public Guid ContaId { get; set; }
    public Conta Conta { get; set; } = null!;

    public Guid SessaoCaixaId { get; set; }
    public SessaoCaixa SessaoCaixa { get; set; } = null!;

    public MetodoPagamento Metodo { get; set; } = MetodoPagamento.Numerario;
    public decimal Valor { get; set; } = 0.00m;
    public decimal ValorRecebido { get; set; } = 0.00m;
    public decimal Troco { get; set; } = 0.00m;
    public decimal Gorjeta { get; set; } = 0.00m;
    public string? Referencia { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public string CriadoPor { get; set; } = string.Empty;
}

public class DocumentoZ : BaseEntity
{
    public Guid SessaoCaixaId { get; set; }
    public SessaoCaixa SessaoCaixa { get; set; } = null!;

    public int NumeroZ { get; set; }
    public DateTime DataHora { get; set; } = DateTime.UtcNow;

    public decimal TotalBruto { get; set; } = 0.00m;
    public decimal TotalDesconto { get; set; } = 0.00m;
    public decimal TotalIva { get; set; } = 0.00m;
    public decimal TotalLiquido { get; set; } = 0.00m;

    // Totais discriminados em formato JSONB
    public string TotaisPorMetodo { get; set; } = "{}";
    public string TotaisPorTaxaIva { get; set; } = "{}";

    public int TotalDocumentosFaturados { get; set; } = 0;
    public string EmitidoPor { get; set; } = string.Empty;

    // Encadeamento Criptográfico SHA-256 (Tamper-evident WORM)
    public string HashAnterior { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
}
