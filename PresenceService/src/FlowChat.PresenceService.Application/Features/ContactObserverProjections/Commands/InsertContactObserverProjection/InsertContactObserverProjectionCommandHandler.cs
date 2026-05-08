using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.InsertContactObserverProjection;

public sealed class InsertContactObserverProjectionCommandHandler(
    IContactObserverProjectionWriteRepository contactObserverProjectionWriteRepository,
    IUnitOfWork unitOfWork,
    IDomainEventDispatcher domainEventDispatcher,
    IDbUpdateExceptionClassifier dbUpdateExceptionClassifier)
    : IdempotentCommandHandlerBase<InsertContactObserverProjectionCommand, Unit>(domainEventDispatcher, unitOfWork, dbUpdateExceptionClassifier)
{
    protected override async Task<FlowChatResult<Unit>> ExecuteCommandAsync(
        InsertContactObserverProjectionCommand request,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        await contactObserverProjectionWriteRepository.InsertAsync(
            new ContactObserverProjectionDto
            {
                ObservedUserId = request.ObservedUserId,
                ObserverUserId = request.ObserverUserId,
                CreatedAtUtc = now,
                LastModifiedAtUtc = now
            },
            cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    protected override Task<(bool Found, Unit Value)> TryGetExistingResponseAsync(
        InsertContactObserverProjectionCommand request,
        CancellationToken cancellationToken)
        => Task.FromResult((true, Unit.Value));

    protected override IAggregateRoot? GetExecutedAggregateRoot(IdempotentCommandResult<Unit> result) => null;

    protected override string GetIdempotencyConflictKey(InsertContactObserverProjectionCommand request) =>
        InsertContactObserverProjectionCommand.IdempotencyConflictKey;
}
