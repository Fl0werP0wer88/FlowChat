using FlowChat.Core.Domain;
using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Queries.GetUserPresencePreferences;

public sealed class GetUserPresencePreferencesQueryHandler(
    IUserPresencePreferencesRepository userPresencePreferencesRepository)
    : IRequestHandler<GetUserPresencePreferencesQuery, FlowChatResult<PresenceStatus?>>
{
    private readonly IUserPresencePreferencesRepository _userPresencePreferencesRepository =
        userPresencePreferencesRepository ?? throw new ArgumentNullException(nameof(userPresencePreferencesRepository));

    public async Task<FlowChatResult<PresenceStatus?>> Handle(
        GetUserPresencePreferencesQuery request,
        CancellationToken cancellationToken)
    {
        var status = await _userPresencePreferencesRepository.FindPreferredStatusAsync(
            request.UserId,
            cancellationToken);

        return FlowChatResult<PresenceStatus?>.Success(status);
    }
}
