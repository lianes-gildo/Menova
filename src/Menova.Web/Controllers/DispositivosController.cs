using System.Security.Cryptography;
using Menova.Application.Interfaces;
using Menova.Application.Security;
using Menova.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Menova.Web.Controllers;

[Authorize(Policy = PermissoesCatalogo.DispositivosGerir)]
[Route("dispositivos")]
public class DispositivosController : Controller
{
    private readonly IAppDbContext _context;
    private readonly IAuditoriaService _auditoriaService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<DispositivosController> _logger;

    public DispositivosController(
        IAppDbContext context,
        IAuditoriaService auditoriaService,
        IDateTimeService dateTimeService,
        ILogger<DispositivosController> logger)
    {
        _context = context;
        _auditoriaService = auditoriaService;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var dispositivos = await _context.Dispositivos
            .OrderByDescending(d => d.CriadoEm)
            .ToListAsync();

        ViewBag.DateTimeService = _dateTimeService;
        ViewBag.Total = dispositivos.Count;
        ViewBag.Autorizados = dispositivos.Count(d => d.Estado == EstadoDispositivo.Autorizado);
        ViewBag.Pendentes = dispositivos.Count(d => d.Estado == EstadoDispositivo.Pendente);
        ViewBag.Revogados = dispositivos.Count(d => d.Estado == EstadoDispositivo.Revogado);

        return View(dispositivos);
    }

    [HttpPost("registar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registar(string nome, TipoDispositivo tipo)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            TempData["Erro"] = "O nome do dispositivo é obrigatório.";
            return RedirectToAction(nameof(Index));
        }

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var tokenHash = Convert.ToHexString(SHA256.HashData(tokenBytes));

        var dispositivo = new Dispositivo
        {
            Nome = nome.Trim(),
            Tipo = tipo,
            TokenHash = tokenHash,
            Estado = EstadoDispositivo.Autorizado,
            AutorizadoPor = User.Identity?.Name ?? "Sistema",
            AutorizadoEm = _dateTimeService.NowUtc,
            UltimoIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString()
        };

        _context.Dispositivos.Add(dispositivo);
        await _context.SaveChangesAsync();

        await _auditoriaService.RegistarAsync(
            acao: "DISPOSITIVO_REGISTADO",
            entidade: "Dispositivos",
            entidadeId: dispositivo.Id.ToString(),
            valoresDepois: new { dispositivo.Nome, Tipo = tipo.ToString(), dispositivo.Estado },
            motivo: $"Dispositivo '{dispositivo.Nome}' registado e autorizado.");

        TempData["Sucesso"] = $"Dispositivo '{dispositivo.Nome}' registado e autorizado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("autorizar/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Autorizar(Guid id)
    {
        var dispositivo = await _context.Dispositivos.FindAsync(id);
        if (dispositivo == null)
        {
            TempData["Erro"] = "Dispositivo não encontrado.";
            return RedirectToAction(nameof(Index));
        }

        dispositivo.Estado = EstadoDispositivo.Autorizado;
        dispositivo.AutorizadoPor = User.Identity?.Name ?? "Sistema";
        dispositivo.AutorizadoEm = _dateTimeService.NowUtc;

        await _context.SaveChangesAsync();

        await _auditoriaService.RegistarAsync(
            acao: "DISPOSITIVO_AUTORIZADO",
            entidade: "Dispositivos",
            entidadeId: dispositivo.Id.ToString(),
            valoresDepois: new { dispositivo.Nome, dispositivo.Estado },
            motivo: $"Dispositivo '{dispositivo.Nome}' autorizado.");

        TempData["Sucesso"] = $"Dispositivo '{dispositivo.Nome}' autorizado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("revogar/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revogar(Guid id)
    {
        var dispositivo = await _context.Dispositivos.FindAsync(id);
        if (dispositivo == null)
        {
            TempData["Erro"] = "Dispositivo não encontrado.";
            return RedirectToAction(nameof(Index));
        }

        dispositivo.Estado = EstadoDispositivo.Revogado;

        // Revogar sessões ativas do dispositivo
        var sessoes = await _context.SessoesUtilizador
            .Where(s => s.DispositivoId == id && !s.Revogada)
            .ToListAsync();

        foreach (var sessao in sessoes)
        {
            sessao.Revogada = true;
        }

        await _context.SaveChangesAsync();

        await _auditoriaService.RegistarAsync(
            acao: "DISPOSITIVO_REVOGADO",
            entidade: "Dispositivos",
            entidadeId: dispositivo.Id.ToString(),
            valoresDepois: new { dispositivo.Nome, dispositivo.Estado, SessoesRevogadas = sessoes.Count },
            motivo: $"Dispositivo '{dispositivo.Nome}' revogado.");

        TempData["Sucesso"] = $"Dispositivo '{dispositivo.Nome}' revogado com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("eliminar/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var dispositivo = await _context.Dispositivos.FindAsync(id);
        if (dispositivo == null)
        {
            TempData["Erro"] = "Dispositivo não encontrado.";
            return RedirectToAction(nameof(Index));
        }

        dispositivo.EliminadoEm = _dateTimeService.NowUtc;
        dispositivo.EliminadoPor = User.Identity?.Name ?? "Sistema";

        await _context.SaveChangesAsync();

        await _auditoriaService.RegistarAsync(
            acao: "DISPOSITIVO_ELIMINADO",
            entidade: "Dispositivos",
            entidadeId: dispositivo.Id.ToString(),
            valoresDepois: new { dispositivo.Nome },
            motivo: $"Dispositivo '{dispositivo.Nome}' eliminado do sistema.");

        TempData["Sucesso"] = $"Dispositivo '{dispositivo.Nome}' eliminado com sucesso.";
        return RedirectToAction(nameof(Index));
    }
}
