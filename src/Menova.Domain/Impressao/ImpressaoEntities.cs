using Menova.Domain.Common;

namespace Menova.Domain.Impressao;

public enum TipoDocumentoImpressao
{
    TalaoConta = 1,
    TalaoCozinha = 2,
    RelatorioX = 3,
    TalaoZ = 4,
    MovimentoCaixa = 5
}

public enum EstadoTrabalhoImpressao
{
    Pendente = 1,
    Enviado = 2,
    Falhado = 3
}

public class Impressora : SoftDeletableEntity
{
    public string Nome { get; set; } = string.Empty;
    public string EnderecoIp { get; set; } = "192.168.1.200";
    public int Porta { get; set; } = 9100;
    public int LarguraColunas { get; set; } = 42; // 42 para 80mm, 32 para 58mm
    public bool Ativa { get; set; } = true;
    public bool CortarPapel { get; set; } = true;
    public bool AbrirGaveta { get; set; } = false;
}

public class TrabalhoImpressao : BaseEntity
{
    public Guid? ImpressoraId { get; set; }
    public Impressora? Impressora { get; set; }

    public TipoDocumentoImpressao TipoDocumento { get; set; } = TipoDocumentoImpressao.TalaoConta;
    public byte[] ConteudoBytes { get; set; } = Array.Empty<byte>();
    public EstadoTrabalhoImpressao Estado { get; set; } = EstadoTrabalhoImpressao.Pendente;
    public int Tentativas { get; set; } = 0;
    public string? MensagemErro { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? EnviadoEm { get; set; }
}
