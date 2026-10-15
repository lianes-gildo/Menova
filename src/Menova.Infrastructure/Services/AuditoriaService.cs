using System.Text.Json;
using Menova.Application.Interfaces;
using Menova.Domain.Auditoria;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Menova.Infrastructure.Services;

public class AuditoriaService : IAuditoriaService
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuditoriaService> _logger;
    private static readonly SemaphoreSlim _lock = new(1, 1);
    public const string GenesisHash = "0000000000000000000000000000000000000000000000000000000000000000";

    public AuditoriaService(
        IAppDbContext context,
        ICurrentUserService currentUserService,
        ILogger<AuditoriaService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task RegistarAsync(
        string acao,
        string entidade,
        string? entidadeId = null,
        object? valoresAntes = null,
        object? valoresDepois = null,
        string? motivo = null,
        Guid? caixaId = null,
        Guid? sessaoCaixaId = null,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var dataHora = DateTime.UtcNow;
            var jsonAntes = valoresAntes != null ? JsonSerializer.Serialize(valoresAntes) : null;
            var jsonDepois = valoresDepois != null ? JsonSerializer.Serialize(valoresDepois) : null;

            // Obter o último hash da cadeia
            var ultimoHash = await _context.Auditoria
                .OrderByDescending(a => a.Id)
                .Select(a => a.Hash)
                .FirstOrDefaultAsync(cancellationToken) ?? GenesisHash;

            var nomeUtilizador = _currentUserService.NomeUtilizador;
            var correlacaoId = _currentUserService.CorrelacaoId;

            var novoHash = RegistoAuditoria.CalcularHash(
                ultimoHash,
                dataHora,
                nomeUtilizador,
                acao,
                entidade,
                entidadeId,
                jsonAntes,
                jsonDepois,
                motivo,
                correlacaoId);

            var registo = new RegistoAuditoria
            {
                DataHora = dataHora,
                UtilizadorId = _currentUserService.UtilizadorId,
                NomeUtilizadorSnapshot = nomeUtilizador,
                Perfil = _currentUserService.PerfilCodigo,
                DispositivoId = _currentUserService.DispositivoId,
                Ip = _currentUserService.IpAddress,
                Acao = acao,
                Entidade = entidade,
                EntidadeId = entidadeId,
                CaixaId = caixaId,
                SessaoCaixaId = sessaoCaixaId,
                ValoresAntes = jsonAntes,
                ValoresDepois = jsonDepois,
                Motivo = motivo,
                CorrelacaoId = correlacaoId,
                HashAnterior = ultimoHash,
                Hash = novoHash
            };

            _context.Auditoria.Add(registo);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha crítica ao gravar registo de auditoria para ação {Acao} na entidade {Entidade}", acao, entidade);
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<VerificacaoIntegridadeResultado> VerificarIntegridadeAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var registos = await _context.Auditoria
                .OrderBy(a => a.Id)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            if (registos.Count == 0)
            {
                return new VerificacaoIntegridadeResultado(
                    Valido: true,
                    TotalRegistos: 0,
                    PrimeiroRegistoCorrompidoId: null,
                    Mensagem: "Cadeia de auditoria vazia. Sem registos.",
                    VerificadoEm: DateTime.UtcNow);
            }

            string hashEsperadoAnterior = GenesisHash;

            foreach (var registo in registos)
            {
                // 1. Validar encadeamento com o registo anterior
                if (!string.Equals(registo.HashAnterior, hashEsperadoAnterior, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Quebra na cadeia de auditoria no ID {Id}: HashAnterior ({HashAnterior}) não corresponde ao esperado ({Esperado})",
                        registo.Id, registo.HashAnterior, hashEsperadoAnterior);

                    return new VerificacaoIntegridadeResultado(
                        Valido: false,
                        TotalRegistos: registos.Count,
                        PrimeiroRegistoCorrompidoId: registo.Id,
                        Mensagem: $"Quebra de encadeamento no registo #{registo.Id}. A cadeia foi adulterada.",
                        VerificadoEm: DateTime.UtcNow);
                }

                // 2. Recalcular o hash do próprio registo
                var hashCalculado = RegistoAuditoria.CalcularHash(
                    registo.HashAnterior,
                    registo.DataHora,
                    registo.NomeUtilizadorSnapshot,
                    registo.Acao,
                    registo.Entidade,
                    registo.EntidadeId,
                    registo.ValoresAntes,
                    registo.ValoresDepois,
                    registo.Motivo,
                    registo.CorrelacaoId);

                if (!string.Equals(registo.Hash, hashCalculado, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Adulteração no conteúdo do registo de auditoria #{Id}: Hash armazenado ({Hash}) difere do recalculado ({Calculado})",
                        registo.Id, registo.Hash, hashCalculado);

                    return new VerificacaoIntegridadeResultado(
                        Valido: false,
                        TotalRegistos: registos.Count,
                        PrimeiroRegistoCorrompidoId: registo.Id,
                        Mensagem: $"Conteúdo adulterado no registo #{registo.Id}. Assinatura SHA-256 inválida.",
                        VerificadoEm: DateTime.UtcNow);
                }

                hashEsperadoAnterior = registo.Hash;
            }

            return new VerificacaoIntegridadeResultado(
                Valido: true,
                TotalRegistos: registos.Count,
                PrimeiroRegistoCorrompidoId: null,
                Mensagem: $"Cadeia íntegra. {registos.Count} registos verificados com sucesso.",
                VerificadoEm: DateTime.UtcNow);
        }
        finally
        {
            _lock.Release();
        }
    }
}
