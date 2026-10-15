using System.Text;
using ClosedXML.Excel;
using Menova.Application.DTOs;
using Menova.Application.Interfaces;
using Menova.Domain.Caixa;
using Menova.Domain.Pedidos;
using Menova.Domain.Relatorios;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Menova.Infrastructure.Services;

public class RelatorioService : IRelatorioService
{
    private readonly IAppDbContext _context;

    static RelatorioService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public RelatorioService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<PainelRelatoriosDto> ObterPainelVendasAsync(DateTime inicio, DateTime fim, CancellationToken ct = default)
    {
        var contasFechadas = await _context.Contas
            .AsNoTracking()
            .Include(c => c.Pedidos).ThenInclude(p => p.Itens).ThenInclude(i => i.Artigo).ThenInclude(a => a.Categoria)
            .Where(c => c.Estado == EstadoConta.Fechada && c.FechadaEm >= inicio && c.FechadaEm <= fim)
            .ToListAsync(ct);

        var pagamentos = await _context.Pagamentos
            .AsNoTracking()
            .Where(p => p.CriadoEm >= inicio && p.CriadoEm <= fim)
            .ToListAsync(ct);

        var totalFaturado = pagamentos.Sum(p => p.Valor);
        var totalIva = contasFechadas.Sum(c => c.TotalIva);
        var totalContas = contasFechadas.Count;
        var ticketMedio = totalContas > 0 ? totalFaturado / totalContas : 0.00m;

        var todosItens = contasFechadas
            .SelectMany(c => c.Pedidos)
            .SelectMany(p => p.Itens)
            .Where(i => !i.Anulado)
            .ToList();

        var totalArtigosVendidos = todosItens.Sum(i => i.Quantidade);

        var kpis = new KpiVendasDto(
            totalFaturado,
            totalIva,
            totalContas,
            ticketMedio,
            totalArtigosVendidos
        );

        // 1. Vendas por Dia
        var vendasPorDia = contasFechadas
            .GroupBy(c => c.FechadaEm!.Value.Date)
            .OrderBy(g => g.Key)
            .Select(g => new VendaPorDiaDto(
                g.Key.ToString("dd/MM/yyyy"),
                g.Key,
                g.Sum(c => c.TotalLiquido),
                g.Count()
            ))
            .ToList();

        // 2. Vendas por Categoria
        var vendasPorCategoria = todosItens
            .GroupBy(i => i.Artigo?.Categoria?.Nome ?? "Sem Categoria")
            .Select(g =>
            {
                var totalCat = g.Sum(i => i.TotalLiquido);
                var pct = totalFaturado > 0 ? (totalCat / totalFaturado) * 100m : 0m;
                return new VendaPorCategoriaDto(
                    g.Key,
                    g.Sum(i => i.Quantidade),
                    totalCat,
                    Math.Round(pct, 1)
                );
            })
            .OrderByDescending(c => c.TotalLiquido)
            .ToList();

        // 3. Vendas por Método de Pagamento
        var vendasPorMetodo = pagamentos
            .GroupBy(p => p.Metodo)
            .Select(g =>
            {
                var totalMetodo = g.Sum(p => p.Valor);
                var pct = totalFaturado > 0 ? (totalMetodo / totalFaturado) * 100m : 0m;
                return new VendaPorMetodoDto(
                    TraduzirMetodo(g.Key),
                    g.Count(),
                    totalMetodo,
                    Math.Round(pct, 1)
                );
            })
            .OrderByDescending(m => m.TotalLiquido)
            .ToList();

        // 4. Vendas por Hora do Dia (0 a 23)
        var vendasPorHora = contasFechadas
            .GroupBy(c => c.FechadaEm!.Value.AddHours(2).Hour) // Ajuste fuso Maputo
            .Select(g => new VendaPorHoraDto(
                g.Key,
                $"{g.Key:D2}:00",
                g.Sum(c => c.TotalLiquido),
                g.Count()
            ))
            .OrderBy(h => h.Hora)
            .ToList();

        // 5. Top 10 Artigos
        var topArtigos = todosItens
            .GroupBy(i => new { i.ArtigoId, i.NomeArtigo, CategoriaNome = i.Artigo?.Categoria?.Nome ?? "Geral" })
            .Select(g =>
            {
                var qtd = g.Sum(i => i.Quantidade);
                var tot = g.Sum(i => i.TotalLiquido);
                var precoMedio = qtd > 0 ? tot / qtd : 0m;
                var pct = totalFaturado > 0 ? (tot / totalFaturado) * 100m : 0m;

                return new VendaPorArtigoDto(
                    g.Key.NomeArtigo,
                    g.Key.CategoriaNome,
                    qtd,
                    precoMedio,
                    tot,
                    Math.Round(pct, 1)
                );
            })
            .OrderByDescending(a => a.TotalLiquido)
            .Take(10)
            .ToList();

        return new PainelRelatoriosDto(
            inicio,
            fim,
            kpis,
            vendasPorDia,
            vendasPorCategoria,
            vendasPorMetodo,
            vendasPorHora,
            topArtigos
        );
    }

