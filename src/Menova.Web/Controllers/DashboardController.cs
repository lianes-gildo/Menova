using Menova.Application.Interfaces;
using Menova.Application.Security;
using Menova.Domain.Caixa;
using Menova.Domain.Pedidos;
using Menova.Domain.Sala;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Menova.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IAppDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly ICurrentUserService _currentUser;

    public DashboardController(
        IAppDbContext context,
        IDateTimeService dateTimeService,
        ICurrentUserService currentUser)
    {
        _context = context;
        _dateTimeService = dateTimeService;
        _currentUser = currentUser;
    }

    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Index()
    {
        // 1. Redirecionamento inteligente para perfis estritamente operacionais
        if (User.IsInRole(PerfisSistema.Garcom) && !User.IsInRole(PerfisSistema.Administrador) && !User.IsInRole(PerfisSistema.Gerente))
        {
            return RedirectToAction("Index", "Garcom");
        }

        if (User.IsInRole(PerfisSistema.CozinhaBar) && !User.IsInRole(PerfisSistema.Administrador) && !User.IsInRole(PerfisSistema.Gerente))
        {
            return RedirectToAction("Index", "Kds");
        }

        if (User.IsInRole(PerfisSistema.Caixa) && !User.IsInRole(PerfisSistema.Administrador) && !User.IsInRole(PerfisSistema.Gerente))
        {
            return RedirectToAction("Index", "Caixa");
        }

        // 2. Proteção de Painel Executivo / Financeiro: utilizadores sem PainelVer são direcionados ao seu posto
        if (!_currentUser.TemPermissao(PermissoesCatalogo.PainelVer))
        {
            if (_currentUser.TemPermissao(PermissoesCatalogo.ContasAbrir))
            {
                return RedirectToAction("Index", "Garcom");
            }
            if (_currentUser.TemPermissao(PermissoesCatalogo.CaixaAbrir) || _currentUser.TemPermissao(PermissoesCatalogo.CaixaCobrar))
            {
                return RedirectToAction("Index", "Caixa");
            }
            if (_currentUser.TemPermissao(PermissoesCatalogo.CozinhaVer))
            {
                return RedirectToAction("Index", "Kds");
            }

            return RedirectToAction("AcessoNegado", "Erros");
        }

        var restaurante = await _context.Restaurante.FirstOrDefaultAsync();

        // 3. Cálculos Reais de Estatísticas do Restaurante
        var agoraLocal = _dateTimeService.NowLocal;
        var inicioHojeLocal = agoraLocal.Date;
        var inicioHojeUtc = DateTime.SpecifyKind(inicioHojeLocal.AddHours(-2), DateTimeKind.Utc); // Horário de Moçambique UTC+2 convertido para UTC
        var fimHojeUtc = DateTime.SpecifyKind(inicioHojeUtc.AddDays(1), DateTimeKind.Utc);

        var totalVendasHoje = await _context.Pagamentos
            .Where(p => p.CriadoEm >= inicioHojeUtc && p.CriadoEm < fimHojeUtc)
            .SumAsync(p => (decimal?)p.Valor) ?? 0.00m;

        var totalContasPagasHoje = await _context.Contas
            .Where(c => c.Estado == EstadoConta.Fechada && c.FechadaEm >= inicioHojeUtc && c.FechadaEm < fimHojeUtc)
            .CountAsync();

        var totalMesasOcupadas = await _context.Mesas.CountAsync(m => m.Estado == EstadoMesa.Ocupada);
        var totalMesasGeral = await _context.Mesas.CountAsync(m => m.Ativa);

        var pedidosEmPreparacao = await _context.ItensPedido
            .CountAsync(i => (i.Estado == EstadoItemPedido.Pendente || i.Estado == EstadoItemPedido.EmPreparacao) && !i.Anulado);

        var totalComandasAbertas = await _context.Contas
            .CountAsync(c => c.Estado == EstadoConta.Aberta);

        var totalArtigosMenu = await _context.Artigos
            .CountAsync(a => a.Ativo);

        var totalUtilizadoresAtivos = await _context.Utilizadores
            .CountAsync(u => u.Ativo);

        var sessoesAbertas = await _context.SessoesCaixa
            .CountAsync(s => s.Estado == EstadoSessaoCaixa.Aberta);

        // 4. Últimas Comandas Ativas Reais
        var ultimasComandas = await _context.Contas
            .AsNoTracking()
            .Include(c => c.Mesa)
            .Where(c => c.Estado == EstadoConta.Aberta)
            .OrderByDescending(c => c.CriadoEm)
            .Take(5)
            .Select(c => new
            {
                c.Id,
                c.Numero,
                MesaNumero = c.Mesa != null ? c.Mesa.Numero : "Balcão",
                c.TotalLiquido,
                c.CriadoEm,
                c.CriadoPor
            })
            .ToListAsync();

        ViewBag.RestauranteNome = restaurante?.Nome ?? "Menova Restaurante";
        ViewBag.RestauranteNuit = restaurante?.Nuit ?? "100000000";
        ViewBag.Moeda = restaurante?.Moeda ?? "MZN";
        ViewBag.HoraLocal = _dateTimeService.FormatarDataHora(DateTime.UtcNow);

        ViewBag.VendasHoje = totalVendasHoje;
        ViewBag.ContasPagasHoje = totalContasPagasHoje;
        ViewBag.MesasOcupadas = totalMesasOcupadas;
        ViewBag.TotalMesas = totalMesasGeral;
        ViewBag.PedidosCozinha = pedidosEmPreparacao;
        ViewBag.ComandasAbertas = totalComandasAbertas;
        ViewBag.TotalArtigos = totalArtigosMenu;
        ViewBag.TotalUtilizadores = totalUtilizadoresAtivos;
        ViewBag.SessoesCaixaAbertas = sessoesAbertas;
        ViewBag.UltimasComandas = ultimasComandas;

        return View();
    }
}
