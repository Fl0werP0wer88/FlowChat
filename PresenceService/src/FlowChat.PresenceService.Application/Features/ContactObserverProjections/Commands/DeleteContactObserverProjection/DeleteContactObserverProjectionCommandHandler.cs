using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.DeleteContactObserverProjection;

public sealed class DeleteContactObserverProjectionCommandHandler(
    IContactObserverProjectionWriteRepository contactObserverProjectionWriteRepository,
    IUnitOfWork unitOfWork)
    : TransactionalCommandHandlerBase<DeleteContactObserverProjectionCommand, Unit>(unitOfWork)
{
    protected override async Task<FlowChatResult<Unit>> HandleInTransactionAsync(
        DeleteContactObserverProjectionCommand request,
        CancellationToken cancellationToken)
    {
        await contactObserverProjectionWriteRepository.DeleteAsync(
            request.ObservedUserId,
            request.ObserverUserId,
            cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }
}
