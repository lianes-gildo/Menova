namespace Menova.Application.DTOs;

public record FicheiroBackupDto(
    string NomeFicheiro,
    long TamanhoBytes,
    string TamanhoFormatado,
    DateTime CriadoEm,
    string HashSha256,
    bool HashValido);

public record CriarBackupResultadoDto(
    bool Sucesso,
    string NomeFicheiro,
    string Mensagem,
    string? HashSha256);
