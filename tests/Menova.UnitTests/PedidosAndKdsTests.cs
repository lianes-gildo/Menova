using Menova.Application.DTOs;
using Menova.Domain.Pedidos;
using Xunit;

namespace Menova.UnitTests;

public class PedidosAndKdsTests
{
    [Theory]
    [InlineData(5, UrgenciaKds.Normal)]
    [InlineData(9, UrgenciaKds.Normal)]
    [InlineData(10, UrgenciaKds.Atencao)]
    [InlineData(15, UrgenciaKds.Atencao)]
    [InlineData(20, UrgenciaKds.Atencao)]
    [InlineData(21, UrgenciaKds.Urgente)]
    [InlineData(45, UrgenciaKds.Urgente)]
    public void Kds_CalculoDeUrgencia_DeveSeguirFaixasDeTempo(int minutos, UrgenciaKds urgenciaEsperada)
    {
        var urgencia = minutos switch
        {
            < 10 => UrgenciaKds.Normal,
            <= 20 => UrgenciaKds.Atencao,
            _ => UrgenciaKds.Urgente
        };

        Assert.Equal(urgenciaEsperada, urgencia);
    }

    [Fact]
    public void CalculoIvaMocambique_PrecoComIva16PorCento_DeveCalcularValoresCorretos()
    {
        // Exemplo: Frango à Zambeziana a 650.00 MT com 16% de IVA incluído
        decimal precoVenda = 650.00m;
        decimal taxaIva = 16.00m;

        decimal valorSemIva = Math.Round(precoVenda / (1 + (taxaIva / 100m)), 4);
        decimal valorIva = precoVenda - valorSemIva;

        Assert.Equal(560.3448m, valorSemIva);
        Assert.Equal(89.6552m, valorIva);
        Assert.Equal(precoVenda, valorSemIva + valorIva);
    }

    [Fact]
    public void EstadoPedido_DeveTransitarCorretamente()
    {
        var pedido = new Pedido
        {
            Estado = EstadoPedido.Pendente
        };

        Assert.Equal(EstadoPedido.Pendente, pedido.Estado);

        pedido.Estado = EstadoPedido.EmPreparacao;
        Assert.Equal(EstadoPedido.EmPreparacao, pedido.Estado);

        pedido.Estado = EstadoPedido.Pronto;
        Assert.Equal(EstadoPedido.Pronto, pedido.Estado);

        pedido.Estado = EstadoPedido.Entregue;
        Assert.Equal(EstadoPedido.Entregue, pedido.Estado);
    }
}
