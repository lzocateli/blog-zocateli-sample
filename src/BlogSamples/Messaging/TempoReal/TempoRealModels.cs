namespace BlogSamples.Messaging.TempoReal;

public enum TempoRealStatus
{
    Criada,
    Processando,
    Concluida,
    Falhou
}

public sealed record TempoRealCriacaoRequest(string Descricao);

public sealed record TempoRealCriacaoResponse(
    Guid TarefaId,
    string Descricao,
    string Status,
    int Versao);

public sealed record TempoRealEstadoResponse(
    Guid TarefaId,
    string Descricao,
    string Status,
    string Etapa,
    int Progresso,
    int Versao,
    DateTimeOffset AtualizadoEmUtc,
    string Mensagem);