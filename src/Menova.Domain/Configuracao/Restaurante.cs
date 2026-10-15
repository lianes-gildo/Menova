using Menova.Domain.Common;

namespace Menova.Domain.Configuracao;

public class Restaurante : AuditableEntity
{
    public string Nome { get; set; } = "Menova Restaurante";
    public string Nuit { get; set; } = "100000000"; // NUIT de Moçambique: 9 dígitos
    public string? Morada { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? RodapeTalao { get; set; } = "Obrigado pela preferência! Volte sempre.";
    public string Moeda { get; set; } = "MZN";
    public int CasasDecimais { get; set; } = 2;
    public string FusoHorario { get; set; } = "Africa/Maputo";
    public decimal TaxaIvaPadrao { get; set; } = 16.00m; // Padrão Moçambique: 16%
}

public class Configuracao
{
    public string Chave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public string Tipo { get; set; } = "string"; // string, int, decimal, bool, json
    public string? Descricao { get; set; }
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}

public class Sequencia
{
    public string Serie { get; set; } = string.Empty;
    public int Ano { get; set; }
    public long UltimoNumero { get; set; } = 0;
}
