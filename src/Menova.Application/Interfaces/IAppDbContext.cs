using Menova.Domain.Auditoria;
using Menova.Domain.Configuracao;
using Menova.Domain.Estacoes;
using Menova.Domain.Identity;
using Menova.Domain.Menu;
using Menova.Domain.Sala;
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

    // Sala e Mesas
    DbSet<Zona> Zonas { get; }
    DbSet<Mesa> Mesas { get; }
    DbSet<ContaMesa> ContasMesas { get; }

    // Estações de Preparação
    DbSet<EstacaoPreparacao> EstacoesPreparacao { get; }

    // Menu e Artigos
    DbSet<Categoria> Categorias { get; }
    DbSet<Artigo> Artigos { get; }
    DbSet<ArtigoCodigoBarras> ArtigosCodigosBarras { get; }
    DbSet<ArtigoImagem> ArtigosImagens { get; }
    DbSet<Alergenio> Alergenios { get; }
    DbSet<ArtigoAlergenio> ArtigosAlergenios { get; }
    DbSet<Etiqueta> Etiquetas { get; }
    DbSet<ArtigoEtiqueta> ArtigosEtiquetas { get; }
    DbSet<GrupoOpcoes> GruposOpcoes { get; }
    DbSet<Opcao> Opcoes { get; }
    DbSet<ArtigoGrupoOpcoes> ArtigosGruposOpcoes { get; }
    DbSet<MenuVersao> MenuVersao { get; }

    // Pedidos e Contas
    DbSet<Menova.Domain.Pedidos.Conta> Contas { get; }
    DbSet<Menova.Domain.Pedidos.Pedido> Pedidos { get; }
    DbSet<Menova.Domain.Pedidos.ItemPedido> ItensPedido { get; }
    DbSet<Menova.Domain.Pedidos.ItemPedidoOpcao> ItensPedidoOpcoes { get; }
    DbSet<Menova.Domain.Pedidos.ChaveIdempotencia> ChavesIdempotencia { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
