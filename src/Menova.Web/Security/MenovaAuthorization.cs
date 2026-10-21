using System.Security.Claims;
using Menova.Application.Interfaces;
using Menova.Application.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Menova.Web.Security;

public class MenovaClaimsTransformation : IClaimsTransformation
{
    private readonly IAppDbContext _context;
    private readonly IMemoryCache _cache;

    public MenovaClaimsTransformation(IAppDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return principal;
        }

        var userIdStr = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return principal;
        }

        // 1. Sincronizar Perfis / Roles atribuídos ao utilizador
        var cacheRolesKey = $"user_roles_{userId}";
        if (!_cache.TryGetValue<List<string>>(cacheRolesKey, out var roles))
        {
            roles = await _context.UtilizadoresPerfis
                .Where(up => up.UtilizadorId == userId && up.Perfil.Ativo)
                .Select(up => up.Perfil.Codigo)
                .Distinct()
                .ToListAsync();

            _cache.Set(cacheRolesKey, roles, TimeSpan.FromMinutes(10));
        }

        if (roles != null)
        {
            foreach (var role in roles)
            {
                if (!identity.HasClaim(ClaimTypes.Role, role))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
                }
                if (!identity.HasClaim("perfil", role))
                {
                    identity.AddClaim(new Claim("perfil", role));
                }
            }
        }

        // 2. Sincronizar Permissões Granulares
        var cacheKey = $"user_permissions_{userId}";
        if (!_cache.TryGetValue<List<string>>(cacheKey, out var permissoes))
        {
            permissoes = await _context.UtilizadoresPerfis
                .Where(up => up.UtilizadorId == userId && up.Perfil.Ativo)
                .SelectMany(up => up.Perfil.PerfisPermissoes.Select(pp => pp.PermissaoCodigo))
                .Distinct()
                .ToListAsync();

            _cache.Set(cacheKey, permissoes, TimeSpan.FromMinutes(10));
        }

        if (permissoes != null)
        {
            foreach (var perm in permissoes)
            {
                if (!identity.HasClaim("permissao", perm))
                {
                    identity.AddClaim(new Claim("permissao", perm));
                }
            }
        }

        return principal;
    }
}

public static class MenovaAuthorizationExtensions
{
    public static IServiceCollection AddMenovaAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            foreach (var (codigo, _, _) in PermissoesCatalogo.Todas)
            {
                options.AddPolicy(codigo, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireAssertion(ctx =>
                        ctx.User.IsInRole(PerfisSistema.Administrador) ||
                        ctx.User.HasClaim("permissao", codigo));
                });
            }
        });

        services.AddScoped<IClaimsTransformation, MenovaClaimsTransformation>();

        return services;
    }
}
