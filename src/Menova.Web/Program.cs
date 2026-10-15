using System.Globalization;
using FluentValidation;
using Menova.Application.DTOs;
using Menova.Application.Interfaces;
using Menova.Domain.Identity;
using Menova.Infrastructure;
using Menova.Infrastructure.Persistence;
using Menova.Web.Middlewares;
using Menova.Web.Security;
using Menova.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Menova POS")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/menova-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Serviços Web & MVC
builder.Services.AddControllersWithViews(options =>
{
    // Proteção Anti-CSRF automática em ações de mutação (POST, PUT, DELETE)
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

// 3. Validações FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<LoginDtoValidator>();

// 4. Camada de Infraestrutura (DbContext PostgreSQL, Identity, Auditoria, Serviços)
builder.Services.AddInfrastructure(builder.Configuration);

// 5. Utilizador Atual
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// 6. Autenticação por Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".Menova.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        options.LoginPath = "/auth/login";
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/erros/acesso-negado";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
    });

// 7. Autorização Menova por Políticas Granulares
builder.Services.AddMenovaAuthorization();

// 8. Localização: Forçar pt-PT e pt-MZ com Fuso de Moçambique
var supportedCultures = new[]
{
    new CultureInfo("pt-PT"),
    new CultureInfo("pt-MZ")
};

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("pt-PT");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

var app = builder.Build();

// 9. Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/erros/500");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/erros/{0}");

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseStaticFiles();

app.UseRequestLocalization();

app.UseRouting();

app.UseAuthentication();
app.UseMiddleware<MustChangePasswordMiddleware>();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllers();

// 10. Migração Automática e Seeding da Base de Dados
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher<Utilizador>>();

        // Aplicar migrações se aplicável
        if (context.Database.IsNpgsql() && context.Database.GetPendingMigrations().Any())
        {
            logger.LogInformation("A aplicar migrações pendentes do PostgreSQL...");
            await context.Database.MigrateAsync();
        }
        else if (context.Database.IsNpgsql())
        {
            await context.Database.EnsureCreatedAsync();
        }

        await DbInitializer.InicializarAsync(context, passwordHasher, logger);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Aviso no arranque da base de dados. Se o PostgreSQL ainda estiver a iniciar, as migrações serão reexecutadas.");
    }
}

try
{
    Log.Information("A iniciar o Menova POS (Moçambique - Africa/Maputo)...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Terminação inesperada da aplicação Menova.");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
