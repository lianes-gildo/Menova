using Menova.Application.DTOs;
using Menova.Application.Interfaces;
using Menova.Application.Security;
using Menova.Domain.Caixa;
using Menova.Domain.Impressao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    private readonly ILogger<CaixaController> _logger;

    public CaixaController(
        ICaixaService caixaService,
        IPagamentoService pagamentoService,
        IFechoZService fechoZService,
        IImpressaoService impressaoService,
        IContaService contaService,
        ICurrentUserService currentUser,
        IDateTimeService dateTimeService,
        ILogger<CaixaController> logger)
    {
        _caixaService = caixaService;
        _pagamentoService = pagamentoService;
        _fechoZService = fechoZService;
        _impressaoService = impressaoService;
        _contaService = contaService;
        _currentUser = currentUser;
        _dateTimeService = dateTimeService;
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

            // Imprimir comprovativo em segundo plano
            _ = Task.Run(async () =>
            {
                try
                {
                    var sessao = await _caixaService.ObterSessaoPorIdAsync(dto.SessaoCaixaId);
                    var bytes = _impressaoService.GerarComprovativoMovimento(mov, sessao?.CaixaNome ?? "Caixa");
                    await _impressaoService.EnviarParaImpressoraAsync(null, bytes, TipoDocumentoImpressao.MovimentoCaixa);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao imprimir comprovativo de movimento");
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
                        var bytes = _impressaoService.GerarReciboPagamento(conta, pagamento, saldoRestante);
                        await _impressaoService.EnviarParaImpressoraAsync(null, bytes, TipoDocumentoImpressao.TalaoConta);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao imprimir recibo");
                    }
                });
            }

            TempData["Sucesso"] = $"Pagamento de {_dateTimeService.FormatarMoeda(pagamento.Valor)} liquidado com sucesso via {pagamento.MetodoDescricao}." +
                (pagamento.Troco > 0 ? $" Troco a entregar: {_dateTimeService.FormatarMoeda(pagamento.Troco)}." : "");

            if (saldoRestante <= 0)
            {
                return RedirectToAction("Index", "Pedidos");
            }

            return RedirectToAction(nameof(Pagamento), new { contaId = dto.ContaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar pagamento");
            TempData["Erro"] = ex.Message;
            return RedirectToAction(nameof(Pagamento), new { contaId = dto.ContaId });
        }
    }

    [HttpGet("x/{sessaoId:guid}")]
    public async Task<IActionResult> RelatorioX(Guid sessaoId)
    {
        try
        {
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
                    var bytes = _impressaoService.GerarTalaoZ(z);
                    await _impressaoService.EnviarParaImpressoraAsync(null, bytes, TipoDocumentoImpressao.TalaoZ);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao imprimir talão Z");
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
        var lista = await _fechoZService.ObterHistoricoZAsync();
        var cadeiaValida = await _fechoZService.VerificarIntegridadeCadeiaAsync();

        ViewBag.CadeiaValida = cadeiaValida;
        ViewBag.DateTimeService = _dateTimeService;

        return View(lista);
    }
}
