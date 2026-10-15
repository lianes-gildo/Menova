using System.Text.Json;
using Menova.Application.DTOs;
using Menova.Application.Interfaces;
using Menova.Domain.Caixa;
using Menova.Domain.Pedidos;
using Menova.Domain.Sala;
using Microsoft.EntityFrameworkCore;

namespace Menova.Infrastructure.Services;

public class PagamentoService : IPagamentoService
{
    private readonly IAppDbContext _context;
    private readonly IAuditoriaService _auditoriaService;
    private readonly INotificacaoService _notificacaoService;

    public PagamentoService(
        IAppDbContext context,
        IAuditoriaService auditoriaService,
        INotificacaoService notificacaoService)
    {
        _context = context;
        _auditoriaService = auditoriaService;
        _notificacaoService = notificacaoService;
    }

    public async Task<PagamentoDto> RegistarPagamentoAsync(
        PagamentoEntradaDto dto,
        string utilizadorNome,
        string? idempotencyKey = null,
        CancellationToken ct = default)
    {
        // 1. Verificar idempotência se chave fornecida
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var chaveExistente = await _context.ChavesIdempotencia
                .FirstOrDefaultAsync(c => c.Chave == idempotencyKey && c.ExpiraEm > DateTime.UtcNow, ct);

            if (chaveExistente != null)
            {
                var cachedDto = JsonSerializer.Deserialize<PagamentoDto>(chaveExistente.ResultadoJson);
                if (cachedDto != null)
                {
                    return cachedDto;
                }
            }
        }

        var conta = await _context.Contas
            .Include(c => c.Mesa)
            .FirstOrDefaultAsync(c => c.Id == dto.ContaId, ct);

        if (conta == null)
        {
            throw new InvalidOperationException("Conta não encontrada.");
        }

        if (conta.Estado != EstadoConta.Aberta)
        {
            throw new InvalidOperationException("A conta já se encontra fechada ou anulada.");
        }

        var sessao = await _context.SessoesCaixa
            .FirstOrDefaultAsync(s => s.Id == dto.SessaoCaixaId && s.Estado == EstadoSessaoCaixa.Aberta, ct);

        if (sessao == null)
        {
            throw new InvalidOperationException("Sessão de caixa não encontrada ou já encerrada.");
        }

        if (dto.Valor <= 0)
        {
            throw new InvalidOperationException("O valor a liquidar deve ser superior a zero.");
        }

        // 2. Calcular troco
        var troco = 0.00m;
        if (dto.Metodo == MetodoPagamento.Numerario && dto.ValorRecebido > dto.Valor)
        {
            troco = dto.ValorRecebido - dto.Valor;
        }

        var pagamento = new Pagamento
        {
            ContaId = dto.ContaId,
            SessaoCaixaId = dto.SessaoCaixaId,
            Metodo = dto.Metodo,
            Valor = dto.Valor,
            ValorRecebido = dto.ValorRecebido > 0 ? dto.ValorRecebido : dto.Valor,
            Troco = troco,
            Gorjeta = dto.Gorjeta,
            Referencia = dto.Referencia,
            CriadoEm = DateTime.UtcNow,
            CriadoPor = utilizadorNome
        };

        _context.Pagamentos.Add(pagamento);

        // 3. Atualizar total de vendas da sessão de caixa
        sessao.TotalVendas += dto.Valor;
        if (dto.Metodo == MetodoPagamento.Numerario)
        {
            sessao.SaldoEsperado += dto.Valor;
        }

        // 4. Verificar se a conta fica totalmente liquidada
        var pagamentosAnteriores = await _context.Pagamentos
            .Where(p => p.ContaId == dto.ContaId)
            .SumAsync(p => p.Valor, ct);

        var totalAcumulado = pagamentosAnteriores + dto.Valor;

        if (totalAcumulado >= conta.TotalLiquido)
        {
            conta.Estado = EstadoConta.Fechada;
            conta.FechadaEm = DateTime.UtcNow;
            conta.FechadaPor = utilizadorNome;

            if (conta.MesaId.HasValue)
            {
                var mesa = await _context.Mesas.FindAsync(new object[] { conta.MesaId.Value }, ct);
                if (mesa != null)
                {
                    mesa.Estado = EstadoMesa.Livre;
                    await _notificacaoService.NotificarMesaAlteradaAsync(mesa.Id, EstadoMesa.Livre, mesa.Numero, ct);
                }
            }
        }

        await _context.SaveChangesAsync(ct);

        var resultadoDto = new PagamentoDto(
            pagamento.Id,
            pagamento.ContaId,
            pagamento.SessaoCaixaId,
            pagamento.Metodo,
            TraduzirMetodo(pagamento.Metodo),
            pagamento.Valor,
            pagamento.ValorRecebido,
            pagamento.Troco,
            pagamento.Gorjeta,
            pagamento.Referencia,
            pagamento.CriadoEm,
            pagamento.CriadoPor
        );

        // 5. Salvar chave de idempotência se fornecida
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            _context.ChavesIdempotencia.Add(new ChaveIdempotencia
            {
                Chave = idempotencyKey,
                ResultadoJson = JsonSerializer.Serialize(resultadoDto),
                CriadoEm = DateTime.UtcNow,
                ExpiraEm = DateTime.UtcNow.AddHours(24)
            });
            await _context.SaveChangesAsync(ct);
        }

        await _auditoriaService.RegistarAsync(
            acao: "PAGAMENTO.REGISTAR",
            entidade: "pagamentos",
            entidadeId: pagamento.Id.ToString(),
            valoresDepois: new { pagamento.ContaId, pagamento.Metodo, pagamento.Valor, pagamento.Troco, pagamento.CriadoPor },
            motivo: $"Liquidação de {dto.Valor:N2} MT via {TraduzirMetodo(dto.Metodo)}",
            cancellationToken: ct);

        await _notificacaoService.NotificarAlertaCaixaAsync($"Pagamento de {dto.Valor:N2} MT recebido para conta {conta.Numero}", ct);

        return resultadoDto;
    }

    public async Task<List<PagamentoDto>> ObterPagamentosPorContaAsync(Guid contaId, CancellationToken ct = default)
    {
        return await _context.Pagamentos
            .AsNoTracking()
            .Where(p => p.ContaId == contaId)
            .OrderBy(p => p.CriadoEm)
            .Select(p => new PagamentoDto(
                p.Id,
                p.ContaId,
                p.SessaoCaixaId,
                p.Metodo,
                TraduzirMetodo(p.Metodo),
                p.Valor,
                p.ValorRecebido,
                p.Troco,
                p.Gorjeta,
                p.Referencia,
                p.CriadoEm,
                p.CriadoPor
            ))
            .ToListAsync(ct);
    }

    private static string TraduzirMetodo(MetodoPagamento metodo) => metodo switch
    {
        MetodoPagamento.Numerario => "Numerário",
        MetodoPagamento.MulticaixaPOS => "Multicaixa POS",
        MetodoPagamento.MPesaManual => "M-Pesa",
        MetodoPagamento.EMolaManual => "e-Mola",
        MetodoPagamento.Transferencia => "Transferência",
        MetodoPagamento.Cheque => "Cheque",
        _ => "Outro"
    };
}
