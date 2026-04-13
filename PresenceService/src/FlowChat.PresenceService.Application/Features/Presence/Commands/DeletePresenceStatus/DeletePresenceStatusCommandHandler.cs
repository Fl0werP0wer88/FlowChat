using FlowChat.PresenceService.Application.Contracts.Infrastructure;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.DeletePresenceStatus;

public sealed class DeletePresenceStatusCommandHandler(
    IPresenceStatusStore presenceStatusStore,
    IUnitOfWork unitOfWork,
    IDomainEventDispatcher domainEventDispatcher)
    : CommandHandlerBase<DeletePresenceStatusCommand, Unit>(domainEventDispatcher, unitOfWork)
{
    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(
        DeletePresenceStatusCommand request,
        CancellationToken cancellationToken)
    {
        await presenceStatusStore.DeleteAsync(request.UserId, cancellationToken);
        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Unit> result) => null;
}
