using Menova.Domain.Auditoria;
using Menova.Domain.Configuracao;
using Menova.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Menova.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<Utilizador> Utilizadores { get; }
    DbSet<Perfil> Perfis { get; }
    DbSet<Permissao> Permissoes { get; }
    DbSet<PerfilPermissao> PerfisPermissoes { get; }
    DbSet<UtilizadorPerfil> UtilizadoresPerfis { get; }
    DbSet<Dispositivo> Dispositivos { get; }
    DbSet<SessaoUtilizador> SessoesUtilizador { get; }
    DbSet<TentativaLogin> TentativasLogin { get; }
    DbSet<Restaurante> Restaurante { get; }
    DbSet<Configuracao> Configuracoes { get; }
    DbSet<Sequencia> Sequencias { get; }
    DbSet<RegistoAuditoria> Auditoria { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
