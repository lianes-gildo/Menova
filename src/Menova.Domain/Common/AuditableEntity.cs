namespace Menova.Domain.Common;

public abstract class AuditableEntity : BaseEntity
{
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public string? CriadoPor { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public string? AtualizadoPor { get; set; }
    public uint Versao { get; set; } = 1;
}

public abstract class AuditableEntity<TKey> : BaseEntity<TKey>
{
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public string? CriadoPor { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public string? AtualizadoPor { get; set; }
    public uint Versao { get; set; } = 1;
}
