using System.Security.Claims;
using FluentValidation;
using Menova.Application.DTOs;
using Menova.Application.Interfaces;
using Menova.Application.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Menova.Web.Controllers;

[Route("auth")]
public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly IValidator<LoginDto> _loginValidator;
    private readonly IValidator<AlterarPalavraPasseDto> _alterarPalavraPasseValidator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IValidator<LoginDto> loginValidator,
        IValidator<AlterarPalavraPasseDto> alterarPalavraPasseValidator,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _loginValidator = loginValidator;
        _alterarPalavraPasseValidator = alterarPalavraPasseValidator;
        _logger = logger;
    }

    [HttpGet("login")]
    [HttpGet("/login")]
    [HttpHead("login")]
    [HttpHead("/login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null, string? motivo = null)
    {
        // 1. Verificar se há notificação de logout por concorrência ou inatividade
        if (string.IsNullOrEmpty(motivo) && Request.Cookies.TryGetValue("menova_logout_motivo", out var cookieMotivo))
        {
            motivo = cookieMotivo;
            Response.Cookies.Delete("menova_logout_motivo");
        }

        if (motivo == "sessao_duplicada")
        {
            ViewBag.AlertaMotivo = "A sua sessão foi encerrada porque foi iniciada sessão noutro dispositivo. Não é permitida a utilização simultânea da mesma conta em múltiplos dispositivos.";
        }
        else if (motivo == "inactividade")
        {
            ViewBag.AlertaMotivo = "A sua sessão expirou automaticamente por inatividade (15 minutos). Por favor, inicie sessão novamente.";
        }

        if (User.Identity?.IsAuthenticated == true && string.IsNullOrEmpty(motivo))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginDto());
    }

    [HttpPost("login")]
    [HttpPost("/login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        var validationResult = await _loginValidator.ValidateAsync(model);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return View(model);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var resultado = await _authService.AutenticarPainelAsync(model.NomeUtilizador, model.PalavraPasse, ip, userAgent);

        if (!resultado.Sucesso)
        {
            ModelState.AddModelError(string.Empty, resultado.MensagemErro ?? "Falha na autenticação.");
            return View(model);
        }

        // Criar claims e autenticar com Cookie contendo Selo de Segurança único
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, resultado.UtilizadorId!.Value.ToString()),
            new(ClaimTypes.Name, resultado.NomeUtilizador!),
            new("nome_completo", resultado.NomeCompleto ?? resultado.NomeUtilizador!),
            new(ClaimTypes.Role, resultado.PerfilCodigo ?? "OPERADOR"),
            new("deve_alterar_palavra_passe", resultado.DeveAlterarPalavraPasse ? "true" : "false"),
            new("selo_seguranca", resultado.SeloSeguranca ?? string.Empty)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.LembrarMe,
            ExpiresUtc = model.LembrarMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddMinutes(15)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        if (resultado.DeveAlterarPalavraPasse)
        {
            TempData["Aviso"] = "Por motivos de segurança, deve alterar a sua palavra-passe temporária no primeiro acesso.";
            return RedirectToAction(nameof(AlterarPalavraPasse));
        }

        return RedirecionarPorPerfil(resultado.PerfilCodigo, returnUrl);
    }

    [HttpPost("login-pin")]
    [HttpPost("/login-pin")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginPin(string pin, string? returnUrl = null, string? tokenDispositivo = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (string.IsNullOrWhiteSpace(pin) || pin.Length < 4)
        {
            TempData["ErroPin"] = "O PIN deve conter pelo menos 4 dígitos numéricos.";
            return RedirectToAction(nameof(Login), new { returnUrl, modo = "pin" });
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var resultado = await _authService.AutenticarPinAsync(pin.Trim(), tokenDispositivo ?? string.Empty, ip, userAgent);
        if (!resultado.Sucesso)
        {
            TempData["ErroPin"] = resultado.MensagemErro ?? "PIN incorreto.";
            return RedirectToAction(nameof(Login), new { returnUrl, modo = "pin" });
        }

        // Criar claims e autenticar com Cookie com Selo de Segurança único
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, resultado.UtilizadorId!.Value.ToString()),
            new(ClaimTypes.Name, resultado.NomeUtilizador!),
            new("nome_completo", resultado.NomeCompleto ?? resultado.NomeUtilizador!),
            new(ClaimTypes.Role, resultado.PerfilCodigo ?? "GARCOM"),
            new("deve_alterar_palavra_passe", "false"),
            new("selo_seguranca", resultado.SeloSeguranca ?? string.Empty)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(15)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return RedirecionarPorPerfil(resultado.PerfilCodigo, returnUrl);
    }

    [HttpGet("heartbeat")]
    [HttpPost("heartbeat")]
    [Authorize]
    public IActionResult Heartbeat()
    {
        return Ok(new { ativo = true, timestamp = DateTime.UtcNow });
    }

    private IActionResult RedirecionarPorPerfil(string? perfilCodigo, string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl != "/" && returnUrl != "/dashboard")
        {
            return Redirect(returnUrl);
        }

        if (string.Equals(perfilCodigo, PerfisSistema.Garcom, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Index", "Garcom");
        }

        if (string.Equals(perfilCodigo, PerfisSistema.CozinhaBar, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Index", "Kds");
        }

        if (string.Equals(perfilCodigo, PerfisSistema.Caixa, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction("Index", "Caixa");
        }

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet("alterar-palavra-passe")]
    [Authorize]
    public IActionResult AlterarPalavraPasse()
    {
        return View(new AlterarPalavraPasseDto());
    }

    [HttpPost("alterar-palavra-passe")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarPalavraPasse(AlterarPalavraPasseDto model)
    {
        var validationResult = await _alterarPalavraPasseValidator.ValidateAsync(model);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return View(model);
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return RedirectToAction(nameof(Login));
        }

        var (sucesso, mensagemErro) = await _authService.AlterarPalavraPasseAsync(userId, model.PalavraPasseAtual, model.NovaPalavraPasse);
        if (!sucesso)
        {
            ModelState.AddModelError(string.Empty, mensagemErro ?? "Erro ao alterar palavra-passe.");
            return View(model);
        }

        // Atualizar claims do utilizador para remover a flag de alteração obrigatória
        var claims = User.Claims.Where(c => c.Type != "deve_alterar_palavra_passe").ToList();
        claims.Add(new Claim("deve_alterar_palavra_passe", "false"));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        TempData["Sucesso"] = "A sua palavra-passe foi alterada com sucesso.";
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost("logout")]
    [HttpPost("/logout")]
    [HttpGet("logout")]
    [HttpGet("/logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            await _authService.TerminarSessaoAsync(userId);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
