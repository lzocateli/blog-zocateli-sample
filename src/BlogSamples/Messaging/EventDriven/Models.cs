using System.ComponentModel.DataAnnotations;

namespace BlogSamples.Messaging.EventDriven;

public enum SagaStatus
{
    Inicializado,
    EmAndamento,
    Cancelado,
    Confirmado
}

public enum SagaEtapa
{
    AguardandoReserva,
    AguardandoPagamento,
    Confirmado,
    Cancelado
}

public sealed class PedidoSagaState
{
    public Guid PedidoId { get; set; }
    public SagaStatus Status { get; set; }
    public SagaEtapa Etapa { get; set; }
    public List<string> Processados { get; set; } = [];
    public List<SagaOutboxMessage> Outbox { get; set; } = [];
}

public sealed class SagaOutboxMessage
{
    public string TipoEvento { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
}

public sealed class CriarPedidoRequest
{
    [Required]
    public string ClienteId { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}

public sealed class PedidoViewModel
{
    public Guid Id { get; init; }
    public string ClienteId { get; init; } = string.Empty;
    public decimal Valor { get; init; }
    public SagaStatus Status { get; init; }
    public SagaEtapa Etapa { get; init; }
    public DateTimeOffset CriadoEm { get; init; }
}
