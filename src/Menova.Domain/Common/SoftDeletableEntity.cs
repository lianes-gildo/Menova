namespace Menova.Domain.Common;

public abstract class SoftDeletableEntity : AuditableEntity
{
    public bool EstaEliminado { get; set; } = false;
    public DateTime? EliminadoEm { get; set; }
    public string? EliminadoPor { get; set; }
}

public abstract class SoftDeletableEntity<TKey> : AuditableEntity<TKey>
{
    public bool EstaEliminado { get; set; } = false;
    public DateTime? EliminadoEm { get; set; }
    public string? EliminadoPor { get; set; }
}
