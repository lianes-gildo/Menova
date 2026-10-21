using System.Text;
using Menova.Application.DTOs;

namespace Menova.Infrastructure.Services;

public class EscPosGenerator
{
    private readonly int _larguraColunas;
    private readonly Encoding _encoding;

    public EscPosGenerator(int larguraColunas = 32)
    {
        // Se largura não especificada ou inválida, assumir 32 colunas (padrão universal 58mm)
        _larguraColunas = (larguraColunas >= 24 && larguraColunas <= 64) ? larguraColunas : 32;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        try
        {
            _encoding = Encoding.GetEncoding(860);
        }
        catch
        {
            _encoding = Encoding.Latin1;
        }
    }

    public static byte[] ComandoInicializar => new byte[] { 0x1B, 0x40 }; // ESC @
    public static byte[] ComandoAlinhamentoEsquerda => new byte[] { 0x1B, 0x61, 0x00 }; // ESC a 0
    public static byte[] ComandoAlinhamentoCentro => new byte[] { 0x1B, 0x61, 0x01 }; // ESC a 1
    public static byte[] ComandoAlinhamentoDireita => new byte[] { 0x1B, 0x61, 0x02 }; // ESC a 2
    public static byte[] ComandoNegritoLigado => new byte[] { 0x1B, 0x45, 0x01 }; // ESC E 1
    public static byte[] ComandoNegritoDesligado => new byte[] { 0x1B, 0x45, 0x00 }; // ESC E 0
    public static byte[] ComandoFonteDuplaAltura => new byte[] { 0x1D, 0x21, 0x01 }; // GS ! 1
    public static byte[] ComandoFonteNormal => new byte[] { 0x1D, 0x21, 0x00 }; // GS ! 0
    public static byte[] ComandoCortarPapel => new byte[] { 0x1D, 0x56, 0x41, 0x10 }; // GS V A 16 (Feed and cut)
    public static byte[] ComandoAbrirGaveta => new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA }; // ESC p 0 25 250

    private string Separador() => new string('-', _larguraColunas);
    private string SeparadorDuplo() => new string('=', _larguraColunas);

    private string AlinharLinha(string esquerda, string direita)
    {
        direita = direita?.Trim() ?? string.Empty;
        esquerda = esquerda?.Trim() ?? string.Empty;

        // Se o valor da direita por si só já preenche ou excede a largura
        if (direita.Length >= _larguraColunas)
        {
            return direita.Substring(0, _larguraColunas);
        }

        var maxEsquerda = _larguraColunas - direita.Length - 1;
        if (esquerda.Length > maxEsquerda && maxEsquerda > 0)
        {
            esquerda = esquerda.Substring(0, maxEsquerda);
        }

        var espacos = _larguraColunas - esquerda.Length - direita.Length;
        if (espacos < 1) espacos = 1;

        return esquerda + new string(' ', espacos) + direita;
    }

    public byte[] GerarTalaoConta(ContaDto conta, string restauranteNome, string nuit, string endereco, bool eSegundaVia = false)
    {
        using var ms = new MemoryStream();
        ms.Write(ComandoInicializar);
        ms.Write(ComandoAlinhamentoCentro);

        if (eSegundaVia)
        {
            ms.Write(ComandoNegritoLigado);
            EscreverLinha(ms, "*** 2ª VIA / CÓPIA ***");
            ms.Write(ComandoNegritoDesligado);
        }

        ms.Write(ComandoNegritoLigado);
        EscreverLinha(ms, FormatarTextoCentro(restauranteNome));
        ms.Write(ComandoNegritoDesligado);

        EscreverLinha(ms, $"NUIT: {nuit}");
        if (!string.IsNullOrWhiteSpace(endereco))
        {
            EscreverLinha(ms, FormatarTextoCentro(endereco));
        }
        EscreverLinha(ms, Separador());

        ms.Write(ComandoAlinhamentoEsquerda);
        EscreverLinha(ms, $"Conta: {conta.Numero}");
        if (!string.IsNullOrWhiteSpace(conta.MesaNumero))
        {
            EscreverLinha(ms, $"Mesa: {conta.MesaNumero} ({conta.ZonaNome ?? "Sala"})");
        }
        EscreverLinha(ms, $"Abertura: {conta.CriadoEm.AddHours(2):dd/MM/yyyy HH:mm}");
        EscreverLinha(ms, $"Operador: {conta.CriadoPor}");
        EscreverLinha(ms, Separador());

        EscreverLinha(ms, AlinharLinha("ARTIGO", "TOTAL (MT)"));
        EscreverLinha(ms, Separador());

        var todosItens = conta.Pedidos.SelectMany(p => p.Itens).Where(i => !i.Anulado).ToList();
        foreach (var item in todosItens)
        {
            var qtdPreco = $"{item.Quantidade:0.##}x {item.PrecoUnitario:N2}";
            var totalItem = $"{item.TotalLiquido:N2} MT";

            // Se o nome do artigo for longo, imprime o nome na 1ª linha e quantidade/preço na 2ª
            if (item.NomeArtigo.Length + totalItem.Length >= _larguraColunas - 1)
            {
                EscreverLinha(ms, item.NomeArtigo);
                EscreverLinha(ms, AlinharLinha($"  {qtdPreco}", totalItem));
            }
            else
            {
                EscreverLinha(ms, AlinharLinha($"{item.Quantidade:0.##}x {item.NomeArtigo}", totalItem));
            }

            if (item.Opcoes != null && item.Opcoes.Any())
            {
                foreach (var opc in item.Opcoes)
                {
                    EscreverLinha(ms, $"  + {opc}");
                }
            }
        }

        EscreverLinha(ms, Separador());
        ms.Write(ComandoNegritoLigado);
        EscreverLinha(ms, AlinharLinha("SUBTOTAL:", $"{conta.TotalBruto:N2} MT"));
        if (conta.TotalDesconto > 0)
        {
            EscreverLinha(ms, AlinharLinha("DESCONTO:", $"-{conta.TotalDesconto:N2} MT"));
        }
        EscreverLinha(ms, AlinharLinha("IVA (16% Incl.):", $"{conta.TotalIva:N2} MT"));
        EscreverLinha(ms, AlinharLinha("TOTAL A PAGAR:", $"{conta.TotalLiquido:N2} MT"));
        ms.Write(ComandoNegritoDesligado);
        EscreverLinha(ms, Separador());

        ms.Write(ComandoAlinhamentoCentro);
        EscreverLinha(ms, "Talão de Consulta de Mesa");
        EscreverLinha(ms, "Não serve de fatura");
        EscreverLinha(ms, "Menova Restaurante");
        EscreverLinha(ms, "\n");
        ms.Write(ComandoCortarPapel);

        return ms.ToArray();
    }

    public byte[] GerarReciboPagamento(ContaDto conta, PagamentoDto pagamento, decimal saldoRestante, string restauranteNome, string nuit, string endereco, bool eSegundaVia = false)
    {
        using var ms = new MemoryStream();
        ms.Write(ComandoInicializar);
        ms.Write(ComandoAlinhamentoCentro);

        if (eSegundaVia)
        {
            ms.Write(ComandoNegritoLigado);
            EscreverLinha(ms, "*** 2ª VIA / CÓPIA ***");
            ms.Write(ComandoNegritoDesligado);
        }

        ms.Write(ComandoNegritoLigado);
        EscreverLinha(ms, FormatarTextoCentro(restauranteNome));
        ms.Write(ComandoNegritoDesligado);

        EscreverLinha(ms, $"NUIT: {nuit}");
        if (!string.IsNullOrWhiteSpace(endereco))
        {
            EscreverLinha(ms, FormatarTextoCentro(endereco));
        }
        EscreverLinha(ms, Separador());

        ms.Write(ComandoNegritoLigado);
        EscreverLinha(ms, "RECIBO DE PAGAMENTO");
        ms.Write(ComandoNegritoDesligado);
        EscreverLinha(ms, Separador());

        ms.Write(ComandoAlinhamentoEsquerda);
        EscreverLinha(ms, $"Conta: {conta.Numero}");
        if (!string.IsNullOrWhiteSpace(conta.MesaNumero))
        {
            EscreverLinha(ms, $"Mesa: {conta.MesaNumero}");
        }
        EscreverLinha(ms, $"Data/Hora: {pagamento.CriadoEm.AddHours(2):dd/MM/yyyy HH:mm}");
        EscreverLinha(ms, $"Operador: {pagamento.CriadoPor}");
        EscreverLinha(ms, Separador());

        EscreverLinha(ms, AlinharLinha("Forma Pagamento:", pagamento.MetodoDescricao));
        ms.Write(ComandoNegritoLigado);
        EscreverLinha(ms, AlinharLinha("VALOR PAGO:", $"{pagamento.Valor:N2} MT"));
        ms.Write(ComandoNegritoDesligado);

        if (pagamento.ValorRecebido > 0)
        {
            EscreverLinha(ms, AlinharLinha("Valor Entregue:", $"{pagamento.ValorRecebido:N2} MT"));
            EscreverLinha(ms, AlinharLinha("Troco Entregue:", $"{pagamento.Troco:N2} MT"));
        }
        if (pagamento.Gorjeta > 0)
        {
            EscreverLinha(ms, AlinharLinha("Gorjeta:", $"{pagamento.Gorjeta:N2} MT"));
        }
        if (!string.IsNullOrWhiteSpace(pagamento.Referencia))
        {
            EscreverLinha(ms, $"Ref/Transação: {pagamento.Referencia}");
        }

        EscreverLinha(ms, Separador());
        ms.Write(ComandoNegritoLigado);
        if (saldoRestante > 0)
        {
            EscreverLinha(ms, AlinharLinha("SALDO RESTANTE:", $"{saldoRestante:N2} MT"));
        }
        else
        {
            EscreverLinha(ms, AlinharLinha("ESTADO CONTA:", "TOTALMENTE LIQUIDADA"));
        }
        ms.Write(ComandoNegritoDesligado);

        EscreverLinha(ms, Separador());
        ms.Write(ComandoAlinhamentoCentro);
        EscreverLinha(ms, "Obrigado pela sua preferência!");
        EscreverLinha(ms, "Volte Sempre!");
        EscreverLinha(ms, "\n");
        ms.Write(ComandoCortarPapel);

        return ms.ToArray();
    }

    public byte[] GerarTalaoCozinha(TicketKdsDto ticket)
    {
        using var ms = new MemoryStream();
        ms.Write(ComandoInicializar);
        ms.Write(ComandoAlinhamentoCentro);
        ms.Write(ComandoNegritoLigado);
        EscreverLinha(ms, $"PEDIDO #{ticket.NumeroSequencial}");
        ms.Write(ComandoNegritoDesligado);

        EscreverLinha(ms, SeparadorDuplo());
        ms.Write(ComandoAlinhamentoEsquerda);
        EscreverLinha(ms, $"Mesa: {ticket.MesaNumero ?? "Balcão"} ({ticket.ZonaNome ?? "Salão"})");
        EscreverLinha(ms, $"Hora: {ticket.CriadoEm.AddHours(2):HH:mm:ss}");
        if (!string.IsNullOrWhiteSpace(ticket.Observacoes))
        {
            EscreverLinha(ms, $"Obs: {ticket.Observacoes}");
        }
        EscreverLinha(ms, Separador());

        foreach (var item in ticket.Itens)
        {
            ms.Write(ComandoNegritoLigado);
            EscreverLinha(ms, $"{item.Quantidade:0.##}x {item.NomeArtigo}");
            ms.Write(ComandoNegritoDesligado);

            if (item.Opcoes != null && item.Opcoes.Any())
            {
                foreach (var opc in item.Opcoes)
                {
                    EscreverLinha(ms, $"  > {opc}");
                }
            }
            if (!string.IsNullOrWhiteSpace(item.Notas))
            {
                EscreverLinha(ms, $"  * NOTA: {item.Notas}");
            }
        }

        EscreverLinha(ms, SeparadorDuplo());
        EscreverLinha(ms, "\n");
        ms.Write(ComandoCortarPapel);

        return ms.ToArray();
    }

    public byte[] GerarRelatorioX(RelatorioXDto x)
    {
        using var ms = new MemoryStream();
        ms.Write(ComandoInicializar);
        ms.Write(ComandoAlinhamentoCentro);
        ms.Write(ComandoNegritoLigado);
        EscreverLinha(ms, "RELATÓRIO X");
        ms.Write(ComandoNegritoDesligado);
        EscreverLinha(ms, "(Consulta Parcial de Turno)");
        EscreverLinha(ms, SeparadorDuplo());

        ms.Write(ComandoAlinhamentoEsquerda);
        EscreverLinha(ms, $"Caixa: {x.CaixaNome} (#{x.CaixaNumero})");
        EscreverLinha(ms, $"Operador: {x.OperadorNome}");
        EscreverLinha(ms, $"Abertura: {x.AbertaEm.AddHours(2):dd/MM/yyyy HH:mm}");
        EscreverLinha(ms, $"Emissão: {x.EmitidoEm.AddHours(2):dd/MM/yyyy HH:mm}");
        EscreverLinha(ms, Separador());

        EscreverLinha(ms, AlinharLinha("Saldo Inicial:", $"{x.SaldoInicial:N2} MT"));
        EscreverLinha(ms, AlinharLinha("Total Vendas:", $"{x.TotalVendas:N2} MT"));
        EscreverLinha(ms, AlinharLinha("Entradas/Suprim.:", $"{x.TotalEntradas:N2} MT"));
        EscreverLinha(ms, AlinharLinha("Saídas/Sangrias:", $"{x.TotalSaidas:N2} MT"));
        ms.Write(ComandoNegritoLigado);
        EscreverLinha(ms, AlinharLinha("Saldo Esperado:", $"{x.SaldoEsperado:N2} MT"));
        ms.Write(ComandoNegritoDesligado);
        EscreverLinha(ms, Separador());

        EscreverLinha(ms, "VENDAS POR FORMA DE PAGAMENTO:");
        foreach (var (metodo, total) in x.VendasPorMetodo)
        {
            EscreverLinha(ms, AlinharLinha($"  {metodo}:", $"{total:N2} MT"));
        }

        EscreverLinha(ms, Separador());
        EscreverLinha(ms, "RESUMO DE IVA:");
        foreach (var (taxa, valor) in x.TotaisPorIva)
        {
            EscreverLinha(ms, AlinharLinha($"  IVA {taxa}:", $"{valor:N2} MT"));
        }

        EscreverLinha(ms, SeparadorDuplo());
        ms.Write(ComandoAlinhamentoCentro);
        EscreverLinha(ms, "Menova POS - Controlo Interno");
        EscreverLinha(ms, "\n");
        ms.Write(ComandoCortarPapel);

        return ms.ToArray();
    }

    public byte[] GerarTalaoZ(DocumentoZDto z, bool eSegundaVia = false)
    {
        using var ms = new MemoryStream();
        ms.Write(ComandoInicializar);
        ms.Write(ComandoAlinhamentoCentro);

        if (eSegundaVia)
        {
            ms.Write(ComandoNegritoLigado);
            EscreverLinha(ms, "*** 2ª VIA / CÓPIA ***");
            ms.Write(ComandoNegritoDesligado);
        }

        ms.Write(ComandoNegritoLigado);
        EscreverLinha(ms, $"TALÃO Z N.º {z.NumeroZ}");
        ms.Write(ComandoNegritoDesligado);
        EscreverLinha(ms, "FECHO DIÁRIO / TURNO DE CAIXA");
        EscreverLinha(ms, SeparadorDuplo());

        ms.Write(ComandoAlinhamentoEsquerda);
        EscreverLinha(ms, $"Caixa: {z.CaixaNome} (#{z.CaixaNumero})");
        EscreverLinha(ms, $"Data/Hora: {z.DataHora.AddHours(2):dd/MM/yyyy HH:mm:ss}");
        EscreverLinha(ms, $"Emitido Por: {z.EmitidoPor}");
        EscreverLinha(ms, $"Documentos Faturados: {z.TotalDocumentosFaturados}");
        EscreverLinha(ms, Separador());

        EscreverLinha(ms, AlinharLinha("Total Bruto:", $"{z.TotalBruto:N2} MT"));
        EscreverLinha(ms, AlinharLinha("Total Descontos:", $"{z.TotalDesconto:N2} MT"));
        EscreverLinha(ms, AlinharLinha("Total IVA (16%):", $"{z.TotalIva:N2} MT"));
        ms.Write(ComandoNegritoLigado);
        EscreverLinha(ms, AlinharLinha("TOTAL LÍQUIDO:", $"{z.TotalLiquido:N2} MT"));
        ms.Write(ComandoNegritoDesligado);
        EscreverLinha(ms, Separador());

        EscreverLinha(ms, "TOTAIS POR MÉTODO DE PAGAMENTO:");
        foreach (var (metodo, total) in z.TotaisPorMetodo)
        {
            EscreverLinha(ms, AlinharLinha($"  {metodo}:", $"{total:N2} MT"));
        }

        EscreverLinha(ms, Separador());
        EscreverLinha(ms, "TOTAIS POR INCIDÊNCIA DE IVA:");
        foreach (var (taxa, total) in z.TotaisPorTaxaIva)
        {
            EscreverLinha(ms, AlinharLinha($"  Taxa {taxa}:", $"{total:N2} MT"));
        }

        EscreverLinha(ms, SeparadorDuplo());
        ms.Write(ComandoAlinhamentoCentro);
        EscreverLinha(ms, "Processado por Sistema Certificado");
        EscreverLinha(ms, "Menova Restaurante");
        EscreverLinha(ms, "\n");
        ms.Write(ComandoCortarPapel);

        return ms.ToArray();
    }

    public byte[] GerarComprovativoMovimento(MovimentoCaixaDto mov, string caixaNome)
    {
        using var ms = new MemoryStream();
        ms.Write(ComandoInicializar);
        ms.Write(ComandoAlinhamentoCentro);
        ms.Write(ComandoNegritoLigado);
        EscreverLinha(ms, $"COMPROVATIVO DE {mov.TipoDescricao.ToUpperInvariant()}");
        ms.Write(ComandoNegritoDesligado);
        EscreverLinha(ms, Separador());

        ms.Write(ComandoAlinhamentoEsquerda);
        EscreverLinha(ms, $"Caixa: {caixaNome}");
        EscreverLinha(ms, $"Data: {mov.CriadoEm.AddHours(2):dd/MM/yyyy HH:mm}");
        EscreverLinha(ms, $"Operador: {mov.CriadoPor}");
        EscreverLinha(ms, Separador());

        ms.Write(ComandoNegritoLigado);
        EscreverLinha(ms, AlinharLinha("VALOR:", $"{mov.Valor:N2} MT"));
        ms.Write(ComandoNegritoDesligado);
        EscreverLinha(ms, $"Motivo: {mov.Motivo}");
        EscreverLinha(ms, Separador());

        EscreverLinha(ms, "\nAssinatura: ___________________________");
        EscreverLinha(ms, "\n");
        ms.Write(ComandoCortarPapel);

        return ms.ToArray();
    }

    private string FormatarTextoCentro(string texto)
    {
        texto = texto?.Trim() ?? string.Empty;
        if (texto.Length >= _larguraColunas) return texto.Substring(0, _larguraColunas);
        return texto;
    }

    private void EscreverLinha(MemoryStream ms, string texto)
    {
        var bytes = _encoding.GetBytes(texto + "\n");
        ms.Write(bytes, 0, bytes.Length);
    }
}
