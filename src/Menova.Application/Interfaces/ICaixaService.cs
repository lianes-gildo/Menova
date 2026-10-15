using Menova.Application.DTOs;

namespace Menova.Application.Interfaces;

public interface ICaixaService
{
    Task<List<CaixaDto>> ObterCaixasAsync(CancellationToken ct = default);
    Task<SessaoCaixaDto?> ObterSessaoAtivaAsync(Guid utilizadorId, CancellationToken ct = default);
    Task<SessaoCaixaDto?> ObterSessaoPorIdAsync(Guid sessaoId, CancellationToken ct = default);
    Task<SessaoCaixaDto> AbrirSessaoAsync(AbrirCaixaDto dto, Guid utilizadorId, string utilizadorNome, CancellationToken ct = default);
    Task<SessaoCaixaDto> FecharSessaoAsync(FecharCaixaDto dto, string utilizadorNome, CancellationToken ct = default);
    Task<MovimentoCaixaDto> RegistarMovimentoAsync(MovimentoCaixaEntradaDto dto, string utilizadorNome, CancellationToken ct = default);
    Task<RelatorioXDto> GerarRelatorioXAsync(Guid sessaoId, CancellationToken ct = default);
}
