using Menova.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Menova.Web.Controllers;

[AllowAnonymous]
[Route("erros")]
public class ErrosController : Controller
{
    private readonly ICurrentUserService _currentUserService;

    public ErrosController(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    [HttpGet("404")]
    public IActionResult NaoEncontrado()
    {
        Response.StatusCode = 404;
        ViewBag.CorrelacaoId = _currentUserService.CorrelacaoId;
        return View("404");
    }

    [HttpGet("403")]
    [HttpGet("acesso-negado")]
    public IActionResult AcessoNegado()
    {
        Response.StatusCode = 403;
        ViewBag.CorrelacaoId = _currentUserService.CorrelacaoId;
        ViewBag.Perfil = _currentUserService.PerfilCodigo ?? "Sem perfil atribuído";
        return View("403");
    }

    [HttpGet("500")]
    public IActionResult ErroServidor()
    {
        Response.StatusCode = 500;
        ViewBag.CorrelacaoId = _currentUserService.CorrelacaoId;
        return View("500");
    }

    [HttpGet("bloqueado")]
    public IActionResult Bloqueado()
    {
        Response.StatusCode = 423;
        return View("Bloqueado");
    }
}

[AllowAnonymous]
[Route("health")]
public class HealthController : Controller
{
    private readonly IAppDbContext _context;

    public HealthController(IAppDbContext context)
    {
        _context = context;
    }

    [HttpGet("live")]
    [HttpHead("live")]
    public IActionResult Live()
    {
        return Ok(new { status = "Healthy", servico = "Menova POS", timestamp = DateTime.UtcNow });
    }

    [HttpGet("ready")]
    [HttpHead("ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                return StatusCode(503, new { status = "Unhealthy", motivo = "Falha de conectividade com a base de dados PostgreSQL." });
            }

            return Ok(new { status = "Healthy", baseDados = "PostgreSQL Conectado", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { status = "Unhealthy", motivo = ex.Message });
        }
    }
}

[AllowAnonymous]
[Route("instalar-certificado")]
public class CertificadoController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }
}
