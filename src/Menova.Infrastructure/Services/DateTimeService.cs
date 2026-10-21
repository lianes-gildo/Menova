using System.Globalization;
using Menova.Application.Interfaces;

namespace Menova.Infrastructure.Services;

public class DateTimeService : IDateTimeService
{
    private readonly TimeZoneInfo _fusoHorario;
    private readonly CultureInfo _cultura;

    public DateTimeService()
    {
        // Forçar sempre Moçambique (Africa/Maputo, UTC+2)
        try
        {
            _fusoHorario = TimeZoneInfo.FindSystemTimeZoneById("Africa/Maputo");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                // Fallback para ambientes Windows antigos onde o ID pode ser "South Africa Standard Time"
                _fusoHorario = TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time");
            }
            catch
            {
                _fusoHorario = TimeZoneInfo.CreateCustomTimeZone("Africa/Maputo", TimeSpan.FromHours(2), "Africa/Maputo", "UTC+2");
            }
        }

        // Cultura com padrão Moçambique (pt-MZ / pt-PT)
        var cult = new CultureInfo("pt-PT");
        cult.NumberFormat.CurrencySymbol = "MZN";
        cult.NumberFormat.CurrencyDecimalDigits = 2;
        _cultura = cult;
    }

    public DateTime NowUtc => DateTime.UtcNow;

    public DateTime NowLocal => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _fusoHorario);

    public TimeZoneInfo FusoHorario => _fusoHorario;

    public DateTime ParaHoraLocal(DateTime utcDateTime)
    {
        var utc = utcDateTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc)
            : utcDateTime.ToUniversalTime();

        return TimeZoneInfo.ConvertTimeFromUtc(utc, _fusoHorario);
    }

    public DateTime ParaUtc(DateTime localDateTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(localDateTime, _fusoHorario);
    }

    public string FormatarDataHora(DateTime dateTime)
    {
        var local = ParaHoraLocal(dateTime);
        return local.ToString("dd/MM/yyyy HH:mm:ss", _cultura);
    }

    public string FormatarData(DateTime dateTime)
    {
        var local = ParaHoraLocal(dateTime);
        return local.ToString("dd/MM/yyyy", _cultura);
    }

    public string FormatarHora(DateTime dateTime)
    {
        var local = ParaHoraLocal(dateTime);
        return local.ToString("HH:mm", _cultura);
    }

    public string FormatarMoeda(decimal valor)
    {
        return $"{valor.ToString("N2", _cultura)} MZN";
    }
}
