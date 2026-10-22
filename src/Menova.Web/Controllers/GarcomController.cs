using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Menova.Application.DTOs;
using Menova.Application.Interfaces;
using Menova.Application.Security;
using Menova.Domain.Identity;
using Menova.Domain.Pedidos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Menova.Web.Controllers;

public record AlterarPinGarcomDto(string? CredencialAtual, string NovoPin, string ConfirmarPin);
public record RegistarBiometriaDto(string CredentialId, string? PublicKey, string? Dispositivo);

[Authorize(Policy = PermissoesCatalogo.ContasAbrir)]
[Route("garcom")]
public class GarcomController : Controller
{
    private readonly ISalaService _salaService;
    private readonly IMenuService _menuService;
    private readonly IKdsService _kdsService;
    private readonly IAppDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPinService _pinService;
    private readonly IPasswordHasher<Utilizador> _passwordHasher;
    private readonly IAuditoriaService _auditoriaService;
    private readonly ILogger<GarcomController> _logger;

    public GarcomController(
        ISalaService salaService,
        IMenuService menuService,
        IKdsService kdsService,
        IAppDbContext context,
        IDateTimeService dateTimeService,
        ICurrentUserService currentUserService,
        IPinService pinService,
        IPasswordHasher<Utilizador> passwordHasher,
        IAuditoriaService auditoriaService,
        ILogger<GarcomController> logger)
    {
        _salaService = salaService;
        _menuService = menuService;
        _kdsService = kdsService;
        _context = context;
        _dateTimeService = dateTimeService;
        _currentUserService = currentUserService;
        _pinService = pinService;
        _passwordHasher = passwordHasher;
        _auditoriaService = auditoriaService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(Guid? mesaId = null)
    {
        var zonas = await _salaService.GetZonasComMesasAsync();
        var menu = await _menuService.GetMenuCompletoAsync();

        ViewBag.Zonas = zonas;
        ViewBag.Menu = menu;
        ViewBag.DateTimeService = _dateTimeService;
        ViewBag.MesaPreselecionadaId = mesaId;

        return View();
    }

    [HttpGet("perfil")]
    public async Task<IActionResult> Perfil()
    {
        var userId = _currentUserService.UtilizadorId;
        if (!userId.HasValue)
        {
            return RedirectToAction("Login", "Auth");
        }

        var utilizador = await _context.Utilizadores
            .Include(u => u.UtilizadoresPerfis).ThenInclude(up => up.Perfil)
            .FirstOrDefaultAsync(u => u.Id == userId.Value);

        if (utilizador == null)
        {
            return NotFound("Utilizador não encontrado.");
        }

        ViewBag.DateTimeService = _dateTimeService;
        return View(utilizador);
    }

    [HttpPost("alterar-pin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlterarPin([FromBody] AlterarPinGarcomDto dto)
    {
        var userId = _currentUserService.UtilizadorId;
        if (!userId.HasValue)
        {
            return Unauthorized(new { sucesso = false, mensagem = "Sessão expirada. Faça login novamente." });
        }

        if (dto == null)
        {
            return BadRequest(new { sucesso = false, mensagem = "Dados do pedido inválidos." });
        }

        var novoPin = dto.NovoPin?.Trim() ?? string.Empty;
        var confirmarPin = dto.ConfirmarPin?.Trim() ?? string.Empty;

        // 1. Validação do Formato do Novo PIN (exatamente 4 dígitos numéricos)
        if (!Regex.IsMatch(novoPin, @"^\d{4}$"))
        {
            return BadRequest(new { sucesso = false, mensagem = "O novo PIN deve ser composto exatamente por 4 dígitos numéricos (0-9)." });
        }

        if (novoPin != confirmarPin)
        {
            return BadRequest(new { sucesso = false, mensagem = "A confirmação do novo PIN não coincide com o PIN introduzido." });
        }

        var utilizador = await _context.Utilizadores.FindAsync(userId.Value);
        if (utilizador == null)
        {
            return NotFound(new { sucesso = false, mensagem = "Utilizador não encontrado." });
        }

        // 2. Validação da Credencial Atual (PIN atual ou Palavra-passe)
        var credencialAtual = dto.CredencialAtual?.Trim() ?? string.Empty;
        bool credencialValida = false;

        if (!string.IsNullOrEmpty(utilizador.HashPin))
        {
            // Se já tem PIN, permite validar com o PIN atual (se 4 dígitos) ou com a palavra-passe
            string[] partesAtual = utilizador.HashPin.Split(new[] { ':' }, StringSplitOptions.None);
            if (partesAtual.Length == 2 && credencialAtual.Length == 4 && _pinService.VerificarPin(credencialAtual, partesAtual[0], utilizador.HashPin))
            {
                credencialValida = true;
            }
        }

        if (!credencialValida && !string.IsNullOrEmpty(credencialAtual))
        {
            var resultPass = _passwordHasher.VerifyHashedPassword(utilizador, utilizador.HashPalavraPasse, credencialAtual);
            if (resultPass != PasswordVerificationResult.Failed)
            {
                credencialValida = true;
            }
        }

        if (!credencialValida)
        {
            return BadRequest(new { sucesso = false, mensagem = "A sua credencial atual (PIN ou palavra-passe) está incorreta." });
        }

        // 3. Validação de Unicidade de PIN no Restaurante (nenhum utilizador ativo pode ter o mesmo PIN)
        var utilizadoresComPin = await _context.Utilizadores
            .Where(u => u.Ativo && u.HashPin != null && u.Id != utilizador.Id)
            .ToListAsync();

        foreach (var u in utilizadoresComPin)
        {
            string[] partesOutro = u.HashPin!.Split(new[] { ':' }, StringSplitOptions.None);
            if (partesOutro.Length == 2 && _pinService.VerificarPin(novoPin, partesOutro[0], u.HashPin))
            {
                return BadRequest(new { sucesso = false, mensagem = "Este código PIN já está em uso por outro colaborador. Por favor, escolha um código diferente." });
            }
        }

        // 4. Gravação segura com Salt e Pepper PBKDF2
        var salt = _pinService.GerarSalt();
        utilizador.HashPin = _pinService.HashPin(novoPin, salt);
        utilizador.AtualizadoEm = DateTime.UtcNow;
        utilizador.AtualizadoPor = utilizador.NomeUtilizador;

        await _context.SaveChangesAsync();

        await _auditoriaService.RegistarAsync(
            "garcom.pin.alterado",
            "utilizadores",
            utilizador.Id.ToString(),
            valoresDepois: new { utilizador.NomeUtilizador, data = DateTime.UtcNow });

        return Json(new { sucesso = true, mensagem = "O seu código PIN de 4 dígitos foi alterado com sucesso! Já pode utilizá-lo para aceder rapidamente ao sistema." });
    }

    [HttpPost("biometria/desafio")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BiometriaDesafio()
    {
        var userId = _currentUserService.UtilizadorId;
        if (!userId.HasValue)
        {
            return Unauthorized(new { sucesso = false, mensagem = "Sessão expirada." });
        }

        var utilizador = await _context.Utilizadores.FindAsync(userId.Value);
        if (utilizador == null)
        {
            return NotFound(new { sucesso = false, mensagem = "Utilizador não encontrado." });
        }

        var challengeBytes = RandomNumberGenerator.GetBytes(32);
        var challengeBase64 = Convert.ToBase64String(challengeBytes);

        Response.Cookies.Append("menova_bio_reg_challenge", challengeBase64, new CookieOptions
        {
            HttpOnly = true,
            Secure = !Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase),
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddMinutes(5)
        });

        return Json(new
        {
            sucesso = true,
            challenge = challengeBase64,
            userId = utilizador.Id.ToString(),
            userName = utilizador.NomeUtilizador,
            displayName = utilizador.NomeCompleto
        });
    }

    [HttpPost("biometria/registar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BiometriaRegistar([FromBody] RegistarBiometriaDto dto)
    {
        var userId = _currentUserService.UtilizadorId;
        if (!userId.HasValue)
        {
            return Unauthorized(new { sucesso = false, mensagem = "Sessão expirada." });
        }

        if (dto == null || string.IsNullOrWhiteSpace(dto.CredentialId))
        {
            return BadRequest(new { sucesso = false, mensagem = "Identificador biométrico inválido." });
        }

        if (!Request.Cookies.TryGetValue("menova_bio_reg_challenge", out var challenge) || string.IsNullOrEmpty(challenge))
        {
            return BadRequest(new { sucesso = false, mensagem = "O desafio biométrico expirou. Por favor tente novamente." });
        }

        Response.Cookies.Delete("menova_bio_reg_challenge");

        var utilizador = await _context.Utilizadores.FindAsync(userId.Value);
        if (utilizador == null)
        {
            return NotFound(new { sucesso = false, mensagem = "Utilizador não encontrado." });
        }

        utilizador.BiometriaCredencialId = dto.CredentialId.Trim();
        utilizador.BiometriaPublicKey = dto.PublicKey;
        utilizador.BiometriaDispositivo = string.IsNullOrWhiteSpace(dto.Dispositivo) ? "Dispositivo Móvel" : dto.Dispositivo.Trim();
        utilizador.BiometriaRegistadaEm = DateTime.UtcNow;
        utilizador.AtualizadoEm = DateTime.UtcNow;
        utilizador.AtualizadoPor = utilizador.NomeUtilizador;

        await _context.SaveChangesAsync();

        await _auditoriaService.RegistarAsync(
            "garcom.biometria.registada",
            "utilizadores",
            utilizador.Id.ToString(),
            valoresDepois: new { utilizador.NomeUtilizador, dispositivo = utilizador.BiometriaDispositivo, data = DateTime.UtcNow });

        return Json(new
        {
            sucesso = true,
            mensagem = "Biometria (Face ID / Impressão Digital) registada com sucesso neste dispositivo!",
            credentialId = utilizador.BiometriaCredencialId,
            dispositivo = utilizador.BiometriaDispositivo,
            registadaEm = utilizador.BiometriaRegistadaEm?.ToString("dd/MM/yyyy HH:mm")
        });
    }

    [HttpPost("biometria/remover")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BiometriaRemover()
    {
        var userId = _currentUserService.UtilizadorId;
        if (!userId.HasValue)
        {
            return Unauthorized(new { sucesso = false, mensagem = "Sessão expirada." });
        }

        var utilizador = await _context.Utilizadores.FindAsync(userId.Value);
        if (utilizador == null)
        {
            return NotFound(new { sucesso = false, mensagem = "Utilizador não encontrado." });
        }

        utilizador.BiometriaCredencialId = null;
        utilizador.BiometriaPublicKey = null;
        utilizador.BiometriaDispositivo = null;
        utilizador.BiometriaRegistadaEm = null;
        utilizador.AtualizadoEm = DateTime.UtcNow;
        utilizador.AtualizadoPor = utilizador.NomeUtilizador;

        await _context.SaveChangesAsync();

        await _auditoriaService.RegistarAsync(
            "garcom.biometria.removida",
            "utilizadores",
            utilizador.Id.ToString(),
            valoresDepois: new { utilizador.NomeUtilizador, data = DateTime.UtcNow });

        return Json(new { sucesso = true, mensagem = "Biometria desativada com sucesso deste utilizador." });
    }

    [HttpGet("pedidos-cozinha")]
    public async Task<IActionResult> ObterPedidosCozinha()
    {
        try
        {
            var tickets = await _kdsService.GetTicketsAtivosAsync();
            return Json(tickets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter pedidos da cozinha para o garçom");
            return StatusCode(500, new { sucesso = false, mensagem = "Erro ao carregar estado da cozinha." });
        }
    }

    [HttpPost("entregar-item/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EntregarItem(Guid id)
    {
        try
        {
            await _kdsService.AtualizarEstadoItemAsync(id, EstadoItemPedido.Entregue);
            return Json(new { sucesso = true, mensagem = "Artigo entregue à mesa com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao marcar item {Id} como entregue", id);
            return StatusCode(500, new { sucesso = false, mensagem = "Erro ao marcar artigo como entregue." });
        }
    }

    [HttpPost("entregar-ticket/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EntregarTicket(Guid id)
    {
        try
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
            {
                return NotFound(new { sucesso = false, mensagem = "Ticket não encontrado." });
            }

            foreach (var item in pedido.Itens.Where(i => !i.Anulado && i.Estado != EstadoItemPedido.Entregue))
            {
                item.Estado = EstadoItemPedido.Entregue;
            }
            pedido.Estado = EstadoPedido.Entregue;
            await _context.SaveChangesAsync();

            return Json(new { sucesso = true, mensagem = "Todos os artigos do ticket foram entregues à mesa." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao entregar ticket completo {Id}", id);
            return StatusCode(500, new { sucesso = false, mensagem = "Erro ao entregar artigos do ticket." });
        }
    }
}