    public async Task<List<VendaPorArtigoDto>> ObterTopArtigosAsync(DateTime inicio, DateTime fim, int limite = 50, CancellationToken ct = default)
    {
        var itens = await _context.ItensPedido
            .AsNoTracking()
            .Include(i => i.Pedido).ThenInclude(p => p.Conta)
            .Include(i => i.Artigo).ThenInclude(a => a.Categoria)
            .Where(i => !i.Anulado && i.Pedido.Conta.Estado == EstadoConta.Fechada &&
                        i.Pedido.Conta.FechadaEm >= inicio && i.Pedido.Conta.FechadaEm <= fim)
            .ToListAsync(ct);

        var totalGlobal = itens.Sum(i => i.TotalLiquido);

        return itens
            .GroupBy(i => new { i.ArtigoId, i.NomeArtigo, Categoria = i.Artigo?.Categoria?.Nome ?? "Geral" })
            .Select(g =>
            {
                var qtd = g.Sum(i => i.Quantidade);
                var tot = g.Sum(i => i.TotalLiquido);
                var precoMedio = qtd > 0 ? tot / qtd : 0m;
                var pct = totalGlobal > 0 ? (tot / totalGlobal) * 100m : 0m;

                return new VendaPorArtigoDto(
                    g.Key.NomeArtigo,
                    g.Key.Categoria,
                    qtd,
                    precoMedio,
                    tot,
                    Math.Round(pct, 1)
                );
            })
            .OrderByDescending(a => a.TotalLiquido)
            .Take(limite)
            .ToList();
    }

    public async Task<List<ResumoIvaDto>> ObterResumoIvaAsync(DateTime inicio, DateTime fim, CancellationToken ct = default)
    {
        var contas = await _context.Contas
            .AsNoTracking()
            .Where(c => c.Estado == EstadoConta.Fechada && c.FechadaEm >= inicio && c.FechadaEm <= fim)
            .ToListAsync(ct);

        var totalIva = contas.Sum(c => c.TotalIva);
        var totalLiquido = contas.Sum(c => c.TotalLiquido);
        var baseTributavel = totalLiquido - totalIva;

        return new List<ResumoIvaDto>
        {
            new ResumoIvaDto(
                Taxa: "16% (Normal Moçambique)",
                BaseTributavel: baseTributavel,
                TotalIva: totalIva,
                TotalComIva: totalLiquido
            )
        };
    }

    public async Task<List<DesempenhoGarcomDto>> ObterDesempenhoGarconsAsync(DateTime inicio, DateTime fim, CancellationToken ct = default)
    {
        var pedidos = await _context.Pedidos
            .AsNoTracking()
            .Include(p => p.Conta)
            .Where(p => p.Conta.Estado == EstadoConta.Fechada && p.CriadoEm >= inicio && p.CriadoEm <= fim)
            .ToListAsync(ct);

        return pedidos
            .GroupBy(p => p.CriadoPor ?? "Balcão")
            .Select(g =>
            {
                var total = g.Sum(p => p.Conta.TotalLiquido);
                var qtd = g.Count();
                var ticketMedio = qtd > 0 ? total / qtd : 0m;

                return new DesempenhoGarcomDto(
                    g.Key,
                    qtd,
                    total,
                    ticketMedio
                );
            })
            .OrderByDescending(d => d.TotalFaturado)
            .ToList();
    }

