using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.DeleteContactObserverProjection;

public sealed class DeleteContactObserverProjectionCommandHandler(
    IContactObserverProjectionWriteRepository contactObserverProjectionWriteRepository,
    IUnitOfWork unitOfWork,
    IDomainEventDispatcher domainEventDispatcher)
    : CommandHandlerBase<DeleteContactObserverProjectionCommand, Unit>(domainEventDispatcher, unitOfWork)
{
    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(
        DeleteContactObserverProjectionCommand request,
        CancellationToken cancellationToken)
    {
        await contactObserverProjectionWriteRepository.DeleteAsync(
            request.ObservedUserId,
            request.ObserverUserId,
            cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Unit> result) => null;
}
