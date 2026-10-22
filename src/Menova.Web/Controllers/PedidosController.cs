using Menova.Application.DTOs;
using Menova.Application.Interfaces;
using Menova.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Menova.Web.Controllers;

[Authorize(Policy = PermissoesCatalogo.ContasAbrir)]
[Route("pedidos")]
public class PedidosController : Controller
{
    private readonly IContaService _contaService;
    private readonly IPedidoService _pedidoService;
    private readonly ISalaService _salaService;
    private readonly IMenuService _menuService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<PedidosController> _logger;

    public PedidosController(
        IContaService contaService,
        IPedidoService pedidoService,
        ISalaService salaService,
        IMenuService menuService,
        IDateTimeService dateTimeService,
        ILogger<PedidosController> logger)
    {
        _contaService = contaService;
        _pedidoService = pedidoService;
        _salaService = salaService;
        _menuService = menuService;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var contasAbertas = await _contaService.ListarContasAbertasAsync();
        ViewBag.DateTimeService = _dateTimeService;
        return View(contasAbertas);
    }

    [HttpGet("api/ativos")]
    public async Task<IActionResult> ObterContasAtivasJson()
    {
        try
        {
            var contas = await _contaService.ListarContasAbertasAsync();
            var totalValor = contas.Sum(c => c.TotalLiquido);
            return Json(new
            {
                sucesso = true,
                totalContas = contas.Count,
                totalValorAberto = totalValor,
                totalValorFormatado = totalValor.ToString("N2") + " MT",
                contas = contas.Select(c => new
                {
                    id = c.Id,
                    numero = c.Numero,
                    mesaNumero = c.MesaNumero,
                    zonaNome = c.ZonaNome,
                    criadoEm = c.CriadoEm.ToString("dd/MM/yyyy HH:mm"),
                    criadoPor = c.CriadoPor,
                    totalPedidos = c.Pedidos.Count,
                    totalLiquido = c.TotalLiquido,
                    totalLiquidoFormatado = c.TotalLiquido.ToString("N2") + " MT"
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter contas ativas via API");
            return StatusCode(500, new { sucesso = false, mensagem = "Erro ao carregar comandas ativas." });
        }
    }

    [HttpGet("nova-comanda")]
    public async Task<IActionResult> NovaComanda(Guid? mesaId = null)
    {
        var zonas = await _salaService.GetZonasComMesasAsync();
        var menu = await _menuService.GetMenuCompletoAsync();

        ViewBag.Zonas = zonas;
        ViewBag.Menu = menu;
        ViewBag.MesaSelecionadaId = mesaId;
        ViewBag.DateTimeService = _dateTimeService;

        return View();
    }

    [HttpPost("criar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar([FromBody] CriarPedidoDto dto)
    {
        if (dto == null || dto.Itens == null || !dto.Itens.Any())
        {
            return BadRequest(new { sucesso = false, mensagem = "O pedido não contém artigos." });
        }

        try
        {
            var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
            var pedido = await _pedidoService.CriarPedidoAsync(dto, idempotencyKey);
            return Json(new { sucesso = true, pedidoId = pedido.Id, contaId = pedido.ContaId, mensagem = "Comanda enviada com sucesso para a preparação!" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { sucesso = false, mensagem = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar pedido.");
            return StatusCode(500, new { sucesso = false, mensagem = "Erro ao enviar comanda para o sistema." });
        }
    }

    [HttpGet("detalhes/{contaId:guid}")]
    public async Task<IActionResult> Detalhes(Guid contaId)
    {
        var conta = await _contaService.GetContaPorIdAsync(contaId);
        if (conta == null) return NotFound();

        ViewBag.DateTimeService = _dateTimeService;
        return View(conta);
    }

    [HttpPost("anular-item")]
    [Authorize(Policy = PermissoesCatalogo.ItensAnular)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AnularItem([FromBody] AnularItemDto dto)
    {
        if (dto == null) return BadRequest();

        try
        {
            await _pedidoService.AnularItemPedidoAsync(dto);
            return Json(new { sucesso = true, mensagem = "Item anulado com sucesso." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { sucesso = false, mensagem = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao anular item {ItemId}", dto.ItemId);
            return StatusCode(500, new { sucesso = false, mensagem = ex.Message });
        }
    }

    [HttpPost("anular-conta/{contaId:guid}")]
    [Authorize(Policy = PermissoesCatalogo.ItensAnular)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AnularConta(Guid contaId, [FromForm] string motivo)
    {
        try
        {
            await _contaService.AnularContaAsync(contaId, motivo);
            TempData["Sucesso"] = "Conta anulada com sucesso.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao anular conta {ContaId}", contaId);
            TempData["Erro"] = "Não foi possível anular a conta.";
            return RedirectToAction(nameof(Detalhes), new { contaId });
        }
    }
}
