using Menova.Application.Interfaces;
using Menova.Application.Security;
using Menova.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Menova.Web.Controllers;

[Authorize(Policy = PermissoesCatalogo.UtilizadoresVer)]
[Route("utilizadores")]
public class UtilizadoresController : Controller
{
    private readonly IAppDbContext _context;
    private readonly IPasswordHasher<Utilizador> _passwordHasher;
    private readonly IPinService _pinService;
    private readonly IAuditoriaService _auditoriaService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<UtilizadoresController> _logger;

    public UtilizadoresController(
        IAppDbContext context,
        IPasswordHasher<Utilizador> passwordHasher,
        IPinService pinService,
        IAuditoriaService auditoriaService,
        IDateTimeService dateTimeService,
        ILogger<UtilizadoresController> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _pinService = pinService;
        _auditoriaService = auditoriaService;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var utilizadores = await _context.Utilizadores
            .Include(u => u.UtilizadoresPerfis).ThenInclude(up => up.Perfil)
            .OrderBy(u => u.NomeCompleto)
            .ToListAsync();

        var perfis = await _context.Perfis.Where(p => p.Ativo).ToListAsync();

        ViewBag.Perfis = perfis;
        ViewBag.DateTimeService = _dateTimeService;

        return View(utilizadores);
    }

    [HttpPost("criar")]
    [Authorize(Policy = PermissoesCatalogo.UtilizadoresGerir)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(
        string nomeCompleto,
        string nomeUtilizador,
        string? email,
        string? telefone,
        Guid perfilId,
        string palavraPasse,
        string? pin)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(nomeUtilizador) || string.IsNullOrWhiteSpace(palavraPasse))
            {
                TempData["Erro"] = "Nome de utilizador e palavra-passe são obrigatórios.";
                return RedirectToAction(nameof(Index));
            }

            var existe = await _context.Utilizadores.AnyAsync(u => u.NomeUtilizador == nomeUtilizador);
            if (existe)
            {
                TempData["Erro"] = $"O nome de utilizador '{nomeUtilizador}' já existe.";
                return RedirectToAction(nameof(Index));
            }

            var utilizador = new Utilizador
            {
                NomeCompleto = nomeCompleto,
                NomeUtilizador = nomeUtilizador.Trim().ToLowerInvariant(),
                Email = email,
                Telefone = telefone,
                Ativo = true,
                SeloSeguranca = Guid.NewGuid().ToString("N")
            };

            utilizador.HashPalavraPasse = _passwordHasher.HashPassword(utilizador, palavraPasse);

            if (!string.IsNullOrWhiteSpace(pin) && pin.Length == 4)
            {
                var salt = _pinService.GerarSalt();
                utilizador.HashPin = _pinService.HashPin(pin, salt);
            }

            _context.Utilizadores.Add(utilizador);

            _context.UtilizadoresPerfis.Add(new UtilizadorPerfil
            {
                UtilizadorId = utilizador.Id,
                PerfilId = perfilId
            });

            await _context.SaveChangesAsync();

            await _auditoriaService.RegistarAsync(
                acao: "UTILIZADOR.CRIAR",
                entidade: "utilizadores",
                entidadeId: utilizador.Id.ToString(),
                valoresDepois: new { utilizador.NomeUtilizador, utilizador.NomeCompleto, perfilId },
                motivo: "Criação de novo colaborador");

            TempData["Sucesso"] = $"Utilizador '{utilizador.NomeUtilizador}' criado com sucesso.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar utilizador");
            TempData["Erro"] = $"Erro ao criar utilizador: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("toggle-ativo/{id:guid}")]
    [Authorize(Policy = PermissoesCatalogo.UtilizadoresGerir)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAtivo(Guid id)
    {
        var u = await _context.Utilizadores.FindAsync(id);
        if (u == null)
        {
            TempData["Erro"] = "Utilizador não encontrado.";
            return RedirectToAction(nameof(Index));
        }

        u.Ativo = !u.Ativo;
        await _context.SaveChangesAsync();

        await _auditoriaService.RegistarAsync(
            acao: u.Ativo ? "UTILIZADOR.ATIVAR" : "UTILIZADOR.DESATIVAR",
            entidade: "utilizadores",
            entidadeId: u.Id.ToString(),
            valoresDepois: new { u.Ativo },
            motivo: "Alteração de estado de atividade");

        TempData["Sucesso"] = $"Estado do utilizador '{u.NomeUtilizador}' alterado com sucesso.";
        return RedirectToAction(nameof(Index));
    }
}
