Design assumptions:
    1. Kafka retry topic should by default be consumed one by one. This is preffered since if we initially consume 
    & process events in batches then one failed event will send whole batch to retry. So the retry should be able to identify failed event and process correct ones. To avoid over writting
    3. Write repositories suppose to operate on domain entities (Which are also EF entities) and modify whole aggregates atomicaly in one transaction.
    4. Read repositorie should not operate on Domain entities. EF should register separate EF-Entities for reads


Rules:
1. Projections tables should contain data same as on orgin service, so no mutations and no new artificial fields.


Cross-service projection system (how data flows from source service to read replicas):
=======================================================================================

Overview:
    A source service (e.g. UserProfileService) publishes integration events to a Kafka topic when its
    aggregate state changes (create / update / soft-delete). A consuming service (e.g. PresenceService,
    ChatService, SocialGraphService) maintains a local read-only projection table that mirrors the
    relevant fields from the source, updated by those events.

Components:

  1. Kafka subscriber (PresenceService.Consumers / ChatService.Consumers / etc.)
     - Inherits SubscriberBase<TIntegrationEvent> (Common/src/FlowChat.Shared.Infrastructure/Silverback/Subscribers)
     - Consumes batches of integration events from the main topic OR one-by-one from the retry topic
     - On success: calls the internal projection API endpoint
     - On TransientException: re-throws so Silverback routes the message to the retry topic
     - On IsolableException: re-throws so Silverback routes the WHOLE BATCH to the retry topic
       (where it will be consumed one-by-one, isolating the faulty event)
     - On NonTransientException: re-throws so Silverback routes the message directly to the DLQ

  2. Internal HTTP client (e.g. UserProfileInternalApiClient)
     - Inherits FlowChatHttpClientBase (Common/src/FlowChat.Shared.Infrastructure/Http)
     - Calls an internal API endpoint (authenticated with X-Internal-Api-Key) that triggers projection upsert
     - Reads the "failureKind" field from ProblemDetails response JSON and throws:
         "Transient"  -> TransientException  (retry with backoff)
         "Isolable"   -> IsolableException   (isolate batch, retry once per item)
         "None" / missing -> NonTransientException (go to DLQ)

  3. Internal API endpoint (e.g. POST /internal/projections/bulk-upsert)
     - Protected by internal API key
     - Dispatches a MediatR command to the Application layer
     - Returns ProblemDetails with "failureKind" extension on failure

  4. MediatR command handler
     - Calls the bulk repository to upsert the projection rows
     - ExceptionHandlingPipelineBehavior (Common/src/FlowChat.Shared.Application/Behaviors) translates
       infrastructure exceptions to DomainError with the appropriate FailureKind:
         TransientException -> FailureKind.Transient
         IsolableException  -> FailureKind.Isolable
         Other              -> FailureKind.None

  5. ProjectionBulkRepositoryBase (Common/src/FlowChat.Shared.Persistance/ProjectionBulk)
     - Executes a single atomic BulkInsertOrUpdateAsync (EFCore.BulkExtensions) for the entire batch
     - Uses "update where SourceVersion > existing.SourceVersion" to make upserts idempotent:
       an older/duplicate event never overwrites a newer projection row
     - On non-transient DbException (e.g. CHECK constraint violation caused by one bad item):
       wraps it as IsolableException so the batch is re-routed for per-item isolation

Kafka error routing (configured in KafkaConsumerEndpointConfigurationBuilderExtensions):

  Main topic (batch consumption):
    TransientException or IsolableException  ->  move entire batch to retry topic
    Everything else                          ->  move to DLQ

  Retry topic (one-by-one consumption):
    TransientException  ->  exponential backoff retry up to MaxRetryCount, then DLQ
    Everything else     ->  DLQ immediately (IsolableException included -- it has already been
                            isolated to a single event here, backoff retries would be pointless)

FailureKind enum (Common/src/FlowChat.Core/Domain/FailureKind.cs):
    None      - permanent failure, route to DLQ
    Transient - temporary infrastructure issue, retry with backoff
    Isolable  - batch may contain one bad item; move whole batch to retry so items are
                processed individually to separate the faulty one from the healthy ones
