using Menova.Domain.Sala;

namespace Menova.Application.DTOs;

public record MesaDto(
    Guid Id,
    Guid ZonaId,
    string ZonaNome,
    string Numero,
    int Lugares,
    int PosicaoX,
    int PosicaoY,
    EstadoMesa Estado,
    string EstadoDescricao,
    bool Ativa,
    Guid? ContaAtivaId,
    decimal? TotalContaAtual,
    DateTime? AbertaEm);

public record ZonaDto(
    Guid Id,
    string Nome,
    int Ordem,
    bool Ativa,
    int TotalMesas,
    int MesasOcupadas,
    List<MesaDto> Mesas);

public record CriarMesaDto(
    Guid ZonaId,
    string Numero,
    int Lugares,
    int PosicaoX,
    int PosicaoY);

public record AtualizarMesaDto(
    Guid Id,
    Guid ZonaId,
    string Numero,
    int Lugares,
    int PosicaoX,
    int PosicaoY,
    bool Ativa);

public record AtualizarEstadoMesaDto(
    Guid MesaId,
    EstadoMesa NovoEstado,
    string? Motivo);

public record CriarZonaDto(
    string Nome,
    int Ordem);

public record AtualizarZonaDto(
    Guid Id,
    string Nome,
    int Ordem,
    bool Ativa);
