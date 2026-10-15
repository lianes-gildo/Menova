using Menova.Domain.Common;

namespace Menova.Domain.Estacoes;

public class EstacaoPreparacao : SoftDeletableEntity
{
    public string Nome { get; set; } = string.Empty;
    public bool TemEcra { get; set; } = true;
    public bool ImprimeTalao { get; set; } = false;
    public Guid? ImpressoraId { get; set; }
    public bool Ativa { get; set; } = true;
}
