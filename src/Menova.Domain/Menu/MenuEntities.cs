using Menova.Domain.Common;
using Menova.Domain.Estacoes;

namespace Menova.Domain.Menu;

public class Categoria : SoftDeletableEntity
{
    public string Nome { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public Categoria? Parent { get; set; }
    public string? Icone { get; set; }
    public int Ordem { get; set; } = 0;
    public Guid? EstacaoPadraoId { get; set; }
    public EstacaoPreparacao? EstacaoPadrao { get; set; }
    public bool Ativa { get; set; } = true;

    public ICollection<Categoria> Subcategorias { get; set; } = new List<Categoria>();
    public ICollection<Artigo> Artigos { get; set; } = new List<Artigo>();
}

public class Artigo : SoftDeletableEntity
{
    public Guid CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;

    public string CodigoInterno { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? DescricaoCurta { get; set; }
    public string? DescricaoLonga { get; set; }
    public decimal Preco { get; set; } = 0.00m;
    public decimal TaxaIva { get; set; } = 16.00m; // Padrão Moçambique: 16%

    // Estação específica caso divirja da estação padrão da categoria
    public Guid? EstacaoId { get; set; }
    public EstacaoPreparacao? Estacao { get; set; }

    public int TempoPreparacaoMin { get; set; } = 15;
    public bool Destaque { get; set; } = false; // Sugestão do Chefe

    // Regra 86: Esgotado manual (ligado/desligado com um toque)
    public bool EsgotadoManual { get; set; } = false;
    public bool Disponivel { get; set; } = true;

    public int Ordem { get; set; } = 0;
    public bool Ativo { get; set; } = true;

    public ICollection<ArtigoCodigoBarras> CodigosBarras { get; set; } = new List<ArtigoCodigoBarras>();
    public ICollection<ArtigoImagem> Imagens { get; set; } = new List<ArtigoImagem>();
    public ICollection<ArtigoAlergenio> ArtigosAlergenios { get; set; } = new List<ArtigoAlergenio>();
    public ICollection<ArtigoEtiqueta> ArtigosEtiquetas { get; set; } = new List<ArtigoEtiqueta>();
    public ICollection<ArtigoGrupoOpcoes> ArtigosGruposOpcoes { get; set; } = new List<ArtigoGrupoOpcoes>();

    public bool EstaDisponivelParaVenda => Ativo && Disponivel && !EsgotadoManual && !EstaEliminado;
}

public class ArtigoCodigoBarras : BaseEntity
{
    public Guid ArtigoId { get; set; }
    public Artigo Artigo { get; set; } = null!;

    public string Codigo { get; set; } = string.Empty;
    public string Tipo { get; set; } = "EAN13"; // EAN13 ou Interno
}

public class ArtigoImagem : BaseEntity
{
    public Guid ArtigoId { get; set; }
    public Artigo Artigo { get; set; } = null!;

    public string CaminhoBase { get; set; } = string.Empty;
    public int Ordem { get; set; } = 0;
    public bool Principal { get; set; } = false;
    public int Largura { get; set; }
    public int Altura { get; set; }
    public string Hash { get; set; } = string.Empty;
}

public class Alergenio : BaseEntity
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Icone { get; set; }

    public ICollection<ArtigoAlergenio> ArtigosAlergenios { get; set; } = new List<ArtigoAlergenio>();
}

public class ArtigoAlergenio
{
    public Guid ArtigoId { get; set; }
    public Artigo Artigo { get; set; } = null!;

    public Guid AlergenioId { get; set; }
    public Alergenio Alergenio { get; set; } = null!;
}

public class Etiqueta : BaseEntity
{
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty; // Vegetariano, Picante, Sem Glúten, Vegano...
    public string CorHex { get; set; } = "#6E1A33";

    public ICollection<ArtigoEtiqueta> ArtigosEtiquetas { get; set; } = new List<ArtigoEtiqueta>();
}

public class ArtigoEtiqueta
{
    public Guid ArtigoId { get; set; }
    public Artigo Artigo { get; set; } = null!;

    public Guid EtiquetaId { get; set; }
    public Etiqueta Etiqueta { get; set; } = null!;
}

public class GrupoOpcoes : SoftDeletableEntity
{
    public string Nome { get; set; } = string.Empty; // Ponto da carne, Tamanho, Extras
    public bool Obrigatorio { get; set; } = false;
    public int Min { get; set; } = 0;
    public int Max { get; set; } = 1;

    public ICollection<Opcao> Opcoes { get; set; } = new List<Opcao>();
    public ICollection<ArtigoGrupoOpcoes> ArtigosGruposOpcoes { get; set; } = new List<ArtigoGrupoOpcoes>();
}

public class Opcao : BaseEntity
{
    public Guid GrupoId { get; set; }
    public GrupoOpcoes Grupo { get; set; } = null!;

    public string Nome { get; set; } = string.Empty;
    public decimal DeltaPreco { get; set; } = 0.00m;
    public int Ordem { get; set; } = 0;
}

public class ArtigoGrupoOpcoes
{
    public Guid ArtigoId { get; set; }
    public Artigo Artigo { get; set; } = null!;

    public Guid GrupoId { get; set; }
    public GrupoOpcoes Grupo { get; set; } = null!;
}

public class MenuVersao
{
    public int Id { get; set; } = 1;
    public long Versao { get; set; } = 1;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}
