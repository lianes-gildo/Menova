using Menova.Application.Interfaces;
using Menova.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Menova.Web.Controllers;

[Authorize(Policy = PermissoesCatalogo.BackupsVer)]
[Route("backups")]
public class BackupsController : Controller
{
    private readonly IBackupService _backupService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<BackupsController> _logger;

    public BackupsController(
        IBackupService backupService,
        IDateTimeService dateTimeService,
        ILogger<BackupsController> logger)
    {
        _backupService = backupService;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var lista = await _backupService.ListarBackupsAsync();
        ViewBag.DateTimeService = _dateTimeService;
        return View(lista);
    }

    [HttpPost("criar")]
    [Authorize(Policy = PermissoesCatalogo.BackupsExecutar)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(string? motivo = null)
    {
        try
        {
            var resultado = await _backupService.CriarBackupAsync(motivo);
            if (resultado.Sucesso)
            {
                TempData["Sucesso"] = resultado.Mensagem;
            }
            else
            {
                TempData["Erro"] = resultado.Mensagem;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar cópia de segurança");
            TempData["Erro"] = $"Erro ao gerar cópia de segurança: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("download/{nomeFicheiro}")]
    public async Task<IActionResult> Download(string nomeFicheiro)
    {
        var stream = await _backupService.ObterStreamBackupAsync(nomeFicheiro);
        if (stream == null)
        {
            TempData["Erro"] = "Ficheiro de cópia de segurança não encontrado.";
            return RedirectToAction(nameof(Index));
        }

        var contentType = nomeFicheiro.EndsWith(".dump", StringComparison.OrdinalIgnoreCase)
            ? "application/octet-stream"
            : "application/gzip";

        return File(stream, contentType, nomeFicheiro);
    }

    [HttpPost("verificar/{nomeFicheiro}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verificar(string nomeFicheiro)
    {
        try
        {
            var integro = await _backupService.VerificarIntegridadeBackupAsync(nomeFicheiro);
            if (integro)
            {
                TempData["Sucesso"] = $"Ficheiro {nomeFicheiro} verificado com sucesso. Assinatura SHA-256 autêntica.";
            }
            else
            {
                TempData["Erro"] = $"Alerta: O ficheiro {nomeFicheiro} está corrompido ou o hash SHA-256 não confere!";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar integridade do backup");
            TempData["Erro"] = $"Erro ao verificar ficheiro: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("eliminar/{nomeFicheiro}")]
    [Authorize(Policy = PermissoesCatalogo.BackupsExecutar)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(string nomeFicheiro)
    {
        try
        {
            await _backupService.EliminarBackupAsync(nomeFicheiro);
            TempData["Sucesso"] = $"Cópia de segurança {nomeFicheiro} eliminada com sucesso.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao eliminar backup");
            TempData["Erro"] = $"Erro ao eliminar ficheiro: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}
