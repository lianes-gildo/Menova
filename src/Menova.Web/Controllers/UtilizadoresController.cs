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

            var usernameNormalizado = nomeUtilizador.Trim().ToLowerInvariant();
            if (usernameNormalizado == "admin")
            {
                TempData["Erro"] = "O nome de utilizador 'admin' é reservado para a conta raiz do sistema.";
                return RedirectToAction(nameof(Index));
            }

            var existe = await _context.Utilizadores.AnyAsync(u => u.NomeUtilizador == usernameNormalizado);
            if (existe)
            {
                TempData["Erro"] = $"O nome de utilizador '{nomeUtilizador}' já existe.";
                return RedirectToAction(nameof(Index));
            }

            var utilizador = new Utilizador
            {
                NomeCompleto = nomeCompleto?.Trim() ?? usernameNormalizado,
                NomeUtilizador = usernameNormalizado,
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                Telefone = string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim(),
                Ativo = true,
                SeloSeguranca = Guid.NewGuid().ToString("N")
            };

            utilizador.HashPalavraPasse = _passwordHasher.HashPassword(utilizador, palavraPasse);

            if (!string.IsNullOrWhiteSpace(pin))
            {
                var pinLimpo = pin.Trim();
                if (pinLimpo.Length != 4 || !pinLimpo.All(char.IsDigit))
                {
                    TempData["Erro"] = "O PIN de acesso rápido deve conter exatamente 4 dígitos numéricos.";
                    return RedirectToAction(nameof(Index));
                }

                if (await IsPinEmUsoAsync(pinLimpo))
                {
                    TempData["Erro"] = "Este código PIN já se encontra atribuído a outro colaborador ativo. Cada PIN deve ser estritamente único no restaurante.";
                    return RedirectToAction(nameof(Index));
                }

                var salt = _pinService.GerarSalt();
                utilizador.HashPin = _pinService.HashPin(pinLimpo, salt);
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
                valoresDepois: new { utilizador.NomeUtilizador, utilizador.NomeCompleto, perfilId, HasPin = utilizador.HashPin != null },
                motivo: "Criação de novo colaborador");

            TempData["Sucesso"] = $"Colaborador '{utilizador.NomeUtilizador}' criado com sucesso.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar utilizador");
            TempData["Erro"] = $"Erro ao criar utilizador: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("editar/{id:guid}")]
    [Authorize(Policy = PermissoesCatalogo.UtilizadoresGerir)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        Guid id,
        string nomeCompleto,
        string? email,
        string? telefone,
        Guid perfilId,
        string? novaPalavraPasse,
        string? pin,
        bool removerPin = false)
    {
        try
        {
            var utilizador = await _context.Utilizadores
                .Include(u => u.UtilizadoresPerfis)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (utilizador == null)
            {
                TempData["Erro"] = "Colaborador não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            if (utilizador.EhRootAdmin)
            {
                TempData["Erro"] = "O Administrador Principal (raiz) não pode ser editado. As suas credenciais só podem ser alteradas através do menu pessoal de alteração de palavra-passe.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(nomeCompleto))
            {
                TempData["Erro"] = "O nome completo do colaborador é obrigatório.";
                return RedirectToAction(nameof(Index));
            }

            utilizador.NomeCompleto = nomeCompleto.Trim();
            utilizador.Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
            utilizador.Telefone = string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim();

            // Atualizar Perfil / Função
            var perfilAtual = utilizador.UtilizadoresPerfis.FirstOrDefault();
            if (perfilAtual != null)
            {
                perfilAtual.PerfilId = perfilId;
            }
            else
            {
                utilizador.UtilizadoresPerfis.Add(new UtilizadorPerfil
                {
                    UtilizadorId = utilizador.Id,
                    PerfilId = perfilId
                });
            }

            // Atualizar Palavra-passe caso tenha sido fornecida
            if (!string.IsNullOrWhiteSpace(novaPalavraPasse))
            {
                if (novaPalavraPasse.Length < 6)
                {
                    TempData["Erro"] = "A nova palavra-passe deve conter pelo menos 6 caracteres.";
                    return RedirectToAction(nameof(Index));
                }
                utilizador.HashPalavraPasse = _passwordHasher.HashPassword(utilizador, novaPalavraPasse);
                utilizador.SeloSeguranca = Guid.NewGuid().ToString("N");
            }

            // Atualizar ou remover PIN
            if (removerPin)
            {
                utilizador.HashPin = null;
            }
            else if (!string.IsNullOrWhiteSpace(pin))
            {
                var pinLimpo = pin.Trim();
                if (pinLimpo.Length != 4 || !pinLimpo.All(char.IsDigit))
                {
                    TempData["Erro"] = "O PIN deve conter exatamente 4 dígitos numéricos.";
                    return RedirectToAction(nameof(Index));
                }

                if (await IsPinEmUsoAsync(pinLimpo, utilizador.Id))
                {
                    TempData["Erro"] = "Este código PIN já se encontra atribuído a outro colaborador ativo. Cada PIN deve ser estritamente único.";
                    return RedirectToAction(nameof(Index));
                }

                var salt = _pinService.GerarSalt();
                utilizador.HashPin = _pinService.HashPin(pinLimpo, salt);
            }

            await _context.SaveChangesAsync();

            await _auditoriaService.RegistarAsync(
                acao: "UTILIZADOR.EDITAR",
                entidade: "utilizadores",
                entidadeId: utilizador.Id.ToString(),
                valoresDepois: new { utilizador.NomeUtilizador, utilizador.NomeCompleto, perfilId, HasPin = utilizador.HashPin != null },
                motivo: "Atualização cadastral do colaborador");

            TempData["Sucesso"] = $"Colaborador '{utilizador.NomeUtilizador}' atualizado com sucesso.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao editar colaborador");
            TempData["Erro"] = $"Erro ao editar colaborador: {ex.Message}";
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
            TempData["Erro"] = "Colaborador não encontrado.";
            return RedirectToAction(nameof(Index));
        }

        if (u.EhRootAdmin)
        {
            TempData["Erro"] = "O Administrador Principal (raiz) está permanentemente protegido e não pode ser desativado.";
            return RedirectToAction(nameof(Index));
        }

        var utilizadorAtualNome = User.Identity?.Name;
        if (string.Equals(u.NomeUtilizador, utilizadorAtualNome, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Erro"] = "Não é permitido desativar a sua própria conta enquanto a sua sessão estiver em curso.";
            return RedirectToAction(nameof(Index));
        }

        u.Ativo = !u.Ativo;
        await _context.SaveChangesAsync();

        await _auditoriaService.RegistarAsync(
            acao: u.Ativo ? "UTILIZADOR.ATIVAR" : "UTILIZADOR.DESATIVAR",
            entidade: "utilizadores",
            entidadeId: u.Id.ToString(),
            valoresDepois: new { u.Ativo },
            motivo: "Alteração de estado operacional do colaborador");

        TempData["Sucesso"] = $"Estado do colaborador '{u.NomeUtilizador}' alterado para {(u.Ativo ? "Ativo" : "Inativo")}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> IsPinEmUsoAsync(string pin, Guid? ignorarUtilizadorId = null)
    {
        var utilizadoresComPin = await _context.Utilizadores
            .Where(u => u.Ativo && u.HashPin != null && (ignorarUtilizadorId == null || u.Id != ignorarUtilizadorId.Value))
            .ToListAsync();

        foreach (var u in utilizadoresComPin)
        {
            var partes = u.HashPin!.Split(':');
            if (partes.Length == 2 && _pinService.VerificarPin(pin, partes[0], u.HashPin))
            {
                return true;
            }
        }
        return false;
    }
}
