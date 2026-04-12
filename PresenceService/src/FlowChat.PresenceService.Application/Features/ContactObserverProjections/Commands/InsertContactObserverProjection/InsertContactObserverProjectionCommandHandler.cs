using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.InsertContactObserverProjection;

public sealed class InsertContactObserverProjectionCommandHandler(
    IContactObserverProjectionWriteRepository contactObserverProjectionWriteRepository,
    IUnitOfWork unitOfWork,
    IDomainEventDispatcher domainEventDispatcher)
    : CommandHandlerBase<InsertContactObserverProjectionCommand, Unit>(domainEventDispatcher, unitOfWork)
{
    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(
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

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Unit> result) => null;
}