    public async Task SincronizarFatoVendasDiariasAsync(DateOnly data, CancellationToken ct = default)
    {
        var inicio = data.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fim = data.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var itens = await _context.ItensPedido
            .AsNoTracking()
            .Include(i => i.Pedido).ThenInclude(p => p.Conta)
            .Include(i => i.Artigo).ThenInclude(a => a.Categoria)
            .Where(i => !i.Anulado && i.Pedido.Conta.Estado == EstadoConta.Fechada &&
                        i.Pedido.Conta.FechadaEm >= inicio && i.Pedido.Conta.FechadaEm <= fim)
            .ToListAsync(ct);

        // Remover consolidação anterior desse dia se existir
        var existentes = await _context.FatoVendasDiarias
            .Where(f => f.Data == data)
            .ToListAsync(ct);

        _context.FatoVendasDiarias.RemoveRange(existentes);

        var novosFatos = itens
            .GroupBy(i => new
            {
                i.ArtigoId,
                i.NomeArtigo,
                CategoriaId = i.Artigo?.CategoriaId ?? Guid.Empty,
                CategoriaNome = i.Artigo?.Categoria?.Nome ?? "Geral"
            })
            .Select(g => new FatoVendasDiarias
            {
                Data = data,
                ArtigoId = g.Key.ArtigoId,
                ArtigoNome = g.Key.NomeArtigo,
                CategoriaId = g.Key.CategoriaId,
                CategoriaNome = g.Key.CategoriaNome,
                QuantidadeVendida = g.Sum(i => i.Quantidade),
                TotalBruto = g.Sum(i => i.Subtotal),
                TotalDesconto = 0.00m,
                TotalIva = g.Sum(i => i.TotalIva),
                TotalLiquido = g.Sum(i => i.TotalLiquido),
                AtualizadoEm = DateTime.UtcNow
            })
            .ToList();

        _context.FatoVendasDiarias.AddRange(novosFatos);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<byte[]> ExportarCsvVendasAsync(DateTime inicio, DateTime fim, CancellationToken ct = default)
    {
        var topArtigos = await ObterTopArtigosAsync(inicio, fim, 1000, ct);

        var sb = new StringBuilder();
        // UTF-8 BOM para garantir acentos perfeitos no Microsoft Excel em Moçambique
        sb.Append('\uFEFF');
        sb.AppendLine("Artigo;Categoria;Quantidade Vendida;Preço Médio (MZN);Total Líquido (MZN);Peso (%)");

        foreach (var a in topArtigos)
        {
            sb.AppendLine($"\"{a.ArtigoNome}\";\"{a.CategoriaNome}\";{a.Quantidade:0.##};{a.PrecoMedio:F2};{a.TotalLiquido:F2};{a.Percentagem:F1}%");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportarExcelVendasAsync(DateTime inicio, DateTime fim, CancellationToken ct = default)
    {
        var painel = await ObterPainelVendasAsync(inicio, fim, ct);
        var resumoIva = await ObterResumoIvaAsync(inicio, fim, ct);

        using var workbook = new XLWorkbook();

        // 1. Aba Resumo Executivo
        var wsResumo = workbook.Worksheets.Add("Resumo de Vendas");
        wsResumo.Cell("A1").Value = "MENOVA RESTAURANTE - RELATÓRIO DE VENDAS";
        wsResumo.Cell("A1").Style.Font.Bold = true;
        wsResumo.Cell("A1").Style.Font.FontSize = 14;
        wsResumo.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#6E1A33");

        wsResumo.Cell("A2").Value = $"Período: {inicio.AddHours(2):dd/MM/yyyy HH:mm} até {fim.AddHours(2):dd/MM/yyyy HH:mm}";
        wsResumo.Cell("A2").Style.Font.Italic = true;

        wsResumo.Cell("A4").Value = "Indicador";
        wsResumo.Cell("B4").Value = "Valor";
        wsResumo.Range("A4:B4").Style.Font.Bold = true;
        wsResumo.Range("A4:B4").Style.Fill.BackgroundColor = XLColor.FromHtml("#6E1A33");
        wsResumo.Range("A4:B4").Style.Font.FontColor = XLColor.White;

        wsResumo.Cell("A5").Value = "Total Faturado Líquido";
        wsResumo.Cell("B5").Value = painel.Kpis.TotalFaturado;
        wsResumo.Cell("B5").Style.NumberFormat.Format = "#,##0.00 \"MT\"";

        wsResumo.Cell("A6").Value = "Total IVA Liquidado (16%)";
        wsResumo.Cell("B6").Value = painel.Kpis.TotalIva;
        wsResumo.Cell("B6").Style.NumberFormat.Format = "#,##0.00 \"MT\"";

        wsResumo.Cell("A7").Value = "Contas Liquidadas";
        wsResumo.Cell("B7").Value = painel.Kpis.TotalContasLiquidadas;

        wsResumo.Cell("A8").Value = "Ticket Médio por Conta";
        wsResumo.Cell("B8").Value = painel.Kpis.TicketMedio;
        wsResumo.Cell("B8").Style.NumberFormat.Format = "#,##0.00 \"MT\"";

        wsResumo.Cell("A9").Value = "Total de Artigos Vendidos";
        wsResumo.Cell("B9").Value = painel.Kpis.TotalArtigosVendidos;
        wsResumo.Cell("B9").Style.NumberFormat.Format = "#,##0.00";

        // 2. Aba Artigos Vendidos
        var wsArtigos = workbook.Worksheets.Add("Vendas por Artigo");
        wsArtigos.Cell("A1").Value = "Artigo";
        wsArtigos.Cell("B1").Value = "Categoria";
        wsArtigos.Cell("C1").Value = "Qtd. Vendida";
        wsArtigos.Cell("D1").Value = "Preço Médio";
        wsArtigos.Cell("E1").Value = "Total Faturado";
        wsArtigos.Cell("F1").Value = "% Total";
        wsArtigos.Range("A1:F1").Style.Font.Bold = true;
        wsArtigos.Range("A1:F1").Style.Fill.BackgroundColor = XLColor.FromHtml("#6E1A33");
        wsArtigos.Range("A1:F1").Style.Font.FontColor = XLColor.White;

        int rowArt = 2;
        foreach (var art in painel.TopArtigos)
        {
            wsArtigos.Cell(rowArt, 1).Value = art.ArtigoNome;
            wsArtigos.Cell(rowArt, 2).Value = art.CategoriaNome;
            wsArtigos.Cell(rowArt, 3).Value = art.Quantidade;
            wsArtigos.Cell(rowArt, 4).Value = art.PrecoMedio;
            wsArtigos.Cell(rowArt, 4).Style.NumberFormat.Format = "#,##0.00 \"MT\"";
            wsArtigos.Cell(rowArt, 5).Value = art.TotalLiquido;
            wsArtigos.Cell(rowArt, 5).Style.NumberFormat.Format = "#,##0.00 \"MT\"";
            wsArtigos.Cell(rowArt, 6).Value = art.Percentagem / 100m;
            wsArtigos.Cell(rowArt, 6).Style.NumberFormat.Format = "0.0%";
            rowArt++;
        }

        // 3. Aba Métodos de Pagamento
        var wsMetodos = workbook.Worksheets.Add("Meios de Pagamento");
        wsMetodos.Cell("A1").Value = "Forma de Pagamento";
        wsMetodos.Cell("B1").Value = "N.º Transações";
        wsMetodos.Cell("C1").Value = "Total Faturado";
        wsMetodos.Cell("D1").Value = "Percentagem";
        wsMetodos.Range("A1:D1").Style.Font.Bold = true;
        wsMetodos.Range("A1:D1").Style.Fill.BackgroundColor = XLColor.FromHtml("#6E1A33");
        wsMetodos.Range("A1:D1").Style.Font.FontColor = XLColor.White;

        int rowMet = 2;
        foreach (var met in painel.VendasPorMetodo)
        {
            wsMetodos.Cell(rowMet, 1).Value = met.Metodo;
            wsMetodos.Cell(rowMet, 2).Value = met.QuantidadeTransacoes;
            wsMetodos.Cell(rowMet, 3).Value = met.TotalLiquido;
            wsMetodos.Cell(rowMet, 3).Style.NumberFormat.Format = "#,##0.00 \"MT\"";
            wsMetodos.Cell(rowMet, 4).Value = met.Percentagem / 100m;
            wsMetodos.Cell(rowMet, 4).Style.NumberFormat.Format = "0.0%";
            rowMet++;
        }

        wsResumo.Columns().AdjustToContents();
        wsArtigos.Columns().AdjustToContents();
        wsMetodos.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportarPdfVendasAsync(DateTime inicio, DateTime fim, CancellationToken ct = default)
    {
        var painel = await ObterPainelVendasAsync(inicio, fim, ct);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                // Header
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("MENOVA RESTAURANTE").FontSize(16).Bold().FontColor("#6E1A33");
                            c.Item().Text("Relatório Financeiro e Operacional de Vendas").FontSize(11).FontColor(Colors.Grey.Darken2);
                            c.Item().Text($"Período: {inicio.AddHours(2):dd/MM/yyyy HH:mm} até {fim.AddHours(2):dd/MM/yyyy HH:mm}").FontSize(9).Italic();
                        });
                        row.ConstantItem(120).AlignRight().Column(c =>
                        {
                            c.Item().Text("Moçambique (MZN)").Bold().FontSize(9);
                            c.Item().Text("IVA: 16% Normal").FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                    });
                    col.Item().PaddingVertical(5).LineHorizontal(1).LineColor("#6E1A33");
                });

                // Content
                page.Content().Column(col =>
                {
                    // Cards de KPI
                    col.Item().PaddingVertical(10).Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                        {
                            c.Item().Text("Total Faturado").FontSize(8).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"{painel.Kpis.TotalFaturado:N2} MT").FontSize(14).Bold().FontColor("#6E1A33");
                        });
                        row.Spacing(10);
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                        {
                            c.Item().Text("Total IVA (16%)").FontSize(8).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"{painel.Kpis.TotalIva:N2} MT").FontSize(14).Bold().FontColor(Colors.Green.Darken2);
                        });
                        row.Spacing(10);
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                        {
                            c.Item().Text("Contas Pagas").FontSize(8).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"{painel.Kpis.TotalContasLiquidadas}").FontSize(14).Bold();
                        });
                        row.Spacing(10);
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                        {
                            c.Item().Text("Ticket Médio").FontSize(8).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"{painel.Kpis.TicketMedio:N2} MT").FontSize(14).Bold();
                        });
                    });

                    // Tabela de Top Artigos
                    col.Item().PaddingTop(15).Text("Principais Artigos Faturados").FontSize(12).Bold().FontColor("#6E1A33");
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background("#6E1A33").Padding(4).Text("Artigo").Bold().FontColor(Colors.White);
                            header.Cell().Background("#6E1A33").Padding(4).Text("Categoria").Bold().FontColor(Colors.White);
                            header.Cell().Background("#6E1A33").Padding(4).AlignRight().Text("Qtd").Bold().FontColor(Colors.White);
                            header.Cell().Background("#6E1A33").Padding(4).AlignRight().Text("Preço Médio").Bold().FontColor(Colors.White);
                            header.Cell().Background("#6E1A33").Padding(4).AlignRight().Text("Total (MT)").Bold().FontColor(Colors.White);
                        });

                        foreach (var a in painel.TopArtigos)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(a.ArtigoNome);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(a.CategoriaNome);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{a.Quantidade:0.##}");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{a.PrecoMedio:N2}");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{a.TotalLiquido:N2} MT").Bold();
                        }
                    });

                    // Tabela Meios de Pagamento
                    col.Item().PaddingTop(15).Text("Vendas por Forma de Pagamento").FontSize(12).Bold().FontColor("#6E1A33");
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Darken2).Padding(4).Text("Meio de Pagamento").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Grey.Darken2).Padding(4).AlignRight().Text("Transações").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Grey.Darken2).Padding(4).AlignRight().Text("Total (MT)").Bold().FontColor(Colors.White);
                            header.Cell().Background(Colors.Grey.Darken2).Padding(4).AlignRight().Text("% Total").Bold().FontColor(Colors.White);
                        });

                        foreach (var m in painel.VendasPorMetodo)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(m.Metodo);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{m.QuantidadeTransacoes}");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{m.TotalLiquido:N2} MT").Bold();
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{m.Percentagem:F1}%");
                        }
                    });
                });

                // Footer
                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                    col.Item().PaddingTop(3).Row(row =>
                    {
                        row.RelativeItem().Text("Processado por software certificado Menova POS").FontSize(8).FontColor(Colors.Grey.Medium);
                        row.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Página ");
                            x.CurrentPageNumber();
                            x.Span(" de ");
                            x.TotalPages();
                        });
                    });
                });
            });
        });

        return doc.GeneratePdf();
    }

    private static string TraduzirMetodo(MetodoPagamento metodo) => metodo switch
    {
        MetodoPagamento.Numerario => "Numerário",
        MetodoPagamento.MulticaixaPOS => "Multicaixa POS",
        MetodoPagamento.MPesaManual => "M-Pesa",
        MetodoPagamento.EMolaManual => "e-Mola",
        MetodoPagamento.Transferencia => "Transferência",
        MetodoPagamento.Cheque => "Cheque",
        _ => "Outro"
    };
}
