namespace Menova.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UtilizadorId { get; }
    string NomeUtilizador { get; }
    string? NomeCompleto { get; }
    string? PerfilCodigo { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    Guid? DispositivoId { get; }
    string CorrelacaoId { get; }
    bool EstaAutenticado { get; }
    bool TemPermissao(string permissaoCodigo);
}

public interface IDateTimeService
{
    DateTime NowUtc { get; }
    DateTime NowLocal { get; }
    TimeZoneInfo FusoHorario { get; }
    DateTime ParaHoraLocal(DateTime utcDateTime);
    DateTime ParaUtc(DateTime localDateTime);
    string FormatarDataHora(DateTime dateTime);
    string FormatarData(DateTime dateTime);
    string FormatarHora(DateTime dateTime);
    string FormatarMoeda(decimal valor);
}

public record VerificacaoIntegridadeResultado(
    bool Valido,
    long TotalRegistos,
    long? PrimeiroRegistoCorrompidoId,
    string Mensagem,
    DateTime VerificadoEm);

public interface IAuditoriaService
{
    Task RegistarAsync(
        string acao,
        string entidade,
        string? entidadeId = null,
        object? valoresAntes = null,
        object? valoresDepois = null,
        string? motivo = null,
        Guid? caixaId = null,
        Guid? sessaoCaixaId = null,
        CancellationToken cancellationToken = default);

    Task<VerificacaoIntegridadeResultado> VerificarIntegridadeAsync(CancellationToken cancellationToken = default);
}
