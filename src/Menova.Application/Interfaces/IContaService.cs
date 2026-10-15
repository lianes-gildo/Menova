using Menova.Application.DTOs;

namespace Menova.Application.Interfaces;

public interface IContaService
{
    Task<ContaDto> AbrirContaAsync(Guid? mesaId, string? observacoes = null, CancellationToken cancellationToken = default);
    Task<ContaDto?> GetContaPorIdAsync(Guid contaId, CancellationToken cancellationToken = default);
    Task<ContaDto?> GetContaAtivaPorMesaAsync(Guid mesaId, CancellationToken cancellationToken = default);
    Task<List<ContaDto>> ListarContasAbertasAsync(CancellationToken cancellationToken = default);
    Task AnularContaAsync(Guid contaId, string motivo, string? pinSupervisor = null, CancellationToken cancellationToken = default);
    Task RecalcularTotaisContaAsync(Guid contaId, CancellationToken cancellationToken = default);
}
