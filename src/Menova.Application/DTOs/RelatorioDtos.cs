namespace Menova.Application.DTOs;

public record KpiVendasDto(
    decimal TotalFaturado,
    decimal TotalIva,
    int TotalContasLiquidadas,
    decimal TicketMedio,
    decimal TotalArtigosVendidos);

public record VendaPorDiaDto(
    string DataFormatada,
    DateTime Data,
    decimal TotalLiquido,
    int TotalContas);

public record VendaPorCategoriaDto(
    string CategoriaNome,
    decimal Quantidade,
    decimal TotalLiquido,
    decimal Percentagem);

public record VendaPorArtigoDto(
    string ArtigoNome,
    string CategoriaNome,
    decimal Quantidade,
    decimal PrecoMedio,
    decimal TotalLiquido,
    decimal Percentagem);

public record VendaPorMetodoDto(
    string Metodo,
    int QuantidadeTransacoes,
    decimal TotalLiquido,
    decimal Percentagem);

public record VendaPorHoraDto(
    int Hora,
    string HoraFormatada,
    decimal TotalLiquido,
    int TotalPedidos);

public record ResumoIvaDto(
    string Taxa,
    decimal BaseTributavel,
    decimal TotalIva,
    decimal TotalComIva);

public record DesempenhoGarcomDto(
    string FuncionarioNome,
    int TotalPedidos,
    decimal TotalFaturado,
    decimal TicketMedio);

public record PainelRelatoriosDto(
    DateTime PeriodoInicio,
    DateTime PeriodoFim,
    KpiVendasDto Kpis,
    List<VendaPorDiaDto> VendasPorDia,
    List<VendaPorCategoriaDto> VendasPorCategoria,
    List<VendaPorMetodoDto> VendasPorMetodo,
    List<VendaPorHoraDto> VendasPorHora,
    List<VendaPorArtigoDto> TopArtigos);
