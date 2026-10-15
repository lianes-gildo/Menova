using System.Text;
using Menova.Application.Interfaces;
using Menova.Application.Security;
using Menova.Domain.Impressao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Menova.Web.Controllers;

[Authorize(Policy = PermissoesCatalogo.ImpressorasGerir)]
[Route("impressoras")]
public class ImpressorasController : Controller
{
    private readonly IAppDbContext _context;
    private readonly IImpressaoService _impressaoService;
    private readonly IAuditoriaService _auditoriaService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<ImpressorasController> _logger;

    public ImpressorasController(
        IAppDbContext context,
        IImpressaoService impressaoService,
        IAuditoriaService auditoriaService,
        IDateTimeService dateTimeService,
        ILogger<ImpressorasController> logger)
    {
        _context = context;
        _impressaoService = impressaoService;
        _auditoriaService = auditoriaService;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var impressoras = await _context.Impressoras
            .Where(i => i.EliminadoEm == null)
            .OrderBy(i => i.Nome)
            .ToListAsync();

        var trabalhosRecentes = await _context.TrabalhosImpressao
            .Include(t => t.Impressora)
            .OrderByDescending(t => t.CriadoEm)
            .Take(15)
            .ToListAsync();

        ViewBag.TrabalhosRecentes = trabalhosRecentes;
        ViewBag.DateTimeService = _dateTimeService;

        return View(impressoras);
    }

