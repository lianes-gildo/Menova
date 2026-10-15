using Menova.Application.Interfaces;
using Menova.Application.Security;
using Menova.Domain.Configuracao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Menova.Web.Controllers;

[Authorize(Policy = PermissoesCatalogo.DefinicoesGerir)]
[Route("definicoes")]
public class DefinicoesController : Controller
{
    private readonly IAppDbContext _context;
    private readonly IAuditoriaService _auditoriaService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<DefinicoesController> _logger;

    public DefinicoesController(
        IAppDbContext context,
        IAuditoriaService auditoriaService,
        IDateTimeService dateTimeService,
        ILogger<DefinicoesController> logger)
    {
        _context = context;
        _auditoriaService = auditoriaService;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var restaurante = await _context.Restaurante.FirstOrDefaultAsync();
        if (restaurante == null)
        {
            restaurante = new Restaurante
            {
                Nome = "Menova Restaurante",
                Nuit = "100000000",
                Morada = "Av. Julius Nyerere, Maputo, Moçambique",
                Telefone = "+258 84 000 0000",
                Email = "contacto@menova.mz",
                RodapeTalao = "Obrigado pela preferência! Volte sempre.",
                Moeda = "MZN",
                CasasDecimais = 2,
                FusoHorario = "Africa/Maputo",
                TaxaIvaPadrao = 16.00m
            };
            _context.Restaurante.Add(restaurante);
            await _context.SaveChangesAsync();
        }

        ViewBag.TotalArtigos = await _context.Artigos.CountAsync(a => a.EliminadoEm == null);
        ViewBag.TotalMesas = await _context.Mesas.CountAsync(m => m.EliminadoEm == null);
        ViewBag.TotalFuncionarios = await _context.Utilizadores.CountAsync(u => u.Ativo);
        ViewBag.DateTimeService = _dateTimeService;

        return View(restaurante);
    }

    [HttpPost("guardar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(
        string nome,
        string nuit,
        string? morada,
        string? telefone,
        string? email,
        string? rodapeTalao,
        string moeda,
        decimal taxaIvaPadrao)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            TempData["Erro"] = "O nome do restaurante é obrigatório.";
            return RedirectToAction(nameof(Index));
        }

        var restaurante = await _context.Restaurante.FirstOrDefaultAsync();
        if (restaurante == null)
        {
            restaurante = new Restaurante();
            _context.Restaurante.Add(restaurante);
        }

        restaurante.Nome = nome.Trim();
        restaurante.Nuit = string.IsNullOrWhiteSpace(nuit) ? "100000000" : nuit.Trim();
        restaurante.Morada = morada?.Trim();
        restaurante.Telefone = telefone?.Trim();
        restaurante.Email = email?.Trim();
        restaurante.RodapeTalao = rodapeTalao?.Trim() ?? "Obrigado pela preferência! Volte sempre.";
        restaurante.Moeda = string.IsNullOrWhiteSpace(moeda) ? "MZN" : moeda.Trim().ToUpperInvariant();
        restaurante.FusoHorario = "Africa/Maputo"; // Fixo Moçambique
        restaurante.TaxaIvaPadrao = taxaIvaPadrao > 0 ? taxaIvaPadrao : 16.00m;

        await _context.SaveChangesAsync();

        await _auditoriaService.RegistarAsync(
            acao: "DEFINICOES_ALTERADAS",
            entidade: "Configuracao",
            entidadeId: restaurante.Id.ToString(),
            valoresDepois: new { restaurante.Nome, restaurante.Nuit, restaurante.Moeda, restaurante.TaxaIvaPadrao },
            motivo: $"Definições do restaurante '{restaurante.Nome}' atualizadas.");

        TempData["Sucesso"] = "Definições do restaurante guardadas com sucesso.";
        return RedirectToAction(nameof(Index));
    }
}
