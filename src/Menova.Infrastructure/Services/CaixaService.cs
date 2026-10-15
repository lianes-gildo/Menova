using System.Text.Json;
using Menova.Application.DTOs;
using Menova.Application.Interfaces;
using Menova.Domain.Caixa;
using Microsoft.EntityFrameworkCore;

namespace Menova.Infrastructure.Services;

public class CaixaService : ICaixaService
{
    private readonly IAppDbContext _context;
    private readonly IAuditoriaService _auditoriaService;
    private readonly IPinService _pinService;

    public CaixaService(IAppDbContext context, IAuditoriaService auditoriaService, IPinService pinService)
    {
        _context = context;
        _auditoriaService = auditoriaService;
        _pinService = pinService;
    }

    public async Task<List<CaixaDto>> ObterCaixasAsync(CancellationToken ct = default)
    {
        var caixas = await _context.Caixas
            .AsNoTracking()
            .Where(c => c.Ativa)
            .OrderBy(c => c.Numero)
            .ToListAsync(ct);

        var sessoesAbertas = await _context.SessoesCaixa
            .AsNoTracking()
            .Include(s => s.Utilizador)
            .Where(s => s.Estado == EstadoSessaoCaixa.Aberta)
            .ToListAsync(ct);

        return caixas.Select(c =>
        {
            var sessao = sessoesAbertas.FirstOrDefault(s => s.CaixaId == c.Id);
            return new CaixaDto(
                c.Id,
                c.Nome,
                c.Numero,
                c.Ativa,
                sessao != null,
                sessao?.Id,
                sessao?.Utilizador?.NomeCompleto ?? sessao?.Utilizador?.NomeUtilizador
            );
        }).ToList();
    }

    public async Task<SessaoCaixaDto?> ObterSessaoAtivaAsync(Guid utilizadorId, CancellationToken ct = default)
    {
        var sessao = await _context.SessoesCaixa
            .AsNoTracking()
            .Include(s => s.Caixa)
            .Include(s => s.Utilizador)
            .Include(s => s.Movimentos)
            .Include(s => s.Pagamentos)
            .Where(s => s.Estado == EstadoSessaoCaixa.Aberta && s.UtilizadorId == utilizadorId)
            .FirstOrDefaultAsync(ct);

        if (sessao == null) return null;

        return MapearSessao(sessao);
    }

    public async Task<SessaoCaixaDto?> ObterSessaoPorIdAsync(Guid sessaoId, CancellationToken ct = default)
    {
        var sessao = await _context.SessoesCaixa
            .AsNoTracking()
            .Include(s => s.Caixa)
            .Include(s => s.Utilizador)
            .Include(s => s.Movimentos)
            .Include(s => s.Pagamentos)
            .FirstOrDefaultAsync(s => s.Id == sessaoId, ct);

        if (sessao == null) return null;

        return MapearSessao(sessao);
    }

    public async Task<SessaoCaixaDto> AbrirSessaoAsync(AbrirCaixaDto dto, Guid utilizadorId, string utilizadorNome, CancellationToken ct = default)
    {
        var caixa = await _context.Caixas.FindAsync(new object[] { dto.CaixaId }, ct);
        if (caixa == null || !caixa.Ativa)
        {
            throw new InvalidOperationException("Caixa não encontrada ou inativa.");
        }

        var temSessaoAbertaCaixa = await _context.SessoesCaixa
            .AnyAsync(s => s.CaixaId == dto.CaixaId && s.Estado == EstadoSessaoCaixa.Aberta, ct);

        if (temSessaoAbertaCaixa)
        {
            throw new InvalidOperationException($"A caixa '{caixa.Nome}' já possui uma sessão aberta.");
        }

        var utilizadorTemSessao = await _context.SessoesCaixa
            .AnyAsync(s => s.UtilizadorId == utilizadorId && s.Estado == EstadoSessaoCaixa.Aberta, ct);

        if (utilizadorTemSessao)
        {
            throw new InvalidOperationException("O operador atual já tem uma sessão de caixa aberta.");
        }

        var sessao = new SessaoCaixa
        {
            CaixaId = dto.CaixaId,
            UtilizadorId = utilizadorId,
            AbertaEm = DateTime.UtcNow,
            SaldoInicial = dto.SaldoInicial,
            SaldoEsperado = dto.SaldoInicial,
            TotalVendas = 0.00m,
            TotalEntradas = 0.00m,
            TotalSaidas = 0.00m,
            Estado = EstadoSessaoCaixa.Aberta,
            Observacoes = dto.Observacoes
        };

        _context.SessoesCaixa.Add(sessao);
        await _context.SaveChangesAsync(ct);

        await _auditoriaService.RegistarAsync(
            acao: "CAIXA.ABRIR",
            entidade: "sessoes_caixa",
            entidadeId: sessao.Id.ToString(),
            valoresDepois: new { sessao.CaixaId, sessao.SaldoInicial, sessao.AbertaEm },
            motivo: $"Abertura de sessão pelo operador {utilizadorNome}",
            cancellationToken: ct);

        return (await ObterSessaoPorIdAsync(sessao.Id, ct))!;
    }

