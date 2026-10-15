using Menova.Application.DTOs;
using Menova.Application.Interfaces;
using Menova.Application.Security;
using Menova.Domain.Sala;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Menova.Web.Controllers;

[Authorize(Policy = PermissoesCatalogo.MesasVer)]
[Route("mesas")]
public class MesasController : Controller
{
    private readonly ISalaService _salaService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<MesasController> _logger;

    public MesasController(
        ISalaService salaService,
        IDateTimeService dateTimeService,
        ILogger<MesasController> logger)
    {
        _salaService = salaService;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var zonas = await _salaService.GetZonasComMesasAsync();
        ViewBag.DateTimeService = _dateTimeService;
        return View(zonas);
    }

    [HttpPost("alterar-estado")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarEstado([FromBody] AtualizarEstadoMesaDto dto)
    {
        if (dto == null) return BadRequest(new { sucesso = false, mensagem = "Dados inválidos." });

        try
        {
            await _salaService.AtualizarEstadoMesaAsync(dto.MesaId, dto.NovoEstado, dto.Motivo);
            return Json(new { sucesso = true, mensagem = "Estado da mesa atualizado com sucesso." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { sucesso = false, mensagem = "Mesa não encontrada." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao alterar estado da mesa {MesaId}", dto.MesaId);
            return StatusCode(500, new { sucesso = false, mensagem = "Erro ao processar alteração de mesa." });
        }
    }

    [HttpGet("detalhes/{id:guid}")]
    public async Task<IActionResult> Detalhes(Guid id)
    {
        var mesa = await _salaService.GetMesaPorIdAsync(id);
        if (mesa == null) return NotFound();
        return Json(mesa);
    }
}
