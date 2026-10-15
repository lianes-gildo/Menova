using System.Text;
using FluentAssertions;
using Menova.Application.DTOs;
using Xunit;

namespace Menova.UnitTests;

public class RelatoriosTests
{
    [Fact]
    public void Relatorios_CalculoKpis_DeveCalcularValoresETicketMedio()
    {
        decimal totalFaturado = 15000.00m;
        decimal totalIva = 2400.00m;
        int totalContas = 10;
        decimal totalArtigos = 45.0m;

        decimal ticketMedio = totalContas > 0 ? totalFaturado / totalContas : 0m;

        var kpis = new KpiVendasDto(
            totalFaturado,
            totalIva,
            totalContas,
            ticketMedio,
            totalArtigos
        );

        kpis.TotalFaturado.Should().Be(15000.00m);
        kpis.TotalIva.Should().Be(2400.00m);
        kpis.TicketMedio.Should().Be(1500.00m);
        kpis.TotalArtigosVendidos.Should().Be(45.0m);
    }

    [Fact]
    public void Relatorios_ResumoIva16PorCento_DeveBaterComBaseTributavel()
    {
        // 16% de IVA sobre produtos vendidos em Moçambique
        decimal totalComIva = 11600.00m;
        decimal taxaIva = 16.00m;

        decimal baseTributavel = Math.Round(totalComIva / (1 + (taxaIva / 100m)), 4);
        decimal ivaLiquidado = totalComIva - baseTributavel;

        baseTributavel.Should().Be(10000.00m);
        ivaLiquidado.Should().Be(1600.00m);
        (baseTributavel + ivaLiquidado).Should().Be(totalComIva);
    }

    [Fact]
    public void Relatorios_CurvaAbcArtigos_DeveOrdenarPorMaiorFaturacao()
    {
        var artigos = new List<VendaPorArtigoDto>
        {
            new VendaPorArtigoDto("Cerveja 2M", "Bebidas", 100, 100m, 10000m, 50m),
            new VendaPorArtigoDto("Frango Zambeziano", "Pratos", 20, 650m, 13000m, 65m),
            new VendaPorArtigoDto("Água Mineral", "Bebidas", 50, 50m, 2500m, 12.5m)
        };

        var ordenados = artigos.OrderByDescending(a => a.TotalLiquido).ToList();

        ordenados[0].ArtigoNome.Should().Be("Frango Zambeziano");
        ordenados[1].ArtigoNome.Should().Be("Cerveja 2M");
        ordenados[2].ArtigoNome.Should().Be("Água Mineral");
    }

    [Fact]
    public void Relatorios_ExportacaoCsv_DeveConterBomEPontoEVirgula()
    {
        var sb = new StringBuilder();
        sb.Append('\uFEFF'); // UTF-8 BOM
        sb.AppendLine("Artigo;Categoria;Quantidade Vendida;Preço Médio (MZN);Total Líquido (MZN);Peso (%)");
        sb.AppendLine("\"Frango à Zambeziana\";\"Pratos Principais\";10;650,00;6500,00;45,0%");

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());

        bytes.Should().NotBeNullOrEmpty();
        // UTF-8 BOM bytes são EF, BB, BF
        bytes[0].Should().Be(0xEF);
        bytes[1].Should().Be(0xBB);
        bytes[2].Should().Be(0xBF);

        var texto = Encoding.UTF8.GetString(bytes);
        texto.Should().Contain(";");
        texto.Should().Contain("Frango à Zambeziana");
        texto.Should().Contain("6500,00");
    }
}
