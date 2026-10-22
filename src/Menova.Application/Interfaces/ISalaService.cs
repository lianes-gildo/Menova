using Menova.Application.DTOs;
using Menova.Domain.Sala;

namespace Menova.Application.Interfaces;

public interface ISalaService
{
    Task<List<ZonaDto>> GetZonasComMesasAsync(CancellationToken cancellationToken = default);
    Task<MesaDto?> GetMesaPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AtualizarEstadoMesaAsync(Guid mesaId, EstadoMesa novoEstado, string? motivo = null, CancellationToken cancellationToken = default);
    Task<Guid> CriarMesaAsync(CriarMesaDto dto, CancellationToken cancellationToken = default);
    Task AtualizarMesaAsync(AtualizarMesaDto dto, CancellationToken cancellationToken = default);
    Task EliminarMesaAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CriarZonaAsync(CriarZonaDto dto, CancellationToken cancellationToken = default);
    Task AtualizarZonaAsync(AtualizarZonaDto dto, CancellationToken cancellationToken = default);
    Task EliminarZonaAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ZonaDto?> GetZonaPorIdAsync(Guid id, CancellationToken cancellationToken = default);
}
