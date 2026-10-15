using Menova.Application.DTOs;

namespace Menova.Application.Interfaces;

public interface IBackupService
{
    Task<List<FicheiroBackupDto>> ListarBackupsAsync(CancellationToken ct = default);
    Task<CriarBackupResultadoDto> CriarBackupAsync(string? motivo = null, CancellationToken ct = default);
    Task<bool> VerificarIntegridadeBackupAsync(string nomeFicheiro, CancellationToken ct = default);
    Task<Stream?> ObterStreamBackupAsync(string nomeFicheiro, CancellationToken ct = default);
    Task<bool> EliminarBackupAsync(string nomeFicheiro, CancellationToken ct = default);
    Task RotacionarBackupsAsync(CancellationToken ct = default);
}
