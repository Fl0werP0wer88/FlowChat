using CSharpFunctionalExtensions;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Domain.Enums;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationChanged;

public sealed class RouteGroupConversationChangedCommandHandler(
    IRealtimeEventRouter realtimeEventRouter,
    IRealtimeGroupMembershipReadModelRepository realtimeGroupMembershipReadModelRepository)
    : ICommandHandler<RouteGroupConversationChangedCommand, Unit>
{
    private readonly IRealtimeEventRouter _realtimeEventRouter = realtimeEventRouter
        ?? throw new ArgumentNullException(nameof(realtimeEventRouter));
    private readonly IRealtimeGroupMembershipReadModelRepository _realtimeGroupMembershipReadModelRepository = realtimeGroupMembershipReadModelRepository
        ?? throw new ArgumentNullException(nameof(realtimeGroupMembershipReadModelRepository));

    public async Task<FlowChatResult<Unit>> Handle(
        RouteGroupConversationChangedCommand request,
        CancellationToken cancellationToken)
    {
        var participantUserIds = await _realtimeGroupMembershipReadModelRepository.GetUserIdsByResourceIdAsync(
            RealtimeGroupType.Conversation,
            request.ConversationId,
            cancellationToken);

        //Review: to chyba nie potrzebuje participantUserIds mielismy wywalic to z notyfikacji GroupConversationChanged
        var notification = new GroupConversationChangedParam(
            request.ConversationId,
            request.Type,
            request.Name,
            request.CreatedByUserId,
            participantUserIds);

        await _realtimeEventRouter.RouteGroupConversationChangedAsync(notification, cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }
}
