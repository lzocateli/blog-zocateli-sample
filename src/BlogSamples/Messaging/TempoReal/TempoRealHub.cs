using Microsoft.AspNetCore.SignalR;

namespace BlogSamples.Messaging.TempoReal;

public sealed class TempoRealHub(TempoRealProcessamentoService processamentoService) : Hub
{
    public async Task AcompanharTarefa(Guid tarefaId, CancellationToken cancellationToken)
    {
        await foreach (var estado in processamentoService.ObterFluxoAsync(tarefaId, cancellationToken))
        {
            await Clients.Caller.SendAsync("estado", estado, cancellationToken);
        }
    }
}