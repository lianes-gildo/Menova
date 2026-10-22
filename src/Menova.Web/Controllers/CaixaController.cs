using Menova.Application.DTOs;
using Menova.Application.Interfaces;
using Menova.Application.Security;
using Menova.Domain.Caixa;
using Menova.Domain.Identity;
using Menova.Domain.Impressao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Menova.Web.Controllers;

[Authorize(Policy = PermissoesCatalogo.CaixaAbrir)]
[Route("caixa")]
public class CaixaController : Controller
{
    private readonly ICaixaService _caixaService;
    private readonly IPagamentoService _pagamentoService;
    private readonly IFechoZService _fechoZService;
    private readonly IImpressaoService _impressaoService;
    private readonly IContaService _contaService;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeService _dateTimeService;
    private readonly IAppDbContext _context;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CaixaController> _logger;

    public CaixaController(
        ICaixaService caixaService,
        IPagamentoService pagamentoService,
        IFechoZService fechoZService,
        IImpressaoService impressaoService,
        IContaService contaService,
        ICurrentUserService currentUser,
        IDateTimeService dateTimeService,
        IAppDbContext context,
        IServiceScopeFactory scopeFactory,
        ILogger<CaixaController> logger)
    {
        _caixaService = caixaService;
        _pagamentoService = pagamentoService;
        _fechoZService = fechoZService;
        _impressaoService = impressaoService;
        _contaService = contaService;
        _currentUser = currentUser;
        _dateTimeService = dateTimeService;
        _context = context;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var caixas = await _caixaService.ObterCaixasAsync();
        SessaoCaixaDto? sessaoAtiva = null;

        if (_currentUser.UtilizadorId.HasValue)
        {
            sessaoAtiva = await _caixaService.ObterSessaoAtivaAsync(_currentUser.UtilizadorId.Value);
        }

        ViewBag.Caixas = caixas;
        ViewBag.SessaoAtiva = sessaoAtiva;
        ViewBag.DateTimeService = _dateTimeService;

        return View();
    }

    [HttpPost("abrir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Abrir(AbrirCaixaDto dto)
    {
        if (!_currentUser.UtilizadorId.HasValue)
        {
            TempData["Erro"] = "Sessão de utilizador inválida.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var sessao = await _caixaService.AbrirSessaoAsync(
                dto,
                _currentUser.UtilizadorId.Value,
                _currentUser.NomeUtilizador);

            TempData["Sucesso"] = $"Caixa aberta com sucesso com saldo inicial de {_dateTimeService.FormatarMoeda(sessao.SaldoInicial)}.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao abrir sessão de caixa");
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("fechar")]
    [Authorize(Policy = PermissoesCatalogo.CaixaFechar)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Fechar(FecharCaixaDto dto)
    {
        try
        {
            var sessao = await _caixaService.FecharSessaoAsync(dto, _currentUser.NomeUtilizador);
            TempData["Sucesso"] = $"Sessão de caixa encerrada com sucesso. Saldo contado: {_dateTimeService.FormatarMoeda(sessao.SaldoContado ?? 0)} (Diferença: {_dateTimeService.FormatarMoeda(sessao.Diferenca ?? 0)}).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fechar sessão de caixa");
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("movimento")]
    [Authorize(Policy = PermissoesCatalogo.CaixaMovimentos)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Movimento(MovimentoCaixaEntradaDto dto)
    {
        try
        {
            var mov = await _caixaService.RegistarMovimentoAsync(dto, _currentUser.NomeUtilizador);

            // Imprimir comprovativo em segundo plano com escopo isolado
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var scopedCaixa = scope.ServiceProvider.GetRequiredService<ICaixaService>();
                    var scopedImpressao = scope.ServiceProvider.GetRequiredService<IImpressaoService>();
                    var sessao = await scopedCaixa.ObterSessaoPorIdAsync(dto.SessaoCaixaId);
                    var bytes = scopedImpressao.GerarComprovativoMovimento(mov, sessao?.CaixaNome ?? "Caixa");
                    await scopedImpressao.EnviarParaImpressoraAsync(null, bytes, TipoDocumentoImpressao.MovimentoCaixa);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Aviso ao imprimir comprovativo de movimento em segundo plano");
                }
            });

            TempData["Sucesso"] = $"Movimento de {mov.TipoDescricao} ({_dateTimeService.FormatarMoeda(mov.Valor)}) registado com sucesso.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registar movimento de caixa");
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("pagamento/{contaId:guid}")]
    [Authorize(Policy = PermissoesCatalogo.CaixaCobrar)]
    public async Task<IActionResult> Pagamento(Guid contaId)
    {
        var conta = await _contaService.GetContaPorIdAsync(contaId);
        if (conta == null)
        {
            TempData["Erro"] = "Conta não encontrada.";
            return RedirectToAction("Index", "Pedidos");
        }

        if (!_currentUser.UtilizadorId.HasValue)
        {
            TempData["Erro"] = "Utilizador não autenticado.";
            return RedirectToAction(nameof(Index));
        }

        var sessao = await _caixaService.ObterSessaoAtivaAsync(_currentUser.UtilizadorId.Value);
        if (sessao == null)
        {
            TempData["Erro"] = "Para cobrar pedidos, precisa de ter uma sessão de caixa aberta.";
            return RedirectToAction(nameof(Index));
        }

        var pagamentos = await _pagamentoService.ObterPagamentosPorContaAsync(contaId);
        var totalPago = pagamentos.Sum(p => p.Valor);
        var saldoRestante = Math.Max(0, conta.TotalLiquido - totalPago);

        ViewBag.Conta = conta;
        ViewBag.Sessao = sessao;
        ViewBag.Pagamentos = pagamentos;
        ViewBag.TotalPago = totalPago;
        ViewBag.SaldoRestante = saldoRestante;
        ViewBag.DateTimeService = _dateTimeService;

        return View();
    }

    [HttpPost("pagamento")]
    [Authorize(Policy = PermissoesCatalogo.CaixaCobrar)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessarPagamento(PagamentoEntradaDto dto, bool imprimirRecibo = true)
    {
        try
        {
            var pagamento = await _pagamentoService.RegistarPagamentoAsync(dto, _currentUser.NomeUtilizador);
            var conta = await _contaService.GetContaPorIdAsync(dto.ContaId);
            var pagamentos = await _pagamentoService.ObterPagamentosPorContaAsync(dto.ContaId);
            var totalPago = pagamentos.Sum(p => p.Valor);
            var saldoRestante = conta != null ? Math.Max(0, conta.TotalLiquido - totalPago) : 0;

            if (imprimirRecibo && conta != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var scopedImpressao = scope.ServiceProvider.GetRequiredService<IImpressaoService>();
                        var bytes = scopedImpressao.GerarReciboPagamento(conta, pagamento, saldoRestante);
                        await scopedImpressao.EnviarParaImpressoraAsync(null, bytes, TipoDocumentoImpressao.TalaoConta);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Aviso ao imprimir recibo na impressora de rede em segundo plano");
                    }
                });
            }

            TempData["Sucesso"] = $"Pagamento de {_dateTimeService.FormatarMoeda(pagamento.Valor)} liquidado com sucesso via {pagamento.MetodoDescricao}." +
                (pagamento.Troco > 0 ? $" Troco a entregar: {_dateTimeService.FormatarMoeda(pagamento.Troco)}." : "");

            return RedirectToAction(nameof(Recibo), new { pagamentoId = pagamento.Id, autoPrint = imprimirRecibo });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar pagamento");
            TempData["Erro"] = ex.Message;
            return RedirectToAction(nameof(Pagamento), new { contaId = dto.ContaId });
        }
    }

    [HttpGet("sessoes")]
    public async Task<IActionResult> Sessoes()
    {
        var ehAdminOuGerente = User.IsInRole(PerfisSistema.Administrador) || User.IsInRole(PerfisSistema.Gerente);
        var utilizadorId = _currentUser.UtilizadorId;

        var query = _context.SessoesCaixa
            .Include(s => s.Caixa)
            .Include(s => s.Utilizador)
            .AsNoTracking();

        // Operador apenas visualiza as suas próprias sessões
        if (!ehAdminOuGerente && utilizadorId.HasValue)
        {
            query = query.Where(s => s.UtilizadorId == utilizadorId.Value);
        }

        var sessoes = await query
            .OrderByDescending(s => s.AbertaEm)
            .Take(150)
            .ToListAsync();

        ViewBag.EhAdminOuGerente = ehAdminOuGerente;
        ViewBag.DateTimeService = _dateTimeService;

        return View(sessoes);
    }

    [HttpGet("recibos")]
    public async Task<IActionResult> Recibos()
    {
        var ehAdminOuGerente = User.IsInRole(PerfisSistema.Administrador) || User.IsInRole(PerfisSistema.Gerente);
        var utilizadorNome = _currentUser.NomeUtilizador;
        var utilizadorId = _currentUser.UtilizadorId;

        var query = _context.Pagamentos
            .Include(p => p.Conta).ThenInclude(c => c.Mesa)
            .Include(p => p.SessaoCaixa).ThenInclude(s => s != null ? s.Caixa : null)
            .AsNoTracking();

        // Operador de caixa apenas visualiza os seus recibos emitidos
        if (!ehAdminOuGerente)
        {
            query = query.Where(p => p.CriadoPor == utilizadorNome || (p.SessaoCaixa != null && utilizadorId.HasValue && p.SessaoCaixa.UtilizadorId == utilizadorId.Value));
        }

        var recibos = await query
            .OrderByDescending(p => p.CriadoEm)
            .Take(200)
            .ToListAsync();

        ViewBag.EhAdminOuGerente = ehAdminOuGerente;
        ViewBag.DateTimeService = _dateTimeService;

        return View(recibos);
    }

    [HttpGet("recibo/{pagamentoId:guid}")]
    [Authorize(Policy = PermissoesCatalogo.CaixaCobrar)]
    public async Task<IActionResult> Recibo(Guid pagamentoId, bool autoPrint = false)
    {
        var pagamento = await _pagamentoService.ObterPagamentoPorIdAsync(pagamentoId);
        if (pagamento == null)
        {
            TempData["Erro"] = "Comprovativo de pagamento não encontrado.";
            return RedirectToAction(nameof(Recibos));
        }

        // Validação de Acesso: Operadores só podem ver os seus recibos
        var ehAdminOuGerente = User.IsInRole(PerfisSistema.Administrador) || User.IsInRole(PerfisSistema.Gerente);
        if (!ehAdminOuGerente)
        {
            var emitidoPeloOperador = string.Equals(pagamento.CriadoPor, _currentUser.NomeUtilizador, StringComparison.OrdinalIgnoreCase);
            if (!emitidoPeloOperador && _currentUser.UtilizadorId.HasValue && pagamento.SessaoCaixaId != Guid.Empty)
            {
                var donoDaSessao = await _context.SessoesCaixa
                    .AnyAsync(s => s.Id == pagamento.SessaoCaixaId && s.UtilizadorId == _currentUser.UtilizadorId.Value);
                if (donoDaSessao) emitidoPeloOperador = true;
            }

            if (!emitidoPeloOperador)
            {
                TempData["Erro"] = "Acesso restrito: Os operadores de caixa apenas podem visualizar e reimprimir os recibos emitidos por si próprios.";
                return RedirectToAction(nameof(Recibos));
            }
        }

        var conta = await _contaService.GetContaPorIdAsync(pagamento.ContaId);
        if (conta == null)
        {
            TempData["Erro"] = "Conta associada não encontrada.";
            return RedirectToAction(nameof(Recibos));
        }

        var todosPagamentos = await _pagamentoService.ObterPagamentosPorContaAsync(conta.Id);
        var totalPago = todosPagamentos.Sum(p => p.Valor);
        var saldoRestante = Math.Max(0, conta.TotalLiquido - totalPago);

        ViewBag.Pagamento = pagamento;
        ViewBag.Conta = conta;
        ViewBag.TotalPago = totalPago;
        ViewBag.SaldoRestante = saldoRestante;
        ViewBag.AutoPrint = autoPrint;
        ViewBag.DateTimeService = _dateTimeService;

        return View();
    }

    [HttpGet("x/{sessaoId:guid}")]
    public async Task<IActionResult> RelatorioX(Guid sessaoId)
    {
        try
        {
            // Validação de Acesso: Operadores apenas consultam as suas próprias sessões
            var ehAdminOuGerente = User.IsInRole(PerfisSistema.Administrador) || User.IsInRole(PerfisSistema.Gerente);
            if (!ehAdminOuGerente && _currentUser.UtilizadorId.HasValue)
            {
                var donoDaSessao = await _context.SessoesCaixa.AnyAsync(s => s.Id == sessaoId && s.UtilizadorId == _currentUser.UtilizadorId.Value);
                if (!donoDaSessao)
                {
                    TempData["Erro"] = "Acesso restrito: Os operadores apenas têm permissão para consultar o relatório das sessões abertas por si.";
                    return RedirectToAction(nameof(Sessoes));
                }
            }

            var x = await _caixaService.GerarRelatorioXAsync(sessaoId);
            ViewBag.DateTimeService = _dateTimeService;
            return View(x);
        }
        catch (Exception ex)
        {
            TempData["Erro"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost("x/{sessaoId:guid}/imprimir")]
    [Authorize(Policy = PermissoesCatalogo.CaixaReimprimir)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImprimirRelatorioX(Guid sessaoId)
    {
        try
        {
            var x = await _caixaService.GerarRelatorioXAsync(sessaoId);
            var bytes = _impressaoService.GerarRelatorioX(x);
            var sucesso = await _impressaoService.EnviarParaImpressoraAsync(null, bytes, TipoDocumentoImpressao.RelatorioX);

            if (sucesso)
                TempData["Sucesso"] = "Relatório X enviado para a impressora térmica.";
            else
                TempData["Erro"] = "Impressora térmica inacessível ou offline. O trabalho ficou registado.";
        }
        catch (Exception ex)
        {
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction(nameof(RelatorioX), new { sessaoId });
    }

    [HttpPost("fecho-z/{sessaoId:guid}")]
    [Authorize(Policy = PermissoesCatalogo.CaixaFechar)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EmitirFechoZ(Guid sessaoId)
    {
        try
        {
            var z = await _fechoZService.EmitirFechoZAsync(sessaoId, _currentUser.NomeUtilizador);

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var scopedImpressao = scope.ServiceProvider.GetRequiredService<IImpressaoService>();
                    var bytes = scopedImpressao.GerarTalaoZ(z);
                    await scopedImpressao.EnviarParaImpressoraAsync(null, bytes, TipoDocumentoImpressao.TalaoZ);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Aviso ao imprimir talão Z na impressora em segundo plano");
                }
            });

            TempData["Sucesso"] = $"Talão Z N.º {z.NumeroZ} emitido e assinado criptograficamente com sucesso.";
            return RedirectToAction(nameof(DocumentoZ), new { documentoZId = z.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao emitir fecho Z");
            TempData["Erro"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet("z/{documentoZId:guid}")]
    [Authorize(Policy = PermissoesCatalogo.ZVer)]
    public async Task<IActionResult> DocumentoZ(Guid documentoZId)
    {
        var docZ = await _fechoZService.ObterZPorIdAsync(documentoZId);
        if (docZ == null)
        {
            TempData["Erro"] = "Documento Z não encontrado.";
            return RedirectToAction(nameof(HistoricoZ));
        }

        ViewBag.DateTimeService = _dateTimeService;
        return View(docZ);
    }

    [HttpPost("z/{documentoZId:guid}/reimprimir")]
    [Authorize(Policy = PermissoesCatalogo.CaixaReimprimir)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReimprimirZ(Guid documentoZId)
    {
        var docZ = await _fechoZService.ObterZPorIdAsync(documentoZId);
        if (docZ == null)
        {
            TempData["Erro"] = "Documento Z não encontrado.";
            return RedirectToAction(nameof(HistoricoZ));
        }

        var bytes = _impressaoService.GerarTalaoZ(docZ, eSegundaVia: true);
        var enviado = await _impressaoService.EnviarParaImpressoraAsync(null, bytes, TipoDocumentoImpressao.TalaoZ);

        if (enviado)
            TempData["Sucesso"] = "2ª Via do Talão Z enviada para a impressora.";
        else
            TempData["Erro"] = "Impressora térmica inacessível na rede.";

        return RedirectToAction(nameof(DocumentoZ), new { documentoZId });
    }

    [HttpGet("historico-z")]
    [Authorize(Policy = PermissoesCatalogo.ZVer)]
    public async Task<IActionResult> HistoricoZ()
    {
        var ehAdminOuGerente = User.IsInRole(PerfisSistema.Administrador) || User.IsInRole(PerfisSistema.Gerente);
        var lista = await _fechoZService.ObterHistoricoZAsync();

        if (!ehAdminOuGerente)
        {
            lista = lista.Where(z => string.Equals(z.EmitidoPor, _currentUser.NomeUtilizador, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var cadeiaValida = await _fechoZService.VerificarIntegridadeCadeiaAsync();

        ViewBag.CadeiaValida = cadeiaValida;
        ViewBag.EhAdminOuGerente = ehAdminOuGerente;
        ViewBag.DateTimeService = _dateTimeService;

        return View(lista);
    }
}
