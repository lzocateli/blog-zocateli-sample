using System.Collections.Concurrent;

namespace BlogSamples.Messaging.TempoReal;

public sealed class TempoRealProcessamentoService
{
    private static readonly TimeSpan IntervaloDeAmostragem = TimeSpan.FromMilliseconds(250);

    private readonly ConcurrentDictionary<Guid, EstadoInterno> _tarefas = new();

    public TempoRealCriacaoResponse CriarTarefa(string descricao)
    {
        var tarefaId = Guid.NewGuid();
        var estadoInicial = new EstadoInterno(
            tarefaId,
            descricao,
            TempoRealStatus.Criada,
            0,
            0,
            DateTimeOffset.UtcNow,
            "Recebida",
            "Aguardando processamento");

        _tarefas[tarefaId] = estadoInicial;

        _ = Task.Run(() => SimularFluxoAsync(tarefaId));

        return new TempoRealCriacaoResponse(
            tarefaId,
            descricao,
            estadoInicial.Status.ToString(),
            estadoInicial.Versao);
    }

    public TempoRealEstadoResponse? ObterEstado(Guid tarefaId)
    {
        return _tarefas.TryGetValue(tarefaId, out var estado)
            ? estado.ToResponse()
            : null;
    }

    public async IAsyncEnumerable<TempoRealEstadoResponse> ObterFluxoAsync(
        Guid tarefaId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var ultimaVersao = -1;

        while (!cancellationToken.IsCancellationRequested)
        {
            var estado = ObterEstado(tarefaId);
            if (estado is null)
            {
                yield break;
            }

            if (estado.Versao > ultimaVersao)
            {
                ultimaVersao = estado.Versao;
                yield return estado;
            }

            if (estado.Status is nameof(TempoRealStatus.Concluida) or nameof(TempoRealStatus.Falhou))
            {
                yield break;
            }

            await Task.Delay(IntervaloDeAmostragem, cancellationToken);
        }
    }

    public async Task<TempoRealEstadoResponse?> AguardarAtualizacaoAsync(
        Guid tarefaId,
        int versaoAtual,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var fim = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < fim && !cancellationToken.IsCancellationRequested)
        {
            var estado = ObterEstado(tarefaId);
            if (estado is null)
            {
                return null;
            }

            if (estado.Versao > versaoAtual || estado.Status is nameof(TempoRealStatus.Concluida) or nameof(TempoRealStatus.Falhou))
            {
                return estado;
            }

            await Task.Delay(IntervaloDeAmostragem, cancellationToken);
        }

        return ObterEstado(tarefaId);
    }

    private async Task SimularFluxoAsync(Guid tarefaId)
    {
        var deveFalhar = _tarefas.TryGetValue(tarefaId, out var estadoInicial)
            && estadoInicial.Descricao.Contains("falha", StringComparison.OrdinalIgnoreCase);

        var etapas = new[]
        {
            ("Validação da entrada", TempoRealStatus.Processando, 15, "Entrada validada"),
            ("Carregando dependências", TempoRealStatus.Processando, 40, "Dependências resolvidas"),
            ("Executando processamento", TempoRealStatus.Processando, 70, "Trabalho em andamento"),
            (deveFalhar ? "Falha simulada" : "Finalização", deveFalhar ? TempoRealStatus.Falhou : TempoRealStatus.Concluida, 100, deveFalhar ? "Falha proposital para demonstrar erro" : "Processamento concluído")
        };

        foreach (var etapa in etapas)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            AtualizarEstado(tarefaId, etapa.Item1, etapa.Item2, etapa.Item3, etapa.Item4);
        }
    }

    private void AtualizarEstado(
        Guid tarefaId,
        string etapa,
        TempoRealStatus status,
        int progresso,
        string mensagem)
    {
        _tarefas.AddOrUpdate(
            tarefaId,
            _ => new EstadoInterno(
                tarefaId,
                mensagem,
                status,
                progresso,
                1,
                DateTimeOffset.UtcNow,
                etapa,
                mensagem),
            (_, atual) => atual with
            {
                Status = status,
                Etapa = etapa,
                Progresso = progresso,
                Versao = atual.Versao + 1,
                AtualizadoEmUtc = DateTimeOffset.UtcNow,
                Mensagem = mensagem
            });
    }

    private sealed record EstadoInterno(
        Guid TarefaId,
        string Descricao,
        TempoRealStatus Status,
        int Progresso,
        int Versao,
        DateTimeOffset AtualizadoEmUtc,
        string Etapa,
        string Mensagem)
    {
        public TempoRealEstadoResponse ToResponse() => new(
            TarefaId,
            Descricao,
            Status.ToString(),
            Etapa,
            Progresso,
            Versao,
            AtualizadoEmUtc,
            Mensagem);
    }
}