using Menova.Application.DTOs;
using Menova.Application.Interfaces;
using Menova.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Menova.Web.Controllers;

[Authorize(Policy = PermissoesCatalogo.RelatoriosVer)]
[Route("relatorios")]
public class RelatoriosController : Controller
{
    private readonly IRelatorioService _relatorioService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<RelatoriosController> _logger;

    public RelatoriosController(
        IRelatorioService relatorioService,
        IDateTimeService dateTimeService,
        ILogger<RelatoriosController> logger)
    {
        _relatorioService = relatorioService;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    private static (DateTime InicioUtc, DateTime FimUtc) NormalizarPeriodo(DateTime? inicio, DateTime? fim)
    {
        var fimUtc = fim.HasValue ? DateTime.SpecifyKind(fim.Value, DateTimeKind.Utc) : DateTime.UtcNow;
        var inicioUtc = inicio.HasValue ? DateTime.SpecifyKind(inicio.Value, DateTimeKind.Utc) : fimUtc.AddDays(-30);
        return (inicioUtc, fimUtc);
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(DateTime? inicio = null, DateTime? fim = null)
    {
        var (dataInicio, dataFim) = NormalizarPeriodo(inicio, fim);

        var painel = await _relatorioService.ObterPainelVendasAsync(dataInicio, dataFim);
        ViewBag.DateTimeService = _dateTimeService;
        ViewBag.Inicio = dataInicio;
        ViewBag.Fim = dataFim;

        return View(painel);
    }

    [HttpGet("artigos")]
    public async Task<IActionResult> Artigos(DateTime? inicio = null, DateTime? fim = null)
    {
        var (dataInicio, dataFim) = NormalizarPeriodo(inicio, fim);

        var topArtigos = await _relatorioService.ObterTopArtigosAsync(dataInicio, dataFim, 100);
        ViewBag.DateTimeService = _dateTimeService;
        ViewBag.Inicio = dataInicio;
        ViewBag.Fim = dataFim;

        return View(topArtigos);
    }

    [HttpGet("iva")]
    public async Task<IActionResult> Iva(DateTime? inicio = null, DateTime? fim = null)
    {
        var (dataInicio, dataFim) = NormalizarPeriodo(inicio, fim);

        var resumoIva = await _relatorioService.ObterResumoIvaAsync(dataInicio, dataFim);
        ViewBag.DateTimeService = _dateTimeService;
        ViewBag.Inicio = dataInicio;
        ViewBag.Fim = dataFim;

        return View(resumoIva);
    }

    [HttpGet("garcons")]
    public async Task<IActionResult> Garcons(DateTime? inicio = null, DateTime? fim = null)
    {
        var (dataInicio, dataFim) = NormalizarPeriodo(inicio, fim);

        var garcons = await _relatorioService.ObterDesempenhoGarconsAsync(dataInicio, dataFim);
        ViewBag.DateTimeService = _dateTimeService;
        ViewBag.Inicio = dataInicio;
        ViewBag.Fim = dataFim;

        return View(garcons);
    }

    [HttpGet("exportar/csv")]
    [Authorize(Policy = PermissoesCatalogo.RelatoriosExportar)]
    public async Task<IActionResult> ExportarCsv(DateTime? inicio = null, DateTime? fim = null)
    {
        var (dataInicio, dataFim) = NormalizarPeriodo(inicio, fim);

        var csvBytes = await _relatorioService.ExportarCsvVendasAsync(dataInicio, dataFim);
        var nomeFicheiro = $"menova_vendas_{dataInicio:yyyyMMdd}_{dataFim:yyyyMMdd}.csv";

        return File(csvBytes, "text/csv; charset=utf-8", nomeFicheiro);
    }

    [HttpGet("exportar/excel")]
    [Authorize(Policy = PermissoesCatalogo.RelatoriosExportar)]
    public async Task<IActionResult> ExportarExcel(DateTime? inicio = null, DateTime? fim = null)
    {
        var (dataInicio, dataFim) = NormalizarPeriodo(inicio, fim);

        var excelBytes = await _relatorioService.ExportarExcelVendasAsync(dataInicio, dataFim);
        var nomeFicheiro = $"menova_vendas_{dataInicio:yyyyMMdd}_{dataFim:yyyyMMdd}.xlsx";

        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nomeFicheiro);
    }

    [HttpGet("exportar/pdf")]
    [Authorize(Policy = PermissoesCatalogo.RelatoriosExportar)]
    public async Task<IActionResult> ExportarPdf(DateTime? inicio = null, DateTime? fim = null)
    {
        var (dataInicio, dataFim) = NormalizarPeriodo(inicio, fim);

        var pdfBytes = await _relatorioService.ExportarPdfVendasAsync(dataInicio, dataFim);
        var nomeFicheiro = $"menova_relatorio_{dataInicio:yyyyMMdd}_{dataFim:yyyyMMdd}.pdf";

        return File(pdfBytes, "application/pdf", nomeFicheiro);
    }
}
