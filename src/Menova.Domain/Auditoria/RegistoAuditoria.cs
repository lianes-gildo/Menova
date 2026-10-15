using System.Security.Cryptography;
using System.Text;

namespace Menova.Domain.Auditoria;

public class RegistoAuditoria
{
    public long Id { get; set; }
    public DateTime DataHora { get; set; } = DateTime.UtcNow;
    public Guid? UtilizadorId { get; set; }
    public string NomeUtilizadorSnapshot { get; set; } = "sistema";
    public string? Perfil { get; set; }
    public Guid? DispositivoId { get; set; }
    public string? Ip { get; set; }
    public string Acao { get; set; } = string.Empty;
    public string Entidade { get; set; } = string.Empty;
    public string? EntidadeId { get; set; }
    public Guid? CaixaId { get; set; }
    public Guid? SessaoCaixaId { get; set; }
    public string? ValoresAntes { get; set; } // JSON
    public string? ValoresDepois { get; set; } // JSON
    public string? Motivo { get; set; }
    public string? CorrelacaoId { get; set; }
    public string HashAnterior { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;

    public static string CalcularHash(
        string hashAnterior,
        DateTime dataHoraUtc,
        string nomeUtilizador,
        string acao,
        string entidade,
        string? entidadeId,
        string? valoresAntes,
        string? valoresDepois,
        string? motivo,
        string? correlacaoId)
    {
        // Normalização canónica do payload
        var payload = $"{hashAnterior}|{dataHoraUtc:O}|{nomeUtilizador}|{acao}|{entidade}|{entidadeId ?? ""}|{valoresAntes ?? ""}|{valoresDepois ?? ""}|{motivo ?? ""}|{correlacaoId ?? ""}";
        var bytes = Encoding.UTF8.GetBytes(payload);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
