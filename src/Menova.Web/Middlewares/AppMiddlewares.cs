namespace Menova.Web.Middlewares;

public class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate _next)
    {
        this._next = _next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var correlationId) || string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        context.Items["CorrelationId"] = correlationId.ToString();
        context.Response.Headers[HeaderName] = correlationId;

        await _next(context);
    }
}

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(self), geolocation=(), microphone=()";
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data: blob:; " +
                "font-src 'self'; " +
                "connect-src 'self' ws: wss:; " +
                "frame-ancestors 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self';";

            return Task.CompletedTask;
        });

        await _next(context);
    }
}

public class MustChangePasswordMiddleware
{
    private readonly RequestDelegate _next;

    public MustChangePasswordMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var deveAlterar = context.User.FindFirst("deve_alterar_palavra_passe")?.Value;
            if (string.Equals(deveAlterar, "true", StringComparison.OrdinalIgnoreCase))
            {
                var path = context.Request.Path.Value ?? string.Empty;
                var isAllowed = path.StartsWith("/auth/alterar-palavra-passe", StringComparison.OrdinalIgnoreCase) ||
                                path.StartsWith("/auth/logout", StringComparison.OrdinalIgnoreCase) ||
                                path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
                                path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
                                path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
                                path.StartsWith("/icons", StringComparison.OrdinalIgnoreCase) ||
                                path.StartsWith("/menova_logo", StringComparison.OrdinalIgnoreCase) ||
                                path.StartsWith("/default.jpg", StringComparison.OrdinalIgnoreCase) ||
                                path.StartsWith("/favicon.ico", StringComparison.OrdinalIgnoreCase) ||
                                path.StartsWith("/manifest.json", StringComparison.OrdinalIgnoreCase) ||
                                path.StartsWith("/sw.js", StringComparison.OrdinalIgnoreCase) ||
                                path.StartsWith("/offline.html", StringComparison.OrdinalIgnoreCase) ||
                                path.StartsWith("/app", StringComparison.OrdinalIgnoreCase) ||
                                path.StartsWith("/instalar-certificado", StringComparison.OrdinalIgnoreCase);

                if (!isAllowed)
                {
                    context.Response.Redirect("/auth/alterar-palavra-passe");
                    return;
                }
            }
        }

        await _next(context);
    }
}
