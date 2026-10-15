using Menova.Application.Interfaces;
using Menova.Domain.Sala;
using Menova.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Menova.Web.Services;

public class NotificacaoService : INotificacaoService
{
    private readonly IHubContext<MenovaHub> _hubContext;
    private readonly ILogger<NotificacaoService> _logger;

    public NotificacaoService(IHubContext<MenovaHub> hubContext, ILogger<NotificacaoService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotificarMesaAlteradaAsync(Guid mesaId, EstadoMesa novoEstado, string numeroMesa, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("MesaAlterada", new
            {
                MesaId = mesaId,
                NovoEstado = (int)novoEstado,
                EstadoDescricao = novoEstado.ToString(),
                Numero = numeroMesa,
                AtualizadoEm = DateTime.UtcNow
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao emitir notificação SignalR de alteração de mesa.");
        }
    }

    public async Task NotificarArtigoEsgotadoAlteradoAsync(Guid artigoId, string nome, bool esgotado, long menuVersao, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("Artigo86Alterado", new
            {
                ArtigoId = artigoId,
                Nome = nome,
                EsgotadoManual = esgotado,
                MenuVersao = menuVersao,
                AtualizadoEm = DateTime.UtcNow
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao emitir notificação SignalR de artigo esgotado (86).");
        }
    }

    public async Task NotificarNovoPedidoAsync(Guid pedidoId, string numeroMesa, Guid? estacaoId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                PedidoId = pedidoId,
                NumeroMesa = numeroMesa,
                EstacaoId = estacaoId,
                CriadoEm = DateTime.UtcNow
            };

            if (estacaoId.HasValue)
            {
                await _hubContext.Clients.Group($"kds_{estacaoId.Value}").SendAsync("NovoPedido", payload, cancellationToken);
            }
            await _hubContext.Clients.Group("kds_geral").SendAsync("NovoPedido", payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao emitir notificação de novo pedido.");
        }
    }

    public async Task NotificarEstadoItemPedidoAsync(Guid itemId, string novoEstado, Guid? estacaoId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("EstadoItemAlterado", new
            {
                ItemId = itemId,
                NovoEstado = novoEstado,
                EstacaoId = estacaoId,
                AtualizadoEm = DateTime.UtcNow
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao emitir notificação de estado de item de pedido.");
        }
    }

    public async Task NotificarAlertaCaixaAsync(string mensagem, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group("caixa").SendAsync("AlertaCaixa", new
            {
                Mensagem = mensagem,
                DataHora = DateTime.UtcNow
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao emitir alerta de caixa.");
        }
    }
}