    public async Task<SessaoCaixaDto> FecharSessaoAsync(FecharCaixaDto dto, string utilizadorNome, CancellationToken ct = default)
    {
        var sessao = await _context.SessoesCaixa
            .Include(s => s.Caixa)
            .Include(s => s.Movimentos)
            .Include(s => s.Pagamentos)
            .FirstOrDefaultAsync(s => s.Id == dto.SessaoCaixaId, ct);

        if (sessao == null)
        {
            throw new InvalidOperationException("Sessão de caixa não encontrada.");
        }

        if (sessao.Estado != EstadoSessaoCaixa.Aberta)
        {
            throw new InvalidOperationException("Esta sessão de caixa já se encontra fechada.");
        }

        var vendasTotais = sessao.Pagamentos.Sum(p => p.Valor);
        var vendasNumerario = sessao.Pagamentos.Where(p => p.Metodo == MetodoPagamento.Numerario).Sum(p => p.Valor);
        var entradas = sessao.Movimentos.Where(m => m.Tipo == TipoMovimentoCaixa.Entrada || m.Tipo == TipoMovimentoCaixa.Suprimento).Sum(m => m.Valor);
        var saidas = sessao.Movimentos.Where(m => m.Tipo == TipoMovimentoCaixa.Saida || m.Tipo == TipoMovimentoCaixa.Sangria).Sum(m => m.Valor);

        var saldoEsperadoGaveta = sessao.SaldoInicial + vendasNumerario + entradas - saidas;

        sessao.TotalVendas = vendasTotais;
        sessao.TotalEntradas = entradas;
        sessao.TotalSaidas = saidas;
        sessao.SaldoEsperado = saldoEsperadoGaveta;
        sessao.SaldoContado = dto.SaldoContado;
        sessao.Diferenca = dto.SaldoContado - saldoEsperadoGaveta;
        sessao.Estado = EstadoSessaoCaixa.Fechada;
        sessao.FechadaEm = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(dto.Observacoes))
        {
            sessao.Observacoes = string.IsNullOrWhiteSpace(sessao.Observacoes)
                ? dto.Observacoes
                : $"{sessao.Observacoes} | {dto.Observacoes}";
        }

        await _context.SaveChangesAsync(ct);

        await _auditoriaService.RegistarAsync(
            acao: "CAIXA.FECHAR",
            entidade: "sessoes_caixa",
            entidadeId: sessao.Id.ToString(),
            valoresDepois: new { sessao.SaldoEsperado, sessao.SaldoContado, sessao.Diferenca, sessao.FechadaEm },
            motivo: $"Fecho de sessão pelo operador {utilizadorNome}. Diferença: {sessao.Diferenca:N2} MT",
            cancellationToken: ct);

        return (await ObterSessaoPorIdAsync(sessao.Id, ct))!;
    }

    public async Task<MovimentoCaixaDto> RegistarMovimentoAsync(MovimentoCaixaEntradaDto dto, string utilizadorNome, CancellationToken ct = default)
    {
        var sessao = await _context.SessoesCaixa
            .FirstOrDefaultAsync(s => s.Id == dto.SessaoCaixaId && s.Estado == EstadoSessaoCaixa.Aberta, ct);

        if (sessao == null)
        {
            throw new InvalidOperationException("Sessão de caixa não encontrada ou já encerrada.");
        }

        if (dto.Valor <= 0)
        {
            throw new InvalidOperationException("O valor do movimento deve ser superior a zero.");
        }

        if (dto.Tipo == TipoMovimentoCaixa.Sangria)
        {
            if (string.IsNullOrWhiteSpace(dto.PinSupervisor))
            {
                throw new InvalidOperationException("É obrigatório o PIN de supervisor para autorizar a sangria de caixa.");
            }

            var pinValido = await _pinService.ValidarPinSupervisorAsync(dto.PinSupervisor, ct);
            if (!pinValido)
            {
                throw new InvalidOperationException("PIN de supervisor incorreto ou utilizador sem privilégios de supervisão.");
            }
        }

        var movimento = new MovimentoCaixa
        {
            SessaoCaixaId = dto.SessaoCaixaId,
            Tipo = dto.Tipo,
            Valor = dto.Valor,
            Motivo = dto.Motivo,
            CriadoEm = DateTime.UtcNow,
            CriadoPor = utilizadorNome
        };

        _context.MovimentosCaixa.Add(movimento);

        if (dto.Tipo == TipoMovimentoCaixa.Entrada || dto.Tipo == TipoMovimentoCaixa.Suprimento)
        {
            sessao.TotalEntradas += dto.Valor;
            sessao.SaldoEsperado += dto.Valor;
        }
        else
        {
            sessao.TotalSaidas += dto.Valor;
            sessao.SaldoEsperado -= dto.Valor;
        }

        await _context.SaveChangesAsync(ct);

        await _auditoriaService.RegistarAsync(
            acao: $"CAIXA.{dto.Tipo.ToString().ToUpperInvariant()}",
            entidade: "movimentos_caixa",
            entidadeId: movimento.Id.ToString(),
            valoresDepois: new { movimento.Tipo, movimento.Valor, movimento.Motivo, movimento.CriadoPor },
            motivo: $"Movimento de {dto.Tipo} no valor de {dto.Valor:N2} MT",
            cancellationToken: ct);

        return new MovimentoCaixaDto(
            movimento.Id,
            movimento.SessaoCaixaId,
            movimento.Tipo,
            TraduzirTipoMovimento(movimento.Tipo),
            movimento.Valor,
            movimento.Motivo,
            movimento.CriadoEm,
            movimento.CriadoPor
        );
    }

    public async Task<RelatorioXDto> GerarRelatorioXAsync(Guid sessaoId, CancellationToken ct = default)
    {
        var sessao = await _context.SessoesCaixa
            .AsNoTracking()
            .Include(s => s.Caixa)
            .Include(s => s.Utilizador)
            .Include(s => s.Movimentos)
            .Include(s => s.Pagamentos).ThenInclude(p => p.Conta)
            .FirstOrDefaultAsync(s => s.Id == sessaoId, ct);

        if (sessao == null)
        {
            throw new InvalidOperationException("Sessão de caixa não encontrada.");
        }

        var totalVendas = sessao.Pagamentos.Sum(p => p.Valor);
        var vendasNumerario = sessao.Pagamentos.Where(p => p.Metodo == MetodoPagamento.Numerario).Sum(p => p.Valor);
        var totalEntradas = sessao.Movimentos.Where(m => m.Tipo == TipoMovimentoCaixa.Entrada || m.Tipo == TipoMovimentoCaixa.Suprimento).Sum(m => m.Valor);
        var totalSaidas = sessao.Movimentos.Where(m => m.Tipo == TipoMovimentoCaixa.Saida || m.Tipo == TipoMovimentoCaixa.Sangria).Sum(m => m.Valor);
        var saldoEsperado = sessao.SaldoInicial + vendasNumerario + totalEntradas - totalSaidas;

        var vendasPorMetodo = sessao.Pagamentos
            .GroupBy(p => p.Metodo)
            .ToDictionary(g => TraduzirMetodo(g.Key), g => g.Sum(p => p.Valor));

        var contasPagasIds = sessao.Pagamentos.Select(p => p.ContaId).Distinct().ToList();
        var totalIva = await _context.Contas
            .AsNoTracking()
            .Where(c => contasPagasIds.Contains(c.Id))
            .SumAsync(c => c.TotalIva, ct);

        var totaisIva = new Dictionary<string, decimal>
        {
            { "16% (Normal)", totalIva }
        };

        return new RelatorioXDto(
            sessao.Id,
            sessao.Caixa.Nome,
            sessao.Caixa.Numero,
            sessao.Utilizador.NomeCompleto ?? sessao.Utilizador.NomeUtilizador,
            sessao.AbertaEm,
            DateTime.UtcNow,
            sessao.SaldoInicial,
            totalVendas,
            totalEntradas,
            totalSaidas,
            saldoEsperado,
            contasPagasIds.Count,
            vendasPorMetodo,
            totaisIva
        );
    }

    private static SessaoCaixaDto MapearSessao(SessaoCaixa s)
    {
        return new SessaoCaixaDto(
            s.Id,
            s.CaixaId,
            s.Caixa?.Nome ?? "Caixa",
            s.Caixa?.Numero ?? 1,
            s.UtilizadorId,
            s.Utilizador?.NomeCompleto ?? s.Utilizador?.NomeUtilizador ?? "Operador",
            s.AbertaEm,
            s.FechadaEm,
            s.SaldoInicial,
            s.TotalVendas,
            s.TotalEntradas,
            s.TotalSaidas,
            s.SaldoEsperado,
            s.SaldoContado,
            s.Diferenca,
            s.Estado,
            s.Estado == EstadoSessaoCaixa.Aberta ? "Aberta" : "Fechada",
            s.Observacoes,
            s.Movimentos.OrderByDescending(m => m.CriadoEm).Select(m => new MovimentoCaixaDto(
                m.Id,
                m.SessaoCaixaId,
                m.Tipo,
                TraduzirTipoMovimento(m.Tipo),
                m.Valor,
                m.Motivo,
                m.CriadoEm,
                m.CriadoPor
            )).ToList(),
            s.Pagamentos.OrderByDescending(p => p.CriadoEm).Select(p => new PagamentoDto(
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
            )).ToList()
        );
    }

    private static string TraduzirTipoMovimento(TipoMovimentoCaixa tipo) => tipo switch
    {
        TipoMovimentoCaixa.Entrada => "Entrada",
        TipoMovimentoCaixa.Saida => "Saída",
        TipoMovimentoCaixa.Sangria => "Sangria",
        TipoMovimentoCaixa.Suprimento => "Suprimento",
        _ => tipo.ToString()
    };

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
