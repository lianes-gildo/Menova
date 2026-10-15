using System.Text.Json;
using Menova.Application.DTOs;
using Menova.Application.Interfaces;
using Menova.Domain.Pedidos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Menova.Infrastructure.Services;

public class PedidoService : IPedidoService
{
    private readonly IAppDbContext _context;
    private readonly IContaService _contaService;
    private readonly INotificacaoService _notificacao;
    private readonly IAuditoriaService _auditoria;
    private readonly IPinService _pinService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<PedidoService> _logger;

    public PedidoService(
        IAppDbContext context,
        IContaService contaService,
        INotificacaoService notificacao,
        IAuditoriaService auditoria,
        IPinService pinService,
        ICurrentUserService currentUser,
        ILogger<PedidoService> logger)
    {
        _context = context;
        _contaService = contaService;
        _notificacao = notificacao;
        _auditoria = auditoria;
        _pinService = pinService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PedidoDto> CriarPedidoAsync(CriarPedidoDto dto, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    {
        // 1. Idempotência: verificar se a chave já foi processada
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var chaveExistente = await _context.ChavesIdempotencia
                .FirstOrDefaultAsync(c => c.Chave == idempotencyKey && c.ExpiraEm > DateTime.UtcNow, cancellationToken);

            if (chaveExistente != null)
            {
                var resultadoCached = JsonSerializer.Deserialize<PedidoDto>(chaveExistente.ResultadoJson);
                if (resultadoCached != null)
                {
                    _logger.LogInformation("Pedido idempotente retornado para chave {Key}", idempotencyKey);
                    return resultadoCached;
                }
            }
        }

        if (dto.Itens == null || !dto.Itens.Any())
        {
            throw new ArgumentException("O pedido deve conter pelo menos um artigo.");
        }

        // 2. Identificar ou abrir a Conta
        Guid contaId;
        if (dto.ContaId.HasValue)
        {
            contaId = dto.ContaId.Value;
        }
        else if (dto.MesaId.HasValue)
        {
            var contaAberta = await _context.Contas
                .FirstOrDefaultAsync(c => c.MesaId == dto.MesaId.Value && c.Estado == EstadoConta.Aberta, cancellationToken);

            if (contaAberta != null)
            {
                contaId = contaAberta.Id;
            }
            else
            {
                var novaConta = await _contaService.AbrirContaAsync(dto.MesaId, dto.Observacoes, cancellationToken);
                contaId = novaConta.Id;
            }
        }
        else
        {
            var novaContaBalcao = await _contaService.AbrirContaAsync(null, dto.Observacoes, cancellationToken);
            contaId = novaContaBalcao.Id;
        }

        // 3. Obter próximo número sequencial de pedido para esta conta
        var totalPedidosNaConta = await _context.Pedidos
            .CountAsync(p => p.ContaId == contaId, cancellationToken);

        var pedido = new Pedido
        {
            ContaId = contaId,
            NumeroSequencial = totalPedidosNaConta + 1,
            Estado = EstadoPedido.Pendente,
            Observacoes = dto.Observacoes
        };

        // 4. Processar Itens
        var artigosIds = dto.Itens.Select(i => i.ArtigoId).Distinct().ToList();
        var artigos = await _context.Artigos
            .Include(a => a.Categoria)
            .Include(a => a.Estacao)
            .Where(a => artigosIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        var todasOpcoesIds = dto.Itens
            .Where(i => i.OpcoesIds != null && i.OpcoesIds.Any())
            .SelectMany(i => i.OpcoesIds!)
            .Distinct()
            .ToList();

        var opcoes = await _context.Opcoes
            .Where(o => todasOpcoesIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, cancellationToken);

        var estacoesNotificar = new HashSet<Guid>();

        foreach (var itemEntrada in dto.Itens)
        {
            if (!artigos.TryGetValue(itemEntrada.ArtigoId, out var artigo))
            {
                throw new KeyNotFoundException($"Artigo {itemEntrada.ArtigoId} não foi encontrado.");
            }

            // Regra 86: Validar se artigo está esgotado
            if (artigo.EsgotadoManual || !artigo.Disponivel || !artigo.Ativo)
            {
                throw new InvalidOperationException($"O artigo '{artigo.Nome}' está ESGOTADO (86) ou indisponível para pedidos.");
            }

            var estacaoId = artigo.EstacaoId ?? artigo.Categoria.EstacaoPadraoId;
            if (estacaoId.HasValue) estacoesNotificar.Add(estacaoId.Value);

            // Calcular preço base e acréscimos das opções
            var precoUnitario = artigo.Preco;
            var opcoesSelecionadas = new List<ItemPedidoOpcao>();

            if (itemEntrada.OpcoesIds != null && itemEntrada.OpcoesIds.Any())
            {
                foreach (var opcaoId in itemEntrada.OpcoesIds)
                {
                    if (opcoes.TryGetValue(opcaoId, out var opcao))
                    {
                        precoUnitario += opcao.DeltaPreco;
                        opcoesSelecionadas.Add(new ItemPedidoOpcao
                        {
                            OpcaoId = opcao.Id,
                            NomeOpcao = opcao.Nome,
                            DeltaPreco = opcao.DeltaPreco
                        });
                    }
                }
            }

            var subtotal = Math.Round(precoUnitario * itemEntrada.Quantidade, 2);
            var taxaIva = artigo.TaxaIva;
            // Cálculo do IVA em Moçambique: IVA incluído no preço de venda ao público
            // TotalLiquido = Subtotal; SubtotalSemIva = Subtotal / (1 + taxaIva/100)
            var valorSemIva = Math.Round(subtotal / (1 + (taxaIva / 100m)), 4);
            var totalIva = subtotal - valorSemIva;

            var itemPedido = new ItemPedido
            {
                ArtigoId = artigo.Id,
                NomeArtigo = artigo.Nome,
                Quantidade = itemEntrada.Quantidade,
                PrecoUnitario = precoUnitario,
                TaxaIva = taxaIva,
                Subtotal = valorSemIva,
                TotalIva = totalIva,
                TotalLiquido = subtotal,
                EstacaoId = estacaoId,
                Estado = EstadoItemPedido.Pendente,
                Notas = itemEntrada.Notas,
                Anulado = false,
                OpcoesSelecionadas = opcoesSelecionadas
            };

            pedido.Itens.Add(itemPedido);
        }

        _context.Pedidos.Add(pedido);
        await _context.SaveChangesAsync(cancellationToken);

        // 5. Recalcular totais da conta
        await _contaService.RecalcularTotaisContaAsync(contaId, cancellationToken);

        // 6. Notificações SignalR em tempo real para KDS e Sala
        var contaInfo = await _context.Contas.Include(c => c.Mesa).FirstOrDefaultAsync(c => c.Id == contaId, cancellationToken);
        var numeroMesa = contaInfo?.Mesa?.Numero ?? "Balcão";

        foreach (var estId in estacoesNotificar)
        {
            await _notificacao.NotificarNovoPedidoAsync(pedido.Id, numeroMesa, estId, cancellationToken);
        }
        await _notificacao.NotificarNovoPedidoAsync(pedido.Id, numeroMesa, null, cancellationToken);

        // 7. Auditoria
        await _auditoria.RegistarAsync(
            acao: "PEDIDO.CRIAR",
            entidade: "pedidos",
            entidadeId: pedido.Id.ToString(),
            valoresDepois: new { PedidoId = pedido.Id, ContaId = contaId, TotalItens = pedido.Itens.Count },
            motivo: $"Novo pedido nº {pedido.NumeroSequencial} para {numeroMesa}",
            cancellationToken: cancellationToken);

        var pedidoDto = await GetPedidoPorIdAsync(pedido.Id, cancellationToken);

        // 8. Registar chave de idempotência se fornecida
        if (!string.IsNullOrWhiteSpace(idempotencyKey) && pedidoDto != null)
        {
            _context.ChavesIdempotencia.Add(new ChaveIdempotencia
            {
                Chave = idempotencyKey,
                ResultadoJson = JsonSerializer.Serialize(pedidoDto),
                CriadoEm = DateTime.UtcNow,
                ExpiraEm = DateTime.UtcNow.AddHours(24)
            });
            await _context.SaveChangesAsync(cancellationToken);
        }

        return pedidoDto!;
    }

    public async Task<PedidoDto?> GetPedidoPorIdAsync(Guid pedidoId, CancellationToken cancellationToken = default)
    {
        var p = await _context.Pedidos
            .AsNoTracking()
            .Include(p => p.Itens).ThenInclude(i => i.Estacao)
            .Include(p => p.Itens).ThenInclude(i => i.OpcoesSelecionadas)
            .FirstOrDefaultAsync(p => p.Id == pedidoId, cancellationToken);

        if (p == null) return null;

        return new PedidoDto(
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
        );
    }

    public async Task AnularItemPedidoAsync(AnularItemDto dto, CancellationToken cancellationToken = default)
    {
        var item = await _context.ItensPedido
            .Include(i => i.Pedido)
            .FirstOrDefaultAsync(i => i.Id == dto.ItemId, cancellationToken)
            ?? throw new KeyNotFoundException($"Item de pedido {dto.ItemId} não encontrado.");

        if (item.Anulado)
        {
            throw new InvalidOperationException("O item já se encontra anulado.");
        }

        // Se o item já começou a ser preparado, exige autorização com PIN de supervisor
        if (item.Estado != EstadoItemPedido.Pendente)
        {
            if (string.IsNullOrWhiteSpace(dto.PinSupervisor))
            {
                throw new UnauthorizedAccessException("É necessário PIN de supervisor para anular itens já enviados para a cozinha.");
            }

            var supervisorValido = await _pinService.ValidarPinSupervisorAsync(dto.PinSupervisor, cancellationToken);
            if (!supervisorValido)
            {
                throw new UnauthorizedAccessException("PIN de supervisor inválido.");
            }
        }

        item.Anulado = true;
        item.Estado = EstadoItemPedido.Anulado;
        item.AnuladoMotivo = dto.Motivo;
        item.AnuladoEm = DateTime.UtcNow;
        item.AnuladoPor = _currentUser.NomeUtilizador;

        await _context.SaveChangesAsync(cancellationToken);

        // Recalcular totais da conta
        await _contaService.RecalcularTotaisContaAsync(item.Pedido.ContaId, cancellationToken);

        // Notificar KDS
        await _notificacao.NotificarEstadoItemPedidoAsync(item.Id, EstadoItemPedido.Anulado.ToString(), item.EstacaoId, cancellationToken);

        await _auditoria.RegistarAsync(
            acao: "ITEM_PEDIDO.ANULAR",
            entidade: "itens_pedido",
            entidadeId: item.Id.ToString(),
            valoresAntes: new { item.NomeArtigo, item.Quantidade, Anulado = false },
            valoresDepois: new { Anulado = true, item.AnuladoMotivo },
            motivo: dto.Motivo,
            cancellationToken: cancellationToken);

        _logger.LogWarning("Item {NomeArtigo} do Pedido {PedidoId} anulado por {Utilizador}. Motivo: {Motivo}",
            item.NomeArtigo, item.PedidoId, _currentUser.NomeUtilizador, dto.Motivo);
    }
}
