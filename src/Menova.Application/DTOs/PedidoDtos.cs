using Menova.Domain.Pedidos;

namespace Menova.Application.DTOs;

public record ItemPedidoEntradaDto(
    Guid ArtigoId,
    decimal Quantidade,
    string? Notas,
    List<Guid>? OpcoesIds);

public record CriarPedidoDto(
    Guid? ContaId,
    Guid? MesaId,
    string? Observacoes,
    List<ItemPedidoEntradaDto> Itens);

public record ItemPedidoDto(
    Guid Id,
    Guid ArtigoId,
    string NomeArtigo,
    decimal Quantidade,
    decimal PrecoUnitario,
    decimal TaxaIva,
    decimal Subtotal,
    decimal TotalIva,
    decimal TotalLiquido,
    Guid? EstacaoId,
    string? EstacaoNome,
    EstadoItemPedido Estado,
    string EstadoDescricao,
    string? Notas,
    bool Anulado,
    string? AnuladoMotivo,
    List<string> Opcoes);

public record PedidoDto(
    Guid Id,
    Guid ContaId,
    int NumeroSequencial,
    EstadoPedido Estado,
    string EstadoDescricao,
    string? Observacoes,
    DateTime CriadoEm,
    string CriadoPor,
    List<ItemPedidoDto> Itens);

public record ContaDto(
    Guid Id,
    string Numero,
    Guid? MesaId,
    string? MesaNumero,
    string? ZonaNome,
    EstadoConta Estado,
    string EstadoDescricao,
    decimal TotalBruto,
    decimal TotalDesconto,
    decimal TotalIva,
    decimal TotalLiquido,
    string? Observacoes,
    DateTime CriadoEm,
    string CriadoPor,
    List<PedidoDto> Pedidos);

public enum UrgenciaKds
{
    Normal = 1,   // < 10 min
    Atencao = 2,  // 10 - 20 min
    Urgente = 3   // > 20 min
}

public record ItemTicketKdsDto(
    Guid ItemId,
    string NomeArtigo,
    decimal Quantidade,
    string? Notas,
    List<string> Opcoes,
    EstadoItemPedido Estado,
    string? EstacaoNome);

public record TicketKdsDto(
    Guid PedidoId,
    Guid ContaId,
    string? MesaNumero,
    string? ZonaNome,
    int NumeroSequencial,
    DateTime CriadoEm,
    int MinutosDecorridos,
    UrgenciaKds Urgencia,
    string? Observacoes,
    List<ItemTicketKdsDto> Itens);

public record AnularItemDto(
    Guid ItemId,
    string Motivo,
    string? PinSupervisor);
