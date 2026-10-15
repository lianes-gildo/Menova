using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Menova.Application.DTOs;
using Menova.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Menova.Infrastructure.Services;

public class BackupService : IBackupService
{
    private readonly string _diretorioBackups;
    private readonly string _connectionString;
    private readonly IAuditoriaService _auditoriaService;
    private readonly ILogger<BackupService> _logger;

    public BackupService(
        IConfiguration configuration,
        IAuditoriaService auditoriaService,
        ILogger<BackupService> logger)
    {
        _auditoriaService = auditoriaService;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=menova_db;Username=menova_app;Password=menova_secret;";

        var configDir = configuration["Backups:Diretorio"];
        if (!string.IsNullOrWhiteSpace(configDir))
        {
            _diretorioBackups = configDir;
        }
        else
        {
            _diretorioBackups = Path.Combine(Directory.GetCurrentDirectory(), "backups");
        }

        if (!Directory.Exists(_diretorioBackups))
        {
            Directory.CreateDirectory(_diretorioBackups);
        }
    }

    public async Task<List<FicheiroBackupDto>> ListarBackupsAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(_diretorioBackups))
            return new List<FicheiroBackupDto>();

        var ficheiros = Directory.GetFiles(_diretorioBackups, "*.*")
            .Where(f => f.EndsWith(".dump", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".sql.gz", StringComparison.OrdinalIgnoreCase))
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTimeUtc)
            .ToList();

        var resultado = new List<FicheiroBackupDto>();

        foreach (var f in ficheiros)
        {
            var hashFicheiroPath = f.FullName + ".sha256";
            string hashArmazenado = "";
            bool hashValido = false;

            if (File.Exists(hashFicheiroPath))
            {
                hashArmazenado = (await File.ReadAllTextAsync(hashFicheiroPath, ct)).Trim();
                try
                {
                    using var stream = File.OpenRead(f.FullName);
                    var hashBytes = await SHA256.HashDataAsync(stream, ct);
                    var hashAtual = Convert.ToHexString(hashBytes).ToLowerInvariant();
                    hashValido = string.Equals(hashArmazenado, hashAtual, StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    hashValido = false;
                }
            }

            resultado.Add(new FicheiroBackupDto(
                NomeFicheiro: f.Name,
                TamanhoBytes: f.Length,
                TamanhoFormatado: FormatarTamanho(f.Length),
                CriadoEm: f.CreationTimeUtc,
                HashSha256: hashArmazenado,
                HashValido: hashValido
            ));
        }

        return resultado;
    }

    public async Task<CriarBackupResultadoDto> CriarBackupAsync(string? motivo = null, CancellationToken ct = default)
    {
        var carimbo = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var nomeBase = $"menova_backup_{carimbo}";
        var caminhoCompleto = Path.Combine(_diretorioBackups, $"{nomeBase}.sql.gz");

        try
        {
            var builder = new NpgsqlConnectionStringBuilder(_connectionString);
            bool executouPgDump = false;

            // 1. Tentar executar pg_dump nativo se existir
            try
            {
                var caminhoDump = Path.Combine(_diretorioBackups, $"{nomeBase}.dump");
                var psi = new ProcessStartInfo
                {
                    FileName = "pg_dump",
                    Arguments = $"-h {builder.Host} -p {builder.Port} -U {builder.Username} -F c -b -v -f \"{caminhoDump}\" {builder.Database}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                psi.EnvironmentVariables["PGPASSWORD"] = builder.Password;

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    var timeoutTask = Task.Delay(120_000, ct);
                    var waitTask = proc.WaitForExitAsync(ct);

                    if (await Task.WhenAny(waitTask, timeoutTask) == waitTask && proc.ExitCode == 0)
                    {
                        executouPgDump = true;
                        caminhoCompleto = caminhoDump;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation("pg_dump nativo indisponível ou falhou. Utilizando exportador SQL nativo do Menova: {Msg}", ex.Message);
            }

            // 2. Se pg_dump não foi executado, efetuar exportação SQL direta e comprimida
            if (!executouPgDump)
            {
                caminhoCompleto = Path.Combine(_diretorioBackups, $"{nomeBase}.sql.gz");
                await using var fs = new FileStream(caminhoCompleto, FileMode.Create, FileAccess.Write, FileShare.None);
                await using var gzip = new GZipStream(fs, CompressionLevel.Optimal);
                await using var sw = new StreamWriter(gzip, Encoding.UTF8);

                await sw.WriteLineAsync($"-- MENOVA RESTAURANTE - CÓPIA DE SEGURANÇA");
                await sw.WriteLineAsync($"-- Data: {DateTime.UtcNow:O} (UTC)");
                await sw.WriteLineAsync($"-- Base de Dados: {builder.Database}");
                await sw.WriteLineAsync();

                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync(ct);

                // Listar tabelas operacionais
                var tabelas = new List<string>();
                await using var cmdTabelas = new NpgsqlCommand(
                    "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE' ORDER BY table_name;", conn);
                await using var readerTabelas = await cmdTabelas.ExecuteReaderAsync(ct);
                while (await readerTabelas.ReadAsync(ct))
                {
                    tabelas.Add(readerTabelas.GetString(0));
                }
                await readerTabelas.CloseAsync();

                foreach (var tab in tabelas)
                {
                    await sw.WriteLineAsync($"-- TABELA: {tab}");
                    await using var cmdSelect = new NpgsqlCommand($"SELECT * FROM \"{tab}\"", conn);
                    await using var rdr = await cmdSelect.ExecuteReaderAsync(ct);

                    while (await rdr.ReadAsync(ct))
                    {
                        var colunas = new List<string>();
                        var valores = new List<string>();

                        for (int i = 0; i < rdr.FieldCount; i++)
                        {
                            colunas.Add($"\"{rdr.GetName(i)}\"");
                            if (rdr.IsDBNull(i))
                            {
                                valores.Add("NULL");
                            }
                            else
                            {
                                var val = rdr.GetValue(i);
                                if (val is bool b)
                                    valores.Add(b ? "TRUE" : "FALSE");
                                else if (val is byte[] bytes)
                                    valores.Add($"decode('{Convert.ToHexString(bytes)}', 'hex')");
                                else if (val is DateTime dt)
                                    valores.Add($"'{dt:yyyy-MM-dd HH:mm:ss.ffffff}+00'");
                                else
                                    valores.Add($"'{val.ToString()?.Replace("'", "''")}'");
                            }
                        }

                        await sw.WriteLineAsync($"INSERT INTO \"{tab}\" ({string.Join(", ", colunas)}) VALUES ({string.Join(", ", valores)}) ON CONFLICT DO NOTHING;");
                    }
                    await rdr.CloseAsync();
                    await sw.WriteLineAsync();
                }

                await sw.FlushAsync();
            }

            // 3. Calcular Hash SHA-256 da cópia
            string hashCalculado;
            await using (var fileStream = File.OpenRead(caminhoCompleto))
            {
                var hashBytes = await SHA256.HashDataAsync(fileStream, ct);
                hashCalculado = Convert.ToHexString(hashBytes).ToLowerInvariant();
            }

            // 4. Salvar arquivo .sha256
            var caminhoSha = caminhoCompleto + ".sha256";
            await File.WriteAllTextAsync(caminhoSha, hashCalculado, ct);

            var fileInfo = new FileInfo(caminhoCompleto);
            var nomeFicheiro = fileInfo.Name;

            // 5. Registar em Auditoria
            await _auditoriaService.RegistarAsync(
                acao: "BACKUP.CRIAR",
                entidade: "backups",
                entidadeId: nomeFicheiro,
                valoresDepois: new { Nome = nomeFicheiro, Tamanho = fileInfo.Length, Hash = hashCalculado },
                motivo: motivo ?? "Cópia de segurança gerada pelo utilizador",
                cancellationToken: ct);

            // 6. Rotação automática de backups antigos
            await RotacionarBackupsAsync(ct);

            return new CriarBackupResultadoDto(
                Sucesso: true,
                NomeFicheiro: nomeFicheiro,
                Mensagem: $"Cópia de segurança {nomeFicheiro} gerada com sucesso ({FormatarTamanho(fileInfo.Length)}).",
                HashSha256: hashCalculado
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar cópia de segurança");
            return new CriarBackupResultadoDto(
                Sucesso: false,
                NomeFicheiro: string.Empty,
                Mensagem: $"Erro ao gerar cópia de segurança: {ex.Message}",
                HashSha256: null
            );
        }
    }

    public async Task<bool> VerificarIntegridadeBackupAsync(string nomeFicheiro, CancellationToken ct = default)
    {
        var caminhoCompleto = Path.Combine(_diretorioBackups, Path.GetFileName(nomeFicheiro));
        var caminhoSha = caminhoCompleto + ".sha256";

        if (!File.Exists(caminhoCompleto) || !File.Exists(caminhoSha))
            return false;

        var hashArmazenado = (await File.ReadAllTextAsync(caminhoSha, ct)).Trim();
        await using var stream = File.OpenRead(caminhoCompleto);
        var hashBytes = await SHA256.HashDataAsync(stream, ct);
        var hashAtual = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hashArmazenado),
            Encoding.UTF8.GetBytes(hashAtual));
    }

    public Task<Stream?> ObterStreamBackupAsync(string nomeFicheiro, CancellationToken ct = default)
    {
        var caminho = Path.Combine(_diretorioBackups, Path.GetFileName(nomeFicheiro));
        if (!File.Exists(caminho))
            return Task.FromResult<Stream?>(null);

        var stream = new FileStream(caminho, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> EliminarBackupAsync(string nomeFicheiro, CancellationToken ct = default)
    {
        var caminho = Path.Combine(_diretorioBackups, Path.GetFileName(nomeFicheiro));
        var caminhoSha = caminho + ".sha256";

        if (File.Exists(caminho)) File.Delete(caminho);
        if (File.Exists(caminhoSha)) File.Delete(caminhoSha);

        return Task.FromResult(true);
    }

    public Task RotacionarBackupsAsync(CancellationToken ct = default)
    {
        // Rotação: manter últimos 7 diários, 4 semanais, 12 mensais
        try
        {
            var ficheiros = Directory.GetFiles(_diretorioBackups, "*.*")
                .Where(f => f.EndsWith(".dump") || f.EndsWith(".sql.gz"))
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTimeUtc)
                .ToList();

            // Mantém os 25 backups mais recentes e elimina os excedentes com mais de 30 dias
            var paraEliminar = ficheiros.Skip(25).Where(f => f.CreationTimeUtc < DateTime.UtcNow.AddDays(-30)).ToList();
            foreach (var f in paraEliminar)
            {
                f.Delete();
                var sha = f.FullName + ".sha256";
                if (File.Exists(sha)) File.Delete(sha);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao rotacionar backups antigos");
        }

        return Task.CompletedTask;
    }

    private static string FormatarTamanho(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F2} MB";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }
}
