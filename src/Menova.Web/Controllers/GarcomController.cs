using Menova.Application.DTOs;
using Menova.Application.Interfaces;
using Menova.Application.Security;
using Menova.Domain.Pedidos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Menova.Web.Controllers;

[Authorize(Policy = PermissoesCatalogo.ContasAbrir)]
[Route("garcom")]
public class GarcomController : Controller
{
    private readonly ISalaService _salaService;
    private readonly IMenuService _menuService;
    private readonly IKdsService _kdsService;
    private readonly IAppDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<GarcomController> _logger;

    public GarcomController(
        ISalaService salaService,
        IMenuService menuService,
        IKdsService kdsService,
        IAppDbContext context,
        IDateTimeService dateTimeService,
        ILogger<GarcomController> logger)
    {
        _salaService = salaService;
        _menuService = menuService;
        _kdsService = kdsService;
        _context = context;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var zonas = await _salaService.GetZonasComMesasAsync();
        var menu = await _menuService.GetMenuCompletoAsync();

        ViewBag.Zonas = zonas;
        ViewBag.Menu = menu;
        ViewBag.DateTimeService = _dateTimeService;

        return View();
    }

    [HttpGet("pedidos-cozinha")]
    public async Task<IActionResult> ObterPedidosCozinha()
    {
        try
        {
            var tickets = await _kdsService.GetTicketsAtivosAsync();
            return Json(tickets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter pedidos da cozinha para o garçom");
            return StatusCode(500, new { sucesso = false, mensagem = "Erro ao carregar estado da cozinha." });
        }
    }

    [HttpPost("entregar-item/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EntregarItem(Guid id)
    {
        try
        {
            await _kdsService.AtualizarEstadoItemAsync(id, EstadoItemPedido.Entregue);
            return Json(new { sucesso = true, mensagem = "Artigo entregue à mesa com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao marcar item {Id} como entregue", id);
            return StatusCode(500, new { sucesso = false, mensagem = "Erro ao marcar artigo como entregue." });
        }
    }

    [HttpPost("entregar-ticket/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EntregarTicket(Guid id)
    {
        try
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
            {
                return NotFound(new { sucesso = false, mensagem = "Ticket não encontrado." });
            }

            foreach (var item in pedido.Itens.Where(i => !i.Anulado && i.Estado != EstadoItemPedido.Entregue))
            {
                item.Estado = EstadoItemPedido.Entregue;
            }
            pedido.Estado = EstadoPedido.Entregue;
            await _context.SaveChangesAsync();

            return Json(new { sucesso = true, mensagem = "Todos os artigos do ticket foram entregues à mesa." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao entregar ticket completo {Id}", id);
            return StatusCode(500, new { sucesso = false, mensagem = "Erro ao entregar artigos do ticket." });
        }
    }
}
