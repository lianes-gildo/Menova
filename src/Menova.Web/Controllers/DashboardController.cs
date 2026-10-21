using Menova.Application.Interfaces;
using Menova.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Menova.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IAppDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly ICurrentUserService _currentUser;

    public DashboardController(
        IAppDbContext context,
        IDateTimeService dateTimeService,
        ICurrentUserService currentUser)
    {
        _context = context;
        _dateTimeService = dateTimeService;
        _currentUser = currentUser;
    }

    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Index()
    {
        // 1. Redirecionamento inteligente para perfis estritamente operacionais
        if (User.IsInRole(PerfisSistema.Garcom) && !User.IsInRole(PerfisSistema.Administrador) && !User.IsInRole(PerfisSistema.Gerente))
        {
            return RedirectToAction("Index", "Garcom");
        }

        if (User.IsInRole(PerfisSistema.CozinhaBar) && !User.IsInRole(PerfisSistema.Administrador) && !User.IsInRole(PerfisSistema.Gerente))
        {
            return RedirectToAction("Index", "Kds");
        }

        if (User.IsInRole(PerfisSistema.Caixa) && !User.IsInRole(PerfisSistema.Administrador) && !User.IsInRole(PerfisSistema.Gerente))
        {
            return RedirectToAction("Index", "Caixa");
        }

        // 2. Proteção de Painel Executivo / Financeiro: utilizadores sem PainelVer são direcionados ao seu posto
        if (!_currentUser.TemPermissao(PermissoesCatalogo.PainelVer))
        {
            if (_currentUser.TemPermissao(PermissoesCatalogo.ContasAbrir))
            {
                return RedirectToAction("Index", "Garcom");
            }
            if (_currentUser.TemPermissao(PermissoesCatalogo.CaixaAbrir) || _currentUser.TemPermissao(PermissoesCatalogo.CaixaCobrar))
            {
                return RedirectToAction("Index", "Caixa");
            }
            if (_currentUser.TemPermissao(PermissoesCatalogo.CozinhaVer))
            {
                return RedirectToAction("Index", "Kds");
            }

            return RedirectToAction("AcessoNegado", "Erros");
        }

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
