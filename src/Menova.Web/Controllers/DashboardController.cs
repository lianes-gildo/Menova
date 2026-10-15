using Menova.Application.Interfaces;
using Menova.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Menova.Web.Controllers;

[Authorize(Policy = PermissoesCatalogo.PainelVer)]
public class DashboardController : Controller
{
    private readonly IAppDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public DashboardController(IAppDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Index()
    {
        var restaurante = await _context.Restaurante.FirstOrDefaultAsync();
        var totalUtilizadoresAtivos = await _context.Utilizadores.CountAsync(u => u.Ativo);
        var totalPerfis = await _context.Perfis.CountAsync(p => p.Ativo);

        ViewBag.RestauranteNome = restaurante?.Nome ?? "Menova Restaurante";
        ViewBag.RestauranteNuit = restaurante?.Nuit ?? "100000000";
        ViewBag.Moeda = restaurante?.Moeda ?? "MZN";
        ViewBag.FusoHorario = _dateTimeService.FusoHorario.Id;
        ViewBag.HoraLocal = _dateTimeService.FormatarDataHora(DateTime.UtcNow);
        ViewBag.TotalUtilizadores = totalUtilizadoresAtivos;
        ViewBag.TotalPerfis = totalPerfis;

        return View();
    }
}
