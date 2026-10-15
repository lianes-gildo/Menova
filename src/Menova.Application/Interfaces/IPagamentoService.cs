using Menova.Application.DTOs;

namespace Menova.Application.Interfaces;

public interface IPagamentoService
{
    Task<PagamentoDto> RegistarPagamentoAsync(PagamentoEntradaDto dto, string utilizadorNome, string? idempotencyKey = null, CancellationToken ct = default);
    Task<List<PagamentoDto>> ObterPagamentosPorContaAsync(Guid contaId, CancellationToken ct = default);
}
