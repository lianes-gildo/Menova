using Menova.Application.DTOs;
using Menova.Application.Interfaces;
using Menova.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Menova.Web.Controllers;

[Authorize(Policy = PermissoesCatalogo.ContasAbrir)]
[Route("garcom")]
public class GarcomController : Controller
{
    private readonly ISalaService _salaService;
    private readonly IMenuService _menuService;
    private readonly IDateTimeService _dateTimeService;

    public GarcomController(
        ISalaService salaService,
        IMenuService menuService,
        IDateTimeService dateTimeService)
    {
        _salaService = salaService;
        _menuService = menuService;
        _dateTimeService = dateTimeService;
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
}
