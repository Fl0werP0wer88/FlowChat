using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.SoftDeleteUserPresencePreferences;

public sealed class SoftDeleteUserPresencePreferencesCommandHandler(
    IUserPresencePreferencesWriteRepository userPresencePreferencesWriteRepository,
    IUnitOfWork unitOfWork)
    : TransactionalCommandHandlerBase<SoftDeleteUserPresencePreferencesCommand, Unit>(unitOfWork)
{
    protected override async Task<FlowChatResult<Unit>> HandleInTransactionAsync(
        SoftDeleteUserPresencePreferencesCommand request,
        CancellationToken cancellationToken)
    {
        var preferences = await userPresencePreferencesWriteRepository.GetByIdAsync(
            request.UserId,
            cancellationToken);

        if (preferences is not null)
        {
            await userPresencePreferencesWriteRepository.SoftDeleteAsync(preferences, cancellationToken);
        }

        return FlowChatResult<Unit>.Success(Unit.Value);
    }
}
