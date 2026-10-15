using Menova.Domain.Caixa;

namespace Menova.Application.DTOs;

public record AbrirCaixaDto(
    Guid CaixaId,
    decimal SaldoInicial,
    string? Observacoes);

public record FecharCaixaDto(
    Guid SessaoCaixaId,
    decimal SaldoContado,
    string? Observacoes);

public record MovimentoCaixaEntradaDto(
    Guid SessaoCaixaId,
    TipoMovimentoCaixa Tipo,
    decimal Valor,
    string Motivo,
    string? PinSupervisor);

public record PagamentoEntradaDto(
    Guid ContaId,
    Guid SessaoCaixaId,
    MetodoPagamento Metodo,
    decimal Valor,
    decimal ValorRecebido,
    decimal Gorjeta,
    string? Referencia);

public record PagamentoDto(
    Guid Id,
    Guid ContaId,
    Guid SessaoCaixaId,
    MetodoPagamento Metodo,
    string MetodoDescricao,
    decimal Valor,
    decimal ValorRecebido,
    decimal Troco,
    decimal Gorjeta,
    string? Referencia,
    DateTime CriadoEm,
    string CriadoPor);

public record MovimentoCaixaDto(
    Guid Id,
    Guid SessaoCaixaId,
    TipoMovimentoCaixa Tipo,
    string TipoDescricao,
    decimal Valor,
    string Motivo,
    DateTime CriadoEm,
    string CriadoPor);

public record SessaoCaixaDto(
    Guid Id,
    Guid CaixaId,
    string CaixaNome,
    int CaixaNumero,
    Guid UtilizadorId,
    string UtilizadorNome,
    DateTime AbertaEm,
    DateTime? FechadaEm,
    decimal SaldoInicial,
    decimal TotalVendas,
    decimal TotalEntradas,
    decimal TotalSaidas,
    decimal SaldoEsperado,
    decimal? SaldoContado,
    decimal? Diferenca,
    EstadoSessaoCaixa Estado,
    string EstadoDescricao,
    string? Observacoes,
    List<MovimentoCaixaDto> Movimentos,
    List<PagamentoDto> Pagamentos);

public record CaixaDto(
    Guid Id,
    string Nome,
    int Numero,
    bool Ativa,
    bool TemSessaoAberta,
    Guid? SessaoAtivaId,
    string? OperadorSessaoAtiva);

public record RelatorioXDto(
    Guid SessaoCaixaId,
    string CaixaNome,
    int CaixaNumero,
    string OperadorNome,
    DateTime AbertaEm,
    DateTime EmitidoEm,
    decimal SaldoInicial,
    decimal TotalVendas,
    decimal TotalEntradas,
    decimal TotalSaidas,
    decimal SaldoEsperado,
    int TotalContasPagas,
    Dictionary<string, decimal> VendasPorMetodo,
    Dictionary<string, decimal> TotaisPorIva);

public record DocumentoZDto(
    Guid Id,
    Guid SessaoCaixaId,
    int NumeroZ,
    DateTime DataHora,
    string CaixaNome,
    int CaixaNumero,
    string EmitidoPor,
    decimal TotalBruto,
    decimal TotalDesconto,
    decimal TotalIva,
    decimal TotalLiquido,
    int TotalDocumentosFaturados,
    Dictionary<string, decimal> TotaisPorMetodo,
    Dictionary<string, decimal> TotaisPorTaxaIva,
    string HashAnterior,
    string Hash,
    bool HashValido);
