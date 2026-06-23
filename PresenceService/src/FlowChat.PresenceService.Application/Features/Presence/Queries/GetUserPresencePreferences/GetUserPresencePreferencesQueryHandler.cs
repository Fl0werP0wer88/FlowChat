using FlowChat.Core.Domain;
using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Queries.GetUserPresencePreferences;

public sealed class GetUserPresencePreferencesQueryHandler(
    IUserPresencePreferencesReadRepository userPresencePreferencesReadRepository)
    : IRequestHandler<GetUserPresencePreferencesQuery, FlowChatResult<PresenceStatus?>>
{
    private readonly IUserPresencePreferencesReadRepository _userPresencePreferencesReadRepository =
        userPresencePreferencesReadRepository ?? throw new ArgumentNullException(nameof(userPresencePreferencesReadRepository));

    public async Task<FlowChatResult<PresenceStatus?>> Handle(
        GetUserPresencePreferencesQuery request,
        CancellationToken cancellationToken)
    {
        var status = await _userPresencePreferencesReadRepository.FindPreferredStatusAsync(
            request.UserId,
            cancellationToken);

        return FlowChatResult<PresenceStatus?>.Success(status);
    }
}
