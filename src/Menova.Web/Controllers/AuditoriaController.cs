using Menova.Application.Interfaces;
using Menova.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Menova.Web.Controllers;

[Authorize(Policy = PermissoesCatalogo.AuditoriaVer)]
[Route("auditoria")]
public class AuditoriaController : Controller
{
    private readonly IAppDbContext _context;
    private readonly IAuditoriaService _auditoriaService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<AuditoriaController> _logger;

    public AuditoriaController(
        IAppDbContext context,
        IAuditoriaService auditoriaService,
        IDateTimeService dateTimeService,
        ILogger<AuditoriaController> logger)
    {
        _context = context;
        _auditoriaService = auditoriaService;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? acao = null,
        string? entidade = null,
        string? utilizador = null,
        int pagina = 1,
        int tamanho = 50)
    {
        var query = _context.Auditoria.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(acao))
            query = query.Where(a => a.Acao.Contains(acao));

        if (!string.IsNullOrWhiteSpace(entidade))
            query = query.Where(a => a.Entidade.Contains(entidade));

        if (!string.IsNullOrWhiteSpace(utilizador))
            query = query.Where(a => a.NomeUtilizadorSnapshot.Contains(utilizador));

        var total = await query.CountAsync();
        var registos = await query
            .OrderByDescending(a => a.Id)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync();

        ViewBag.Acao = acao;
        ViewBag.Entidade = entidade;
        ViewBag.Utilizador = utilizador;
        ViewBag.Pagina = pagina;
        ViewBag.Tamanho = tamanho;
        ViewBag.Total = total;
        ViewBag.TotalPaginas = (int)Math.Ceiling((double)total / tamanho);
        ViewBag.DateTimeService = _dateTimeService;

        return View(registos);
    }

    [HttpPost("verificar")]
    [Authorize(Policy = PermissoesCatalogo.AuditoriaVerificarIntegridade)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verificar()
    {
        try
        {
            var resultado = await _auditoriaService.VerificarIntegridadeAsync();
            if (resultado.Valido)
            {
                TempData["Sucesso"] = $"Auditoria WORM Verificada com Sucesso: {resultado.Mensagem}";
            }
            else
            {
                TempData["Erro"] = $"Alerta Crítico: {resultado.Mensagem}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar integridade da cadeia de auditoria");
            TempData["Erro"] = $"Erro ao verificar cadeia de auditoria: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}
