using Menova.Domain.Sala;

namespace Menova.Application.Interfaces;

public interface INotificacaoService
{
    Task NotificarMesaAlteradaAsync(Guid mesaId, EstadoMesa novoEstado, string numeroMesa, CancellationToken cancellationToken = default);
    Task NotificarArtigoEsgotadoAlteradoAsync(Guid artigoId, string nome, bool esgotado, long menuVersao, CancellationToken cancellationToken = default);
    Task NotificarNovoPedidoAsync(Guid pedidoId, string numeroMesa, Guid? estacaoId = null, CancellationToken cancellationToken = default);
    Task NotificarEstadoItemPedidoAsync(Guid itemId, string novoEstado, Guid? estacaoId = null, CancellationToken cancellationToken = default);
    Task NotificarAlertaCaixaAsync(string mensagem, CancellationToken cancellationToken = default);
}
