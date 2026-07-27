using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;

namespace BlogSamples.Messaging.EventDriven;

public sealed class EventDrivenOutboxStore(
    NpgsqlDataSource dataSource,
    IOptions<EventDrivenOptions> options)
{
    private readonly EventDrivenOptions _options = options.Value;

    public async Task EnsureSchemaAsync(CancellationToken ct)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS eventdriven_pedidos (
                id uuid PRIMARY KEY,
                cliente_id text NOT NULL,
                valor numeric(18,2) NOT NULL,
                status text NOT NULL,
                etapa text NOT NULL,
                criado_em timestamptz NOT NULL
            );

            CREATE TABLE IF NOT EXISTS eventdriven_outbox (
                id uuid PRIMARY KEY,
                aggregate_id uuid NOT NULL,
                event_type text NOT NULL,
                correlation_id text NOT NULL,
                payload jsonb NOT NULL,
                status text NOT NULL,
                attempt_count integer NOT NULL DEFAULT 0,
                created_at timestamptz NOT NULL,
                updated_at timestamptz NOT NULL,
                available_at timestamptz NOT NULL,
                locked_until timestamptz NULL,
                published_at timestamptz NULL,
                last_error text NULL
            );

            CREATE INDEX IF NOT EXISTS ix_eventdriven_outbox_pending
                ON eventdriven_outbox (status, available_at, created_at);
            """;

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<PedidoViewModel> CreatePedidoWithOutboxAsync(
        CriarPedidoRequest request,
        CancellationToken ct)
    {
        var pedidoId = Guid.NewGuid();
        var criadoEm = DateTimeOffset.UtcNow;
        var state = PedidoSagaOrquestrador.HandlePedidoCriado(new PedidoSagaState
        {
            PedidoId = pedidoId,
            Status = SagaStatus.Inicializado,
            Etapa = SagaEtapa.AguardandoReserva
        });

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        var insertPedido = new NpgsqlCommand(
            """
            INSERT INTO eventdriven_pedidos (id, cliente_id, valor, status, etapa, criado_em)
            VALUES (@id, @clienteId, @valor, @status, @etapa, @criadoEm)
            """,
            conn,
            tx);
        insertPedido.Parameters.AddWithValue("id", pedidoId);
        insertPedido.Parameters.AddWithValue("clienteId", request.ClienteId);
        insertPedido.Parameters.AddWithValue("valor", request.Valor);
        insertPedido.Parameters.AddWithValue("status", state.Status.ToString());
        insertPedido.Parameters.AddWithValue("etapa", state.Etapa.ToString());
        insertPedido.Parameters.AddWithValue("criadoEm", criadoEm);
        await insertPedido.ExecuteNonQueryAsync(ct);

        foreach (var message in state.Outbox)
        {
            var envelope = new EventDrivenEnvelope(
                pedidoId,
                request.ClienteId,
                request.Valor,
                message.TipoEvento,
                criadoEm);

            var insertOutbox = new NpgsqlCommand(
                """
                INSERT INTO eventdriven_outbox (
                    id, aggregate_id, event_type, correlation_id, payload, status,
                    attempt_count, created_at, updated_at, available_at
                )
                VALUES (
                    @id, @aggregateId, @eventType, @correlationId, @payload::jsonb, @status,
                    0, @createdAt, @updatedAt, @availableAt
                )
                """,
                conn,
                tx);

            insertOutbox.Parameters.AddWithValue("id", Guid.NewGuid());
            insertOutbox.Parameters.AddWithValue("aggregateId", pedidoId);
            insertOutbox.Parameters.AddWithValue("eventType", message.TipoEvento);
            insertOutbox.Parameters.AddWithValue("correlationId", message.CorrelationId);
            insertOutbox.Parameters.AddWithValue("payload", JsonSerializer.Serialize(envelope));
            insertOutbox.Parameters.AddWithValue("status", OutboxStatus.Pendente.ToString());
            insertOutbox.Parameters.AddWithValue("createdAt", criadoEm);
            insertOutbox.Parameters.AddWithValue("updatedAt", criadoEm);
            insertOutbox.Parameters.AddWithValue("availableAt", criadoEm);
            await insertOutbox.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);

        return new PedidoViewModel
        {
            Id = pedidoId,
            ClienteId = request.ClienteId,
            Valor = request.Valor,
            Status = state.Status,
            Etapa = state.Etapa,
            CriadoEm = criadoEm
        };
    }

    public async Task<PedidoViewModel?> GetPedidoAsync(Guid id, CancellationToken ct)
    {
        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(
            """
            SELECT id, cliente_id, valor, status, etapa, criado_em
            FROM eventdriven_pedidos
            WHERE id = @id
            """,
            conn);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        return new PedidoViewModel
        {
            Id = reader.GetGuid(0),
            ClienteId = reader.GetString(1),
            Valor = reader.GetDecimal(2),
            Status = Enum.Parse<SagaStatus>(reader.GetString(3), ignoreCase: true),
            Etapa = Enum.Parse<SagaEtapa>(reader.GetString(4), ignoreCase: true),
            CriadoEm = reader.GetFieldValue<DateTimeOffset>(5)
        };
    }

    public async Task<IReadOnlyList<OutboxMessage>> LockPendingMessagesAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var lockedUntil = now.AddSeconds(_options.LockTimeoutSeconds);

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        var cmd = new NpgsqlCommand(
            """
            WITH next_messages AS (
                SELECT id
                FROM eventdriven_outbox
                WHERE status = @pending
                  AND available_at <= @now
                ORDER BY created_at
                FOR UPDATE SKIP LOCKED
                LIMIT @batchSize
            )
            UPDATE eventdriven_outbox o
            SET status = @processing,
                updated_at = @now,
                locked_until = @lockedUntil
            FROM next_messages n
            WHERE o.id = n.id
            RETURNING o.id, o.event_type, o.payload, o.attempt_count
            """,
            conn,
            tx);

        cmd.Parameters.AddWithValue("pending", OutboxStatus.Pendente.ToString());
        cmd.Parameters.AddWithValue("processing", OutboxStatus.Processando.ToString());
        cmd.Parameters.AddWithValue("now", now);
        cmd.Parameters.AddWithValue("lockedUntil", lockedUntil);
        cmd.Parameters.AddWithValue("batchSize", _options.PublisherBatchSize);

        var messages = new List<OutboxMessage>();
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                messages.Add(new OutboxMessage(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetInt32(3)));
            }
        }

        await tx.CommitAsync(ct);
        return messages;
    }

    public async Task MarkAsPublishedAsync(Guid messageId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(
            """
            UPDATE eventdriven_outbox
            SET status = @published,
                published_at = @now,
                updated_at = @now,
                locked_until = NULL,
                last_error = NULL
            WHERE id = @id
            """,
            conn);

        cmd.Parameters.AddWithValue("published", OutboxStatus.Publicado.ToString());
        cmd.Parameters.AddWithValue("now", now);
        cmd.Parameters.AddWithValue("id", messageId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task MarkAsFailedAsync(Guid messageId, int attemptCount, Exception exception, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var nextStatus = attemptCount + 1 >= _options.MaxPublishAttempts
            ? OutboxStatus.Falha
            : OutboxStatus.Pendente;

        var retryDelay = TimeSpan.FromSeconds(Math.Min(60, 2 * Math.Max(1, attemptCount + 1)));
        var availableAt = nextStatus == OutboxStatus.Pendente ? now.Add(retryDelay) : now;

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand(
            """
            UPDATE eventdriven_outbox
            SET status = @status,
                attempt_count = @attemptCount,
                last_error = @error,
                available_at = @availableAt,
                updated_at = @now,
                locked_until = NULL
            WHERE id = @id
            """,
            conn);

        cmd.Parameters.AddWithValue("status", nextStatus.ToString());
        cmd.Parameters.AddWithValue("attemptCount", attemptCount + 1);
        cmd.Parameters.AddWithValue("error", exception.Message);
        cmd.Parameters.AddWithValue("availableAt", availableAt);
        cmd.Parameters.AddWithValue("now", now);
        cmd.Parameters.AddWithValue("id", messageId);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<OutboxPageView> GetOutboxMessagesAsync(
        int limit,
        string? status,
        string sort,
        DateTimeOffset? cursorCreatedAt,
        Guid? cursorId,
        CancellationToken ct)
    {
        var safeLimit = Math.Clamp(limit, 1, 200);
        var normalizedStatus = string.IsNullOrWhiteSpace(status)
            ? null
            : status.Trim();
        var hasCursor = cursorCreatedAt.HasValue && cursorId.HasValue;
        var ascending = string.Equals(sort, "asc", StringComparison.OrdinalIgnoreCase);
        var cursorOperator = ascending ? ">" : "<";
        var orderDirection = ascending ? "ASC" : "DESC";

        await using var conn = await dataSource.OpenConnectionAsync(ct);
                var sql = $@"
SELECT id, aggregate_id, event_type, correlation_id, status,
             attempt_count, created_at, available_at, published_at, last_error
FROM eventdriven_outbox
WHERE (@status IS NULL OR status = @status)
    AND (
                NOT @hasCursor
                OR (created_at, id) {cursorOperator} (@cursorCreatedAt, @cursorId)
            )
ORDER BY created_at {orderDirection}, id {orderDirection}
LIMIT @limitPlusOne";
        await using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("status", (object?)normalizedStatus ?? DBNull.Value);
        cmd.Parameters.AddWithValue("hasCursor", hasCursor);
        cmd.Parameters.AddWithValue("cursorCreatedAt", (object?)cursorCreatedAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("cursorId", (object?)cursorId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("limitPlusOne", safeLimit + 1);

        var rows = new List<OutboxMessageView>(safeLimit + 1);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            rows.Add(new OutboxMessageView(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetInt32(5),
                reader.GetFieldValue<DateTimeOffset>(6),
                reader.GetFieldValue<DateTimeOffset>(7),
                reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
                reader.IsDBNull(9) ? null : reader.GetString(9)));
        }

        var hasMore = rows.Count > safeLimit;
        if (hasMore)
        {
            rows.RemoveAt(rows.Count - 1);
        }

        var last = rows.Count == 0 ? null : rows[^1];
        return new OutboxPageView(
            rows,
            hasMore,
            last?.CreatedAt,
            last?.Id);
    }
}

public sealed class EventDrivenSchemaInitializer(
    EventDrivenOutboxStore store,
    IOptions<EventDrivenOptions> options,
    ILogger<EventDrivenSchemaInitializer> logger) : IHostedService
{
    private readonly EventDrivenOptions _options = options.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        try
        {
            await store.EnsureSchemaAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Não foi possível inicializar schema do EventDriven. O app continuará ativo, mas os endpoints EventDriven dependem do PostgreSQL.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public enum OutboxStatus
{
    Pendente,
    Processando,
    Publicado,
    Falha
}

public sealed record OutboxMessage(Guid Id, string EventType, string Payload, int AttemptCount);

public sealed record EventDrivenEnvelope(
    Guid PedidoId,
    string ClienteId,
    decimal Valor,
    string TipoEvento,
    DateTimeOffset CriadoEm);

public sealed record OutboxMessageView(
    Guid Id,
    Guid AggregateId,
    string EventType,
    string CorrelationId,
    string Status,
    int AttemptCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset AvailableAt,
    DateTimeOffset? PublishedAt,
    string? LastError);

public sealed record OutboxPageView(
    IReadOnlyList<OutboxMessageView> Items,
    bool HasMore,
    DateTimeOffset? NextCursorCreatedAt,
    Guid? NextCursorId);