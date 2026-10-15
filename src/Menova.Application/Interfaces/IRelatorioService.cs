using Menova.Application.DTOs;

namespace Menova.Application.Interfaces;

public interface IRelatorioService
{
    Task<PainelRelatoriosDto> ObterPainelVendasAsync(DateTime inicio, DateTime fim, CancellationToken ct = default);
    Task<List<VendaPorArtigoDto>> ObterTopArtigosAsync(DateTime inicio, DateTime fim, int limite = 50, CancellationToken ct = default);
    Task<List<ResumoIvaDto>> ObterResumoIvaAsync(DateTime inicio, DateTime fim, CancellationToken ct = default);
    Task<List<DesempenhoGarcomDto>> ObterDesempenhoGarconsAsync(DateTime inicio, DateTime fim, CancellationToken ct = default);
    Task SincronizarFatoVendasDiariasAsync(DateOnly data, CancellationToken ct = default);
    Task<byte[]> ExportarCsvVendasAsync(DateTime inicio, DateTime fim, CancellationToken ct = default);
    Task<byte[]> ExportarExcelVendasAsync(DateTime inicio, DateTime fim, CancellationToken ct = default);
    Task<byte[]> ExportarPdfVendasAsync(DateTime inicio, DateTime fim, CancellationToken ct = default);
}
