using System.Security.Claims;
using Menova.Application.Interfaces;

namespace Menova.Web.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UtilizadorId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public string NomeUtilizador =>
        _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value ?? "sistema";

    public string? NomeCompleto =>
        _httpContextAccessor.HttpContext?.User.FindFirst("nome_completo")?.Value;

    public string? PerfilCodigo =>
        _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;

    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent =>
        _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public Guid? DispositivoId
    {
        get
        {
            var header = _httpContextAccessor.HttpContext?.Request.Headers["X-Dispositivo-Id"].ToString();
            return Guid.TryParse(header, out var id) ? id : null;
        }
    }

    public string CorrelacaoId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return Guid.NewGuid().ToString("N");

            if (context.Items.TryGetValue("CorrelationId", out var item) && item is string str)
            {
                return str;
            }

            var id = Guid.NewGuid().ToString("N");
            context.Items["CorrelationId"] = id;
            return id;
        }
    }

    public bool EstaAutenticado =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public bool TemPermissao(string permissaoCodigo)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return false;

        return context.User.HasClaim("permissao", permissaoCodigo) ||
               context.User.IsInRole("ADMIN");
    }
}
