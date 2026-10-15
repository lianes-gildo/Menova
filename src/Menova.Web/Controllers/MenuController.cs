using Menova.Application.DTOs;
using Menova.Application.Interfaces;
using Menova.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Menova.Web.Controllers;

[Authorize(Policy = PermissoesCatalogo.MenuVer)]
[Route("menu")]
public class MenuController : Controller
{
    private readonly IMenuService _menuService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<MenuController> _logger;

    public MenuController(
        IMenuService menuService,
        IDateTimeService dateTimeService,
        ILogger<MenuController> logger)
    {
        _menuService = menuService;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(Guid? categoriaId = null, string? pesquisa = null)
    {
        var categorias = await _menuService.GetCategoriasAsync();
        var artigos = await _menuService.GetArtigosPorCategoriaAsync(categoriaId, pesquisa);

        ViewBag.Categorias = categorias;
        ViewBag.CategoriaSelecionada = categoriaId;
        ViewBag.Pesquisa = pesquisa;
        ViewBag.DateTimeService = _dateTimeService;

        return View(artigos);
    }

    [HttpPost("artigos/{id:guid}/toggle-86")]
    [Authorize(Policy = PermissoesCatalogo.ArtigosEsgotar)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle86(Guid id, [FromBody] Toggle86Request? request)
    {
        try
        {
            var res = await _menuService.ToggleEsgotadoManualAsync(id, request?.Motivo);
            return Json(new
            {
                sucesso = true,
                artigoId = res.ArtigoId,
                nome = res.Nome,
                esgotadoManual = res.EsgotadoManual,
                menuVersao = res.MenuVersao,
                mensagem = res.EsgotadoManual
                    ? $"Artigo '{res.Nome}' foi marcado como ESGOTADO (86)."
                    : $"Artigo '{res.Nome}' está agora DISPONÍVEL."
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { sucesso = false, mensagem = "Artigo não encontrado." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao alternar esgotado 86 para o artigo {Id}", id);
            return StatusCode(500, new { sucesso = false, mensagem = "Erro ao alternar disponibilidade." });
        }
    }

    [HttpGet("artigos/criar")]
    [Authorize(Policy = PermissoesCatalogo.MenuGerir)]
    public async Task<IActionResult> CriarArtigo()
    {
        ViewBag.Categorias = await _menuService.GetCategoriasAsync();
        return View();
    }

    [HttpPost("artigos/criar")]
    [Authorize(Policy = PermissoesCatalogo.MenuGerir)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CriarArtigo(CriarArtigoDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categorias = await _menuService.GetCategoriasAsync();
            return View(dto);
        }

        try
        {
            await _menuService.CriarArtigoAsync(dto);
            TempData["Sucesso"] = "Artigo criado com sucesso.";
            return RedirectToAction(nameof(Index), new { categoriaId = dto.CategoriaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar artigo.");
            ModelState.AddModelError("", "Ocorreu um erro ao gravar o artigo. Verifique se o código interno já existe.");
            ViewBag.Categorias = await _menuService.GetCategoriasAsync();
            return View(dto);
        }
    }

    [HttpGet("artigos/editar/{id:guid}")]
    [Authorize(Policy = PermissoesCatalogo.MenuGerir)]
    public async Task<IActionResult> EditarArtigo(Guid id)
    {
        var artigo = await _menuService.GetArtigoDetalheAsync(id);
        if (artigo == null) return NotFound();

        ViewBag.Categorias = await _menuService.GetCategoriasAsync();
        var dto = new AtualizarArtigoDto(
            artigo.Id,
            artigo.CategoriaId,
            artigo.CodigoInterno,
            artigo.Nome,
            artigo.DescricaoCurta,
            artigo.DescricaoLonga,
            artigo.Preco,
            artigo.TaxaIva,
            artigo.EstacaoId,
            artigo.TempoPreparacaoMin,
            artigo.Destaque,
            artigo.Ativo
        );

        return View(dto);
    }

    [HttpPost("artigos/editar/{id:guid}")]
    [Authorize(Policy = PermissoesCatalogo.MenuGerir)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarArtigo(Guid id, AtualizarArtigoDto dto)
    {
        if (id != dto.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            ViewBag.Categorias = await _menuService.GetCategoriasAsync();
            return View(dto);
        }

        try
        {
            await _menuService.AtualizarArtigoAsync(dto);
            TempData["Sucesso"] = "Artigo atualizado com sucesso.";
            return RedirectToAction(nameof(Index), new { categoriaId = dto.CategoriaId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar artigo {Id}", id);
            ModelState.AddModelError("", "Erro ao atualizar o artigo.");
            ViewBag.Categorias = await _menuService.GetCategoriasAsync();
            return View(dto);
        }
    }

    [HttpPost("artigos/eliminar/{id:guid}")]
    [Authorize(Policy = PermissoesCatalogo.MenuGerir)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarArtigo(Guid id)
    {
        try
        {
            await _menuService.EliminarArtigoAsync(id);
            return Json(new { sucesso = true, mensagem = "Artigo eliminado com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao eliminar artigo {Id}", id);
            return StatusCode(500, new { sucesso = false, mensagem = "Não foi possível eliminar o artigo." });
        }
    }
}
