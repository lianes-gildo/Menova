using Menova.Application.DTOs;
using Menova.Domain.Pedidos;

namespace Menova.Application.Interfaces;

public interface IKdsService
{
    Task<List<TicketKdsDto>> GetTicketsAtivosAsync(Guid? estacaoId = null, CancellationToken cancellationToken = default);
    Task AtualizarEstadoItemAsync(Guid itemId, EstadoItemPedido novoEstado, CancellationToken cancellationToken = default);
    Task AvancarEstadoTicketAsync(Guid pedidoId, CancellationToken cancellationToken = default);
}
