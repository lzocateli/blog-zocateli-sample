using BlogSamples.Messaging.EventDriven;

namespace BlogSamples.Tests.Messaging.EventDriven;

public class SagaOrquestradorTests
{
    [Fact]
    public void ProcessarPedidoCriado_EmiteReservarEstoque_QuandoPedidoCriado()
    {
        var saga = new PedidoSagaState
        {
            PedidoId = Guid.NewGuid(),
            Status = SagaStatus.Inicializado,
            Etapa = SagaEtapa.AguardandoReserva
        };

        var resultado = PedidoSagaOrquestrador.HandlePedidoCriado(saga);

        Assert.Equal(SagaStatus.EmAndamento, resultado.Status);
        Assert.Equal(SagaEtapa.AguardandoReserva, resultado.Etapa);
        Assert.Single(resultado.Outbox);
        Assert.Equal("ReservarEstoque", resultado.Outbox[0].TipoEvento);
    }

    [Fact]
    public void ProcessarEstoqueReservado_AtualizaParaPagamento()
    {
        var saga = new PedidoSagaState
        {
            PedidoId = Guid.NewGuid(),
            Status = SagaStatus.EmAndamento,
            Etapa = SagaEtapa.AguardandoReserva
        };

        var resultado = PedidoSagaOrquestrador.HandleEstoqueReservado(saga);

        Assert.Equal(SagaStatus.EmAndamento, resultado.Status);
        Assert.Equal(SagaEtapa.AguardandoPagamento, resultado.Etapa);
        Assert.Single(resultado.Outbox);
        Assert.Equal("ProcessarPagamento", resultado.Outbox[0].TipoEvento);
    }

    [Fact]
    public void ProcessarPagamentoRecusado_EmiteLiberarEstoque_ECancelaPedido()
    {
        var saga = new PedidoSagaState
        {
            PedidoId = Guid.NewGuid(),
            Status = SagaStatus.EmAndamento,
            Etapa = SagaEtapa.AguardandoPagamento
        };

        var resultado = PedidoSagaOrquestrador.HandlePagamentoRecusado(saga);

        Assert.Equal(SagaStatus.Cancelado, resultado.Status);
        Assert.Equal(SagaEtapa.Cancelado, resultado.Etapa);
        Assert.Equal(2, resultado.Outbox.Count);
        Assert.Contains(resultado.Outbox, item => item.TipoEvento == "LiberarEstoque");
    }

    [Fact]
    public void ProcessarEstoqueIndisponivel_EncerraSagaSemPagamento()
    {
        var saga = new PedidoSagaState
        {
            PedidoId = Guid.NewGuid(),
            Status = SagaStatus.EmAndamento,
            Etapa = SagaEtapa.AguardandoReserva
        };

        var resultado = PedidoSagaOrquestrador.HandleEstoqueIndisponivel(saga);

        Assert.Equal(SagaStatus.Cancelado, resultado.Status);
        Assert.Equal(SagaEtapa.Cancelado, resultado.Etapa);
        Assert.Empty(resultado.Outbox);
    }

    [Fact]
    public void ProcessarEventoDuplicado_NaoRepeteEfeito()
    {
        var saga = new PedidoSagaState
        {
            PedidoId = Guid.NewGuid(),
            Status = SagaStatus.EmAndamento,
            Etapa = SagaEtapa.AguardandoReserva,
            Processados = ["PedidoCriado"]
        };

        var resultado = PedidoSagaOrquestrador.HandlePedidoCriado(saga);

        Assert.Equal(SagaStatus.EmAndamento, resultado.Status);
        Assert.Empty(resultado.Outbox);
    }
}
