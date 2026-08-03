Design assumptions:
    1. Production projection consumers process one Kafka event as one transactional ProjectionSingle command.
    2. Projection topics are separate from bespoke domain-event topics and use the -projection suffix.
    3. Projection writes and client-side Kafka offsets are committed in the same EF transaction.
    4. Write repositories operate on domain aggregates; read repositories use persistence-only read entities.

Rules:
    1. Projection tables mirror source-service data without consumer-side business mutations.
    2. Projection upserts are version-aware: an older or duplicate event cannot overwrite a newer row.
    3. Subscribers validate and map transport events, while persistence repositories own database-specific UPSERT logic.

Cross-service projection flow:
==============================

  1. The source service publishes ProjectionIntegrationEvent<TReadModel> through its transactional outbox.

  2. A dedicated subscriber in the consuming service:
     - validates SourceAggregateVersion and the projection payload,
     - optionally filters events that do not produce a local projection,
     - maps the event to ProjectionSingleCommand,
     - dispatches the command through MediatR.

  3. TransactionalCommandHandlerBase executes the version-aware UPSERT and stores the consumed Kafka offset
     in one EF transaction.

  4. Production consumers use the shared tiered Kafka retry pipeline:
     - main TransientException or IsolableException -> first retry tier,
     - retry TransientException -> next tier,
     - permanent, unknown, exhausted, or isolated single-message failure -> DLQ,
     - each move stores the outgoing message and source offset atomically through the Silverback EF outbox.

  5. The retry tiers use separate topics and RetryAtUtc metadata. No consumer uses Silverback's in-place
     policy.Retry(...), which is incompatible with the client-side Kafka offset-store scope lifecycle.

ProjectionBulk:
===============

  ProjectionBulk remains only in HarnessService as a test-oriented batch UPSERT mechanism. It consumes the main
  topic, deduplicates values by key and writes the batch transactionally. It has no retry consumer, retry producer
  or DLQ path. A failed batch rolls back the transaction and leaves its source offset uncommitted.
