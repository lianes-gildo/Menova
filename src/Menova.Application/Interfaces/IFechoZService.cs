using Menova.Application.DTOs;

namespace Menova.Application.Interfaces;

public interface IFechoZService
{
    Task<DocumentoZDto> EmitirFechoZAsync(Guid sessaoCaixaId, string utilizadorNome, CancellationToken ct = default);
    Task<DocumentoZDto?> ObterZPorIdAsync(Guid documentoZId, CancellationToken ct = default);
    Task<List<DocumentoZDto>> ObterHistoricoZAsync(CancellationToken ct = default);
    Task<bool> VerificarIntegridadeCadeiaAsync(CancellationToken ct = default);
}