    [HttpPost("criar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(
        string nome,
        string enderecoIp,
        int porta,
        int larguraColunas,
        bool cortarPapel,
        bool abrirGaveta)
    {
        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(enderecoIp))
        {
            TempData["Erro"] = "O nome e o endereço IP da impressora são obrigatórios.";
            return RedirectToAction(nameof(Index));
        }

        var impressora = new Impressora
        {
            Nome = nome.Trim(),
            EnderecoIp = enderecoIp.Trim(),
            Porta = porta > 0 ? porta : 9100,
            LarguraColunas = larguraColunas == 32 ? 32 : 42,
            CortarPapel = cortarPapel,
            AbrirGaveta = abrirGaveta,
            Ativa = true
        };

        _context.Impressoras.Add(impressora);
        await _context.SaveChangesAsync();

        await _auditoriaService.RegistarAsync(
            acao: "IMPRESSORA_CRIADA",
            entidade: "Impressoras",
            entidadeId: impressora.Id.ToString(),
            valoresDepois: new { impressora.Nome, impressora.EnderecoIp, impressora.Porta, impressora.LarguraColunas },
            motivo: $"Impressora '{impressora.Nome}' ({impressora.EnderecoIp}:{impressora.Porta}) configurada.");

        TempData["Sucesso"] = $"Impressora '{impressora.Nome}' criada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("editar/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        Guid id,
        string nome,
        string enderecoIp,
        int porta,
        int larguraColunas,
        bool ativa,
        bool cortarPapel,
        bool abrirGaveta)
    {
        var impressora = await _context.Impressoras.FindAsync(id);
        if (impressora == null || impressora.EliminadoEm != null)
        {
            TempData["Erro"] = "Impressora não encontrada.";
            return RedirectToAction(nameof(Index));
        }

        impressora.Nome = nome.Trim();
        impressora.EnderecoIp = enderecoIp.Trim();
        impressora.Porta = porta > 0 ? porta : 9100;
        impressora.LarguraColunas = larguraColunas == 32 ? 32 : 42;
        impressora.Ativa = ativa;
        impressora.CortarPapel = cortarPapel;
        impressora.AbrirGaveta = abrirGaveta;

        await _context.SaveChangesAsync();

        await _auditoriaService.RegistarAsync(
            acao: "IMPRESSORA_EDITADA",
            entidade: "Impressoras",
            entidadeId: impressora.Id.ToString(),
            valoresDepois: new { impressora.Nome, impressora.EnderecoIp, impressora.Porta, impressora.Ativa },
            motivo: $"Configuração da impressora '{impressora.Nome}' atualizada.");

        TempData["Sucesso"] = $"Impressora '{impressora.Nome}' atualizada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("testar/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Testar(Guid id)
    {
        var impressora = await _context.Impressoras.FindAsync(id);
        if (impressora == null || impressora.EliminadoEm != null)
        {
            TempData["Erro"] = "Impressora não encontrada.";
            return RedirectToAction(nameof(Index));
        }

        using var ms = new MemoryStream();
        // ESC @ inicializar
        ms.Write(new byte[] { 0x1B, 0x40 });
        // ESC a 1 (centro)
        ms.Write(new byte[] { 0x1B, 0x61, 0x01 });
        // ESC E 1 (negrito)
        ms.Write(new byte[] { 0x1B, 0x45, 0x01 });
        ms.Write(Encoding.Latin1.GetBytes("MENOVA RESTAURANTE\n"));
        ms.Write(Encoding.Latin1.GetBytes("SISTEMA DE GESTAO POS/ERP\n"));
        // ESC E 0 (fim negrito)
        ms.Write(new byte[] { 0x1B, 0x45, 0x00 });
        ms.Write(Encoding.Latin1.GetBytes("------------------------------------------\n"));
        ms.Write(Encoding.Latin1.GetBytes($"TESTE DE CONECTIVIDADE ESC/POS\n"));
        ms.Write(Encoding.Latin1.GetBytes($"Impressora: {impressora.Nome}\n"));
        ms.Write(Encoding.Latin1.GetBytes($"Destino: {impressora.EnderecoIp}:{impressora.Porta}\n"));
        ms.Write(Encoding.Latin1.GetBytes($"Largura: {impressora.LarguraColunas} colunas\n"));
        ms.Write(Encoding.Latin1.GetBytes($"Data/Hora: {_dateTimeService.FormatarDataHora(_dateTimeService.NowLocal)} (CAT)\n"));
        ms.Write(Encoding.Latin1.GetBytes("------------------------------------------\n"));
        ms.Write(Encoding.Latin1.GetBytes("Comunicacao de rede efetuada com sucesso!\n\n\n\n"));
        
        if (impressora.CortarPapel)
        {
            // GS V A 16
            ms.Write(new byte[] { 0x1D, 0x56, 0x41, 0x10 });
        }

        var dados = ms.ToArray();
        var enviado = await _impressaoService.EnviarParaImpressoraAsync(id, dados, TipoDocumentoImpressao.MovimentoCaixa);

        if (enviado)
        {
            TempData["Sucesso"] = $"Teste enviado com sucesso para '{impressora.Nome}' ({impressora.EnderecoIp}:{impressora.Porta}).";
        }
        else
        {
            TempData["Erro"] = $"Não foi possível comunicar com a impressora '{impressora.Nome}' ({impressora.EnderecoIp}:{impressora.Porta}). Verifique se o aparelho está ligado à rede.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("eliminar/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var impressora = await _context.Impressoras.FindAsync(id);
        if (impressora == null)
        {
            TempData["Erro"] = "Impressora não encontrada.";
            return RedirectToAction(nameof(Index));
        }

        impressora.EliminadoEm = _dateTimeService.NowUtc;
        impressora.EliminadoPor = User.Identity?.Name ?? "Sistema";

        await _context.SaveChangesAsync();

        await _auditoriaService.RegistarAsync(
            acao: "IMPRESSORA_ELIMINADA",
            entidade: "Impressoras",
            entidadeId: impressora.Id.ToString(),
            valoresDepois: new { impressora.Nome },
            motivo: $"Impressora '{impressora.Nome}' eliminada do sistema.");

        TempData["Sucesso"] = $"Impressora '{impressora.Nome}' eliminada com sucesso.";
        return RedirectToAction(nameof(Index));
    }
}
