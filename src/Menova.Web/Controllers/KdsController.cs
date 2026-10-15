using Menova.Application.Interfaces;
using Menova.Application.Security;
using Menova.Domain.Pedidos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Menova.Web.Controllers;

[Authorize(Policy = PermissoesCatalogo.CozinhaVer)]
[Route("kds")]
public class KdsController : Controller
{
    private readonly IKdsService _kdsService;
    private readonly IAppDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<KdsController> _logger;

    public KdsController(
        IKdsService kdsService,
        IAppDbContext context,
        IDateTimeService dateTimeService,
        ILogger<KdsController> logger)
    {
        _kdsService = kdsService;
        _context = context;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(Guid? estacaoId = null)
    {
        var estacoes = _context.EstacoesPreparacao.Where(e => e.Ativa).OrderBy(e => e.Nome).ToList();
        var tickets = await _kdsService.GetTicketsAtivosAsync(estacaoId);

        ViewBag.Estacoes = estacoes;
        ViewBag.EstacaoSelecionada = estacaoId;
        ViewBag.DateTimeService = _dateTimeService;

        return View(tickets);
    }

    [HttpGet("api/tickets")]
    public async Task<IActionResult> GetTicketsJson(Guid? estacaoId = null)
    {
        var tickets = await _kdsService.GetTicketsAtivosAsync(estacaoId);
        return Json(tickets);
    }

    [HttpPost("itens/{id:guid}/bump")]
    [Authorize(Policy = PermissoesCatalogo.CozinhaOperar)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BumpItem(Guid id, [FromBody] BumpItemRequest request)
    {
        try
        {
            await _kdsService.AtualizarEstadoItemAsync(id, request.NovoEstado);
            return Json(new { sucesso = true, mensagem = "Estado atualizado." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no bump do item KDS {Id}", id);
            return StatusCode(500, new { sucesso = false, mensagem = "Erro ao atualizar estado." });
        }
    }

    [HttpPost("tickets/{id:guid}/bump")]
    [Authorize(Policy = PermissoesCatalogo.CozinhaOperar)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BumpTicket(Guid id)
    {
        try
        {
            await _kdsService.AvancarEstadoTicketAsync(id);
            return Json(new { sucesso = true, mensagem = "Ticket avançado." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no bump do ticket KDS {Id}", id);
            return StatusCode(500, new { sucesso = false, mensagem = "Erro ao avançar ticket." });
        }
    }
}

public record BumpItemRequest(EstadoItemPedido NovoEstado);
