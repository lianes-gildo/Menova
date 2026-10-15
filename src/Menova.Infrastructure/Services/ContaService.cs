using Menova.Application.DTOs;
using Menova.Application.Interfaces;
using Menova.Domain.Pedidos;
using Menova.Domain.Sala;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Menova.Infrastructure.Services;

public class ContaService : IContaService
{
    private readonly IAppDbContext _context;
    private readonly IAuditoriaService _auditoria;
    private readonly INotificacaoService _notificacao;
    private readonly IPinService _pinService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ContaService> _logger;

    public ContaService(
        IAppDbContext context,
        IAuditoriaService auditoria,
        INotificacaoService notificacao,
        IPinService pinService,
        ICurrentUserService currentUser,
        ILogger<ContaService> logger)
    {
        _context = context;
        _auditoria = auditoria;
        _notificacao = notificacao;
        _pinService = pinService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ContaDto> AbrirContaAsync(Guid? mesaId, string? observacoes = null, CancellationToken cancellationToken = default)
    {
        if (mesaId.HasValue)
        {
            var contaAbertaExistente = await _context.Contas
                .FirstOrDefaultAsync(c => c.MesaId == mesaId.Value && c.Estado == EstadoConta.Aberta, cancellationToken);

            if (contaAbertaExistente != null)
            {
                var dtoExistente = await GetContaPorIdAsync(contaAbertaExistente.Id, cancellationToken);
                if (dtoExistente != null) return dtoExistente;
            }
        }

        var ano = DateTime.UtcNow.Year;
        var seq = await _context.Sequencias.FirstOrDefaultAsync(s => s.Serie == "CONTA" && s.Ano == ano, cancellationToken);
        if (seq == null)
        {
            seq = new Domain.Configuracao.Sequencia { Serie = "CONTA", Ano = ano, UltimoNumero = 1 };
            _context.Sequencias.Add(seq);
        }
        else
        {
            seq.UltimoNumero++;
        }

        var numeroConta = $"C{ano % 100}{seq.UltimoNumero:D5}";

        var conta = new Conta
        {
            Numero = numeroConta,
            MesaId = mesaId,
            Estado = EstadoConta.Aberta,
            Observacoes = observacoes,
            TotalBruto = 0m,
            TotalDesconto = 0m,
            TotalIva = 0m,
            TotalLiquido = 0m
        };

        _context.Contas.Add(conta);

        if (mesaId.HasValue)
        {
            var mesa = await _context.Mesas.FirstOrDefaultAsync(m => m.Id == mesaId.Value, cancellationToken);
            if (mesa != null)
            {
                mesa.Estado = EstadoMesa.Ocupada;
                await _notificacao.NotificarMesaAlteradaAsync(mesa.Id, EstadoMesa.Ocupada, mesa.Numero, cancellationToken);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditoria.RegistarAsync(
            acao: "CONTA.ABRIR",
            entidade: "contas",
            entidadeId: conta.Id.ToString(),
            valoresDepois: new { conta.Numero, conta.MesaId },
            motivo: mesaId.HasValue ? $"Abertura de conta na mesa" : "Abertura de conta de balcão",
            cancellationToken: cancellationToken);

        var dto = await GetContaPorIdAsync(conta.Id, cancellationToken);
        return dto!;
    }

    public async Task<ContaDto?> GetContaPorIdAsync(Guid contaId, CancellationToken cancellationToken = default)
    {
        var conta = await _context.Contas
            .AsNoTracking()
            .Include(c => c.Mesa).ThenInclude(m => m != null ? m.Zona : null)
            .Include(c => c.Pedidos).ThenInclude(p => p.Itens).ThenInclude(i => i.OpcoesSelecionadas)
            .Include(c => c.Pedidos).ThenInclude(p => p.Itens).ThenInclude(i => i.Estacao)
            .FirstOrDefaultAsync(c => c.Id == contaId, cancellationToken);

        if (conta == null) return null;

        return MapearContaParaDto(conta);
    }

    public async Task<ContaDto?> GetContaAtivaPorMesaAsync(Guid mesaId, CancellationToken cancellationToken = default)
    {
        var conta = await _context.Contas
            .AsNoTracking()
            .Include(c => c.Mesa).ThenInclude(m => m != null ? m.Zona : null)
            .Include(c => c.Pedidos).ThenInclude(p => p.Itens).ThenInclude(i => i.OpcoesSelecionadas)
            .Include(c => c.Pedidos).ThenInclude(p => p.Itens).ThenInclude(i => i.Estacao)
            .FirstOrDefaultAsync(c => c.MesaId == mesaId && c.Estado == EstadoConta.Aberta, cancellationToken);

        if (conta == null) return null;

        return MapearContaParaDto(conta);
    }

    public async Task<List<ContaDto>> ListarContasAbertasAsync(CancellationToken cancellationToken = default)
    {
        var contas = await _context.Contas
            .AsNoTracking()
            .Include(c => c.Mesa).ThenInclude(m => m != null ? m.Zona : null)
            .Include(c => c.Pedidos).ThenInclude(p => p.Itens).ThenInclude(i => i.OpcoesSelecionadas)
            .Include(c => c.Pedidos).ThenInclude(p => p.Itens).ThenInclude(i => i.Estacao)
            .Where(c => c.Estado == EstadoConta.Aberta)
            .OrderByDescending(c => c.CriadoEm)
            .ToListAsync(cancellationToken);

        return contas.Select(MapearContaParaDto).ToList();
    }

    public async Task AnularContaAsync(Guid contaId, string motivo, string? pinSupervisor = null, CancellationToken cancellationToken = default)
    {
        var conta = await _context.Contas
            .Include(c => c.Pedidos).ThenInclude(p => p.Itens)
            .FirstOrDefaultAsync(c => c.Id == contaId, cancellationToken)
            ?? throw new KeyNotFoundException($"Conta {contaId} não encontrada.");

        if (conta.Estado != EstadoConta.Aberta)
        {
            throw new InvalidOperationException("Apenas contas abertas podem ser anuladas.");
        }

        conta.Estado = EstadoConta.Anulada;
        conta.Observacoes = $"ANULADA: {motivo}";

        if (conta.MesaId.HasValue)
        {
            var mesa = await _context.Mesas.FirstOrDefaultAsync(m => m.Id == conta.MesaId.Value, cancellationToken);
            if (mesa != null)
            {
                mesa.Estado = EstadoMesa.Livre;
                await _notificacao.NotificarMesaAlteradaAsync(mesa.Id, EstadoMesa.Livre, mesa.Numero, cancellationToken);
            }
        }

        // Anular também os itens associados
        foreach (var pedido in conta.Pedidos)
        {
            pedido.Estado = EstadoPedido.Cancelado;
            foreach (var item in pedido.Itens.Where(i => !i.Anulado))
            {
                item.Anulado = true;
                item.Estado = EstadoItemPedido.Anulado;
                item.AnuladoMotivo = $"Conta anulada: {motivo}";
                item.AnuladoEm = DateTime.UtcNow;
                item.AnuladoPor = _currentUser.NomeUtilizador;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditoria.RegistarAsync(
            acao: "CONTA.ANULAR",
            entidade: "contas",
            entidadeId: conta.Id.ToString(),
            valoresAntes: new { Estado = EstadoConta.Aberta },
            valoresDepois: new { Estado = EstadoConta.Anulada },
            motivo: motivo,
            cancellationToken: cancellationToken);

        _logger.LogWarning("Conta {Numero} ({Id}) anulada por {Utilizador}. Motivo: {Motivo}", conta.Numero, conta.Id, _currentUser.NomeUtilizador, motivo);
    }

    public async Task RecalcularTotaisContaAsync(Guid contaId, CancellationToken cancellationToken = default)
    {
        var conta = await _context.Contas
            .Include(c => c.Pedidos).ThenInclude(p => p.Itens)
            .FirstOrDefaultAsync(c => c.Id == contaId, cancellationToken);

        if (conta == null) return;

        var itensAtivos = conta.Pedidos
            .Where(p => p.Estado != EstadoPedido.Cancelado)
            .SelectMany(p => p.Itens)
            .Where(i => !i.Anulado)
            .ToList();

        conta.TotalBruto = itensAtivos.Sum(i => i.Subtotal);
        conta.TotalIva = itensAtivos.Sum(i => i.TotalIva);
        conta.TotalLiquido = itensAtivos.Sum(i => i.TotalLiquido) - conta.TotalDesconto;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static ContaDto MapearContaParaDto(Conta c)
    {
        var pedidosDtos = c.Pedidos
            .OrderBy(p => p.NumeroSequencial)
            .Select(p => new PedidoDto(
                p.Id,
                p.ContaId,
                p.NumeroSequencial,
                p.Estado,
                p.Estado.ToString(),
                p.Observacoes,
                p.CriadoEm,
                p.CriadoPor ?? "sistema",
                p.Itens.Select(i => new ItemPedidoDto(
                    i.Id,
                    i.ArtigoId,
                    i.NomeArtigo,
                    i.Quantidade,
                    i.PrecoUnitario,
                    i.TaxaIva,
                    i.Subtotal,
                    i.TotalIva,
                    i.TotalLiquido,
                    i.EstacaoId,
                    i.Estacao?.Nome,
                    i.Estado,
                    i.Estado.ToString(),
                    i.Notas,
                    i.Anulado,
                    i.AnuladoMotivo,
                    i.OpcoesSelecionadas.Select(o => o.NomeOpcao).ToList()
                )).ToList()
            )).ToList();

        return new ContaDto(
            c.Id,
            c.Numero,
            c.MesaId,
            c.Mesa?.Numero,
            c.Mesa?.Zona?.Nome,
            c.Estado,
            c.Estado.ToString(),
            c.TotalBruto,
            c.TotalDesconto,
            c.TotalIva,
            c.TotalLiquido,
            c.Observacoes,
            c.CriadoEm,
            c.CriadoPor ?? "sistema",
            pedidosDtos
        );
    }
}
