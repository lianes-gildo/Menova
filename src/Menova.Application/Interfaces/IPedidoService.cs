using Menova.Application.DTOs;

namespace Menova.Application.Interfaces;

public interface IPedidoService
{
    Task<PedidoDto> CriarPedidoAsync(CriarPedidoDto dto, string? idempotencyKey = null, CancellationToken cancellationToken = default);
    Task<PedidoDto?> GetPedidoPorIdAsync(Guid pedidoId, CancellationToken cancellationToken = default);
    Task AnularItemPedidoAsync(AnularItemDto dto, CancellationToken cancellationToken = default);
}
