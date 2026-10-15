using Menova.Domain.Common;

namespace Menova.Domain.Relatorios;

public class FatoVendasDiarias : BaseEntity
{
    public DateOnly Data { get; set; }
    public Guid ArtigoId { get; set; }
    public string ArtigoNome { get; set; } = string.Empty;
    public Guid CategoriaId { get; set; }
    public string CategoriaNome { get; set; } = string.Empty;

    public decimal QuantidadeVendida { get; set; } = 0.000m;
    public decimal TotalBruto { get; set; } = 0.00m;
    public decimal TotalDesconto { get; set; } = 0.00m;
    public decimal TotalIva { get; set; } = 0.00m;
    public decimal TotalLiquido { get; set; } = 0.00m;

    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}
