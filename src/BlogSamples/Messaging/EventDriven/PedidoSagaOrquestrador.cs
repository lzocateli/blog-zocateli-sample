namespace BlogSamples.Messaging.EventDriven;

public static class PedidoSagaOrquestrador
{
    public static PedidoSagaState HandlePedidoCriado(PedidoSagaState saga)
    {
        if (saga.Processados.Contains("PedidoCriado"))
        {
            return saga;
        }

        saga.Processados.Add("PedidoCriado");
        saga.Status = SagaStatus.EmAndamento;
        saga.Etapa = SagaEtapa.AguardandoReserva;
        saga.Outbox.Add(new SagaOutboxMessage
        {
            TipoEvento = "ReservarEstoque",
            CorrelationId = saga.PedidoId.ToString()
        });

        return saga;
    }

    public static PedidoSagaState HandleEstoqueReservado(PedidoSagaState saga)
    {
        if (saga.Processados.Contains("EstoqueReservado"))
        {
            return saga;
        }

        saga.Processados.Add("EstoqueReservado");
        saga.Status = SagaStatus.EmAndamento;
        saga.Etapa = SagaEtapa.AguardandoPagamento;
        saga.Outbox.Add(new SagaOutboxMessage
        {
            TipoEvento = "ProcessarPagamento",
            CorrelationId = saga.PedidoId.ToString()
        });

        return saga;
    }

    public static PedidoSagaState HandleEstoqueIndisponivel(PedidoSagaState saga)
    {
        if (saga.Processados.Contains("EstoqueIndisponivel"))
        {
            return saga;
        }

        saga.Processados.Add("EstoqueIndisponivel");
        saga.Status = SagaStatus.Cancelado;
        saga.Etapa = SagaEtapa.Cancelado;
        saga.Outbox.Clear();
        return saga;
    }

    public static PedidoSagaState HandlePagamentoRecusado(PedidoSagaState saga)
    {
        if (saga.Processados.Contains("PagamentoRecusado"))
        {
            return saga;
        }

        saga.Processados.Add("PagamentoRecusado");
        saga.Status = SagaStatus.Cancelado;
        saga.Etapa = SagaEtapa.Cancelado;
        saga.Outbox.Add(new SagaOutboxMessage
        {
            TipoEvento = "LiberarEstoque",
            CorrelationId = saga.PedidoId.ToString()
        });
        saga.Outbox.Add(new SagaOutboxMessage
        {
            TipoEvento = "CancelarPedido",
            CorrelationId = saga.PedidoId.ToString()
        });

        return saga;
    }
}
