using Menova.Application.Interfaces;
using Menova.Domain.Identity;
using Menova.Infrastructure.Persistence;
using Menova.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Menova.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=menova_db;Username=menova_app;Password=menova_secret;";

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
            });
        });

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        services.AddMemoryCache();

        // Serviços de Segurança e Domínio
        services.AddScoped<IPasswordHasher<Utilizador>, PasswordHasher<Utilizador>>();
        services.AddScoped<IPinService, PinService>();
        services.AddScoped<IAuditoriaService, AuditoriaService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<IDateTimeService, DateTimeService>();

        // Menu e Sala
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<ISalaService, SalaService>();

        // Pedidos, Contas e KDS
        services.AddScoped<IContaService, ContaService>();
        services.AddScoped<IPedidoService, PedidoService>();
        services.AddScoped<IKdsService, KdsService>();

        // Caixa, Pagamentos, Fecho Z e Impressão ESC/POS
        services.AddScoped<ICaixaService, CaixaService>();
        services.AddScoped<IPagamentoService, PagamentoService>();
        services.AddScoped<IFechoZService, FechoZService>();
        services.AddScoped<IImpressaoService, ImpressaoService>();
        services.AddSingleton<EscPosGenerator>();

        // Relatórios Analíticos e Exportações
        services.AddScoped<IRelatorioService, RelatorioService>();

        // Backups e Rotação
        services.AddScoped<IBackupService, BackupService>();

        return services;
    }
}
