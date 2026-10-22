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

    [HttpPost("criar-mesa")]
    [Authorize(Policy = PermissoesCatalogo.MesasGerir)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarMesa([FromForm] CriarMesaDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Numero))
        {
            TempData["Erro"] = "O número ou identificador da mesa é obrigatório.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _salaService.CriarMesaAsync(dto);
            TempData["Sucesso"] = $"Mesa {dto.Numero} criada com sucesso com capacidade para {dto.Lugares} pessoas.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar mesa");
            TempData["Erro"] = $"Erro ao criar mesa: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("editar-mesa")]
    [Authorize(Policy = PermissoesCatalogo.MesasGerir)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarMesa([FromForm] AtualizarMesaDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Numero))
        {
            TempData["Erro"] = "O número da mesa é obrigatório.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _salaService.AtualizarMesaAsync(dto);
            TempData["Sucesso"] = $"Mesa {dto.Numero} atualizada com sucesso.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar mesa");
            TempData["Erro"] = $"Erro ao atualizar mesa: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("eliminar-mesa/{id:guid}")]
    [Authorize(Policy = PermissoesCatalogo.MesasGerir)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarMesa(Guid id)
    {
        try
        {
            await _salaService.EliminarMesaAsync(id);
            TempData["Sucesso"] = "Mesa desativada com sucesso.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao eliminar mesa");
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("criar-zona")]
    [Authorize(Policy = PermissoesCatalogo.MesasGerir)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarZona([FromForm] CriarZonaDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Nome))
        {
            TempData["Erro"] = "O nome do salão/zona é obrigatório.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _salaService.CriarZonaAsync(dto);
            TempData["Sucesso"] = $"Salão/Zona '{dto.Nome}' criado com sucesso.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar zona");
            TempData["Erro"] = $"Erro ao criar salão: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("editar-zona")]
    [Authorize(Policy = PermissoesCatalogo.MesasGerir)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarZona([FromForm] AtualizarZonaDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Nome))
        {
            TempData["Erro"] = "O nome do salão é obrigatório.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _salaService.AtualizarZonaAsync(dto);
            TempData["Sucesso"] = $"Salão '{dto.Nome}' atualizado com sucesso.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao editar zona");
            TempData["Erro"] = $"Erro ao editar salão: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("eliminar-zona/{id:guid}")]
    [Authorize(Policy = PermissoesCatalogo.MesasGerir)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarZona(Guid id)
    {
        try
        {
            await _salaService.EliminarZonaAsync(id);
            TempData["Sucesso"] = "Salão desativado com sucesso.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao eliminar salão");
            TempData["Erro"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
