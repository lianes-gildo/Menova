using Menova.Application.DTOs;
using Menova.Application.Interfaces;
using Menova.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Menova.Web.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
[IgnoreAntiforgeryToken] // Para requisições de API via token ou sessão móvel PWA
public class ApiController : ControllerBase
{
    private readonly IMenuService _menuService;
    private readonly ISalaService _salaService;

    public ApiController(IMenuService menuService, ISalaService salaService)
    {
        _menuService = menuService;
        _salaService = salaService;
    }

    [HttpGet("menu")]
    public async Task<IActionResult> GetMenu()
    {
        var menu = await _menuService.GetMenuCompletoAsync();
        Response.Headers.ETag = $"\"{menu.Versao}\"";
        return Ok(menu);
    }

    [HttpGet("menu/versao")]
    public async Task<IActionResult> GetMenuVersao()
    {
        var versao = await _menuService.GetMenuVersaoAsync();
        return Ok(new { versao });
    }

    [HttpPost("menu/artigos/{id:guid}/86")]
    [Authorize(Policy = PermissoesCatalogo.ArtigosEsgotar)]
    public async Task<IActionResult> Toggle86(Guid id, [FromBody] Toggle86Request? request)
    {
        try
        {
            var res = await _menuService.ToggleEsgotadoManualAsync(id, request?.Motivo);
            return Ok(res);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { mensagem = "Artigo não encontrado." });
        }
    }

    [HttpGet("mesas")]
    public async Task<IActionResult> GetMesas()
    {
        var zonas = await _salaService.GetZonasComMesasAsync();
        return Ok(zonas);
    }

    [HttpPut("mesas/{id:guid}/estado")]
    public async Task<IActionResult> AtualizarEstadoMesa(Guid id, [FromBody] AtualizarEstadoMesaDto dto)
    {
        if (id != dto.MesaId) return BadRequest(new { mensagem = "ID divergente." });

        try
        {
            await _salaService.AtualizarEstadoMesaAsync(dto.MesaId, dto.NovoEstado, dto.Motivo);
            return Ok(new { sucesso = true, mensagem = "Estado da mesa atualizado." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { mensagem = "Mesa não encontrada." });
        }
    }
}
