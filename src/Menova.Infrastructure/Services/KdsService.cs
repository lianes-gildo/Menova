using Menova.Application.DTOs;
using Menova.Application.Interfaces;
using Menova.Domain.Pedidos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Menova.Infrastructure.Services;

public class KdsService : IKdsService
{
    private readonly IAppDbContext _context;
    private readonly INotificacaoService _notificacao;
    private readonly IAuditoriaService _auditoria;
    private readonly ILogger<KdsService> _logger;

    public KdsService(
        IAppDbContext context,
        INotificacaoService notificacao,
        IAuditoriaService auditoria,
        ILogger<KdsService> logger)
    {
        _context = context;
        _notificacao = notificacao;
        _auditoria = auditoria;
        _logger = logger;
    }

    public async Task<List<TicketKdsDto>> GetTicketsAtivosAsync(Guid? estacaoId = null, CancellationToken cancellationToken = default)
    {
        var pedidosQuery = _context.Pedidos
            .AsNoTracking()
            .Include(p => p.Conta).ThenInclude(c => c.Mesa).ThenInclude(m => m != null ? m.Zona : null)
            .Include(p => p.Itens).ThenInclude(i => i.OpcoesSelecionadas)
            .Include(p => p.Itens).ThenInclude(i => i.Estacao)
            .Where(p => p.Estado != EstadoPedido.Entregue && p.Estado != EstadoPedido.Cancelado);

        var pedidos = await pedidosQuery
            .OrderBy(p => p.CriadoEm)
            .ToListAsync(cancellationToken);

        var agora = DateTime.UtcNow;
        var resultado = new List<TicketKdsDto>();

        foreach (var p in pedidos)
        {
            var itensFiltrados = p.Itens
                .Where(i => !i.Anulado && i.Estado != EstadoItemPedido.Entregue);

            if (estacaoId.HasValue)
            {
                itensFiltrados = itensFiltrados.Where(i => i.EstacaoId == estacaoId.Value);
            }

            var itensList = itensFiltrados.ToList();
            if (!itensList.Any()) continue;

            var minutos = (int)(agora - p.CriadoEm).TotalMinutes;
            var urgencia = minutos switch
            {
                < 10 => UrgenciaKds.Normal,
                <= 20 => UrgenciaKds.Atencao,
                _ => UrgenciaKds.Urgente
            };

            var itensDto = itensList.Select(i => new ItemTicketKdsDto(
                i.Id,
                i.NomeArtigo,
                i.Quantidade,
                i.Notas,
                i.OpcoesSelecionadas.Select(o => o.NomeOpcao).ToList(),
                i.Estado,
                i.Estacao?.Nome
            )).ToList();

            resultado.Add(new TicketKdsDto(
                p.Id,
                p.ContaId,
                p.Conta.Mesa?.Numero,
                p.Conta.Mesa?.Zona?.Nome,
                p.NumeroSequencial,
                p.CriadoEm,
                minutos,
                urgencia,
                p.Observacoes,
                itensDto
            ));
        }

        return resultado;
    }

    public async Task AtualizarEstadoItemAsync(Guid itemId, EstadoItemPedido novoEstado, CancellationToken cancellationToken = default)
    {
        var item = await _context.ItensPedido
            .Include(i => i.Pedido).ThenInclude(p => p.Itens)
            .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken)
            ?? throw new KeyNotFoundException($"Item {itemId} não encontrado.");

        var estadoAntigo = item.Estado;
        item.Estado = novoEstado;

        // Se todos os itens ativos estiverem prontos ou entregues, atualiza o pedido
        var itensAtivos = item.Pedido.Itens.Where(i => !i.Anulado).ToList();
        if (itensAtivos.All(i => i.Estado == EstadoItemPedido.Entregue))
        {
            item.Pedido.Estado = EstadoPedido.Entregue;
        }
        else if (itensAtivos.All(i => i.Estado == EstadoItemPedido.Pronto || i.Estado == EstadoItemPedido.Entregue))
        {
            item.Pedido.Estado = EstadoPedido.Pronto;
        }
        else if (itensAtivos.Any(i => i.Estado == EstadoItemPedido.EmPreparacao))
        {
            item.Pedido.Estado = EstadoPedido.EmPreparacao;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _notificacao.NotificarEstadoItemPedidoAsync(item.Id, novoEstado.ToString(), item.EstacaoId, cancellationToken);

        _logger.LogInformation("Item KDS {NomeArtigo} ({ItemId}) alterado para {NovoEstado}", item.NomeArtigo, item.Id, novoEstado);
    }

    public async Task AvancarEstadoTicketAsync(Guid pedidoId, CancellationToken cancellationToken = default)
    {
        var pedido = await _context.Pedidos
            .Include(p => p.Itens)
            .FirstOrDefaultAsync(p => p.Id == pedidoId, cancellationToken)
            ?? throw new KeyNotFoundException($"Pedido {pedidoId} não encontrado.");

        var proximoEstado = pedido.Estado switch
        {
            EstadoPedido.Pendente => EstadoPedido.EmPreparacao,
            EstadoPedido.EmPreparacao => EstadoPedido.Pronto,
            EstadoPedido.Pronto => EstadoPedido.Entregue,
            _ => pedido.Estado
        };

        var proximoEstadoItem = proximoEstado switch
        {
            EstadoPedido.EmPreparacao => EstadoItemPedido.EmPreparacao,
            EstadoPedido.Pronto => EstadoItemPedido.Pronto,
            EstadoPedido.Entregue => EstadoItemPedido.Entregue,
            _ => EstadoItemPedido.Pendente
        };

        pedido.Estado = proximoEstado;
        foreach (var item in pedido.Itens.Where(i => !i.Anulado))
        {
            item.Estado = proximoEstadoItem;
            await _notificacao.NotificarEstadoItemPedidoAsync(item.Id, proximoEstadoItem.ToString(), item.EstacaoId, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Ticket KDS {PedidoId} avançado para {ProximoEstado}", pedido.Id, proximoEstado);
    }
}
