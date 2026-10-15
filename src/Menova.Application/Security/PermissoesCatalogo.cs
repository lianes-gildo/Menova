namespace Menova.Application.Security;

public static class PerfisSistema
{
    public const string Administrador = "ADMIN";
    public const string Gerente = "GERENTE";
    public const string Caixa = "CAIXA";
    public const string Garcom = "GARCOM";
    public const string CozinhaBar = "COZINHA";
}

public static class PermissoesCatalogo
{
    // Painel
    public const string PainelVer = "painel.ver";

    // Mesas
    public const string MesasVer = "mesas.ver";
    public const string MesasGerir = "mesas.gerir";

    // Contas
    public const string ContasAbrir = "contas.abrir";
    public const string ContasVerTodas = "contas.ver_todas";

    // Itens e Pedidos
    public const string ItensAdicionar = "itens.adicionar";
    public const string ItensEnviar = "itens.enviar";
    public const string ItensAnular = "itens.anular";
    public const string DescontosAplicar = "descontos.aplicar";
    public const string PrecoAlterar = "preco.alterar";

    // Menu
    public const string MenuVer = "menu.ver";
    public const string MenuGerir = "menu.gerir";
    public const string ArtigosEsgotar = "artigos.esgotar";

    // Cozinha e Estações
    public const string CozinhaVer = "cozinha.ver";
    public const string CozinhaOperar = "cozinha.operar";

    // Caixa
    public const string CaixaAbrir = "caixa.abrir";
    public const string CaixaCobrar = "caixa.cobrar";
    public const string CaixaFechar = "caixa.fechar";
    public const string CaixaMovimentos = "caixa.movimentos";
    public const string CaixaVerTodas = "caixa.ver_todas";
    public const string CaixaReimprimir = "caixa.reimprimir";
    public const string ZVer = "z.ver";

    // Relatórios
    public const string RelatoriosVer = "relatorios.ver";
    public const string RelatoriosExportar = "relatorios.exportar";

    // Auditoria
    public const string AuditoriaVer = "auditoria.ver";
    public const string AuditoriaVerificarIntegridade = "auditoria.verificar_integridade";

    // Gestão e Sistema
    public const string UtilizadoresVer = "utilizadores.ver";
    public const string UtilizadoresGerir = "utilizadores.gerir";
    public const string PerfisGerir = "perfis.gerir";
    public const string DispositivosGerir = "dispositivos.gerir";
    public const string ImpressorasGerir = "impressoras.gerir";
    public const string DefinicoesGerir = "definicoes.gerir";
    public const string BackupsVer = "backups.ver";
    public const string BackupsExecutar = "backups.executar";
    public const string BackupsRestaurar = "backups.restaurar";

    public static IReadOnlyList<(string Codigo, string Modulo, string Descricao)> Todas =>
    [
        (PainelVer, "Painel", "Ver o painel geral de indicadores"),
        (MesasVer, "Mesas", "Ver mapa de mesas e estados"),
        (MesasGerir, "Mesas", "Adicionar, editar e eliminar mesas e zonas"),
        (ContasAbrir, "Contas", "Abrir novas contas em mesas ou balcão"),
        (ContasVerTodas, "Contas", "Ver todas as contas do restaurante"),
        (ItensAdicionar, "Pedidos", "Adicionar artigos a contas"),
        (ItensEnviar, "Pedidos", "Enviar pedidos para estações de preparação"),
        (ItensAnular, "Pedidos", "Anular itens já enviados (com motivo/PIN)"),
        (DescontosAplicar, "Pedidos", "Aplicar descontos em contas ou artigos"),
        (PrecoAlterar, "Pedidos", "Alterar preço unitário de artigos"),
        (MenuVer, "Menu", "Consultar categorias, artigos e opções"),
        (MenuGerir, "Menu", "Criar e editar artigos, categorias e opções"),
        (ArtigosEsgotar, "Menu", "Marcar artigo como esgotado manualmente (86)"),
        (CozinhaVer, "Cozinha", "Aceder ao ecrã KDS de preparação"),
        (CozinhaOperar, "Cozinha", "Alterar estado de preparação de tickets"),
        (CaixaAbrir, "Caixa", "Abrir sessão de caixa e registar fundo"),
        (CaixaCobrar, "Caixa", "Efetuar cobranças e receber pagamentos"),
        (CaixaFechar, "Caixa", "Fechar sessão de caixa e emitir talão Z"),
        (CaixaMovimentos, "Caixa", "Efetuar sangrias e suprimentos de caixa"),
        (CaixaVerTodas, "Caixa", "Ver sessões e movimentos de todas as caixas"),
        (CaixaReimprimir, "Caixa", "Reimprimir talões e recibos com marca de cópia"),
        (ZVer, "Caixa", "Consultar histórico de fechos Z"),
        (RelatoriosVer, "Relatórios", "Visualizar relatórios operacionais e financeiros"),
        (RelatoriosExportar, "Relatórios", "Exportar relatórios em PDF, Excel ou CSV"),
        (AuditoriaVer, "Auditoria", "Consultar registos de auditoria imutáveis"),
        (AuditoriaVerificarIntegridade, "Auditoria", "Executar verificação da cadeia de hash"),
        (UtilizadoresVer, "Utilizadores", "Listar funcionários e dados de acesso"),
        (UtilizadoresGerir, "Utilizadores", "Criar, editar e ativar/desativar funcionários"),
        (PerfisGerir, "Utilizadores", "Gerir perfis e matriz de permissões"),
        (DispositivosGerir, "Dispositivos", "Autorizar e revogar dispositivos móveis/terminais"),
        (ImpressorasGerir, "Impressoras", "Configurar impressoras térmicas e fila"),
        (DefinicoesGerir, "Definições", "Alterar dados do restaurante e limites"),
        (BackupsVer, "Backups", "Ver lista e estados dos backups automáticos"),
        (BackupsExecutar, "Backups", "Executar backup manual de emergência"),
        (BackupsRestaurar, "Backups", "Restaurar cópia de segurança com PIN de admin")
    ];
}
