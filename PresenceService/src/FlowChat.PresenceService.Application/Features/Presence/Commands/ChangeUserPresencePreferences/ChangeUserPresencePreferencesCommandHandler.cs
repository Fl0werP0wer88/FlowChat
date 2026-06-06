using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Domain.Entities.UserPresencePreferences;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.ChangeUserPresencePreferences;

public sealed class ChangeUserPresencePreferencesCommandHandler(
    IUserPresencePreferencesWriteRepository userPresencePreferencesWriteRepository,
    IUnitOfWork unitOfWork)
    : TransactionalCommandHandlerBase<ChangeUserPresencePreferencesCommand, Unit>(unitOfWork)
{
    protected override async Task<FlowChatResult<Unit>> HandleInTransactionAsync(
        ChangeUserPresencePreferencesCommand request,
        CancellationToken cancellationToken)
    {
        var preferences = await userPresencePreferencesWriteRepository.GetByIdAsync(
            request.UserId,
            cancellationToken);

        if (preferences is null)
        {
            await userPresencePreferencesWriteRepository.AddAsync(
                UserPresencePreferences.Create(request.UserId, request.Status),
                cancellationToken);
        }
        else
        {
            preferences.SetPreferredStatus(request.Status);
        }

        return FlowChatResult<Unit>.Success(Unit.Value);
    }
}
