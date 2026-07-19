using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Domain.Entities.UserPresencePreferences;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.ChangeUserPresencePreferences;

public sealed class ChangeUserPresencePreferencesCommandHandler
    : AggregateRootUpsertCommandHandlerBaseV3<ChangeUserPresencePreferencesCommand, Unit, UserPresencePreferences>
{
    private readonly IUserPresencePreferencesWriteRepository _userPresencePreferencesWriteRepository;

    public ChangeUserPresencePreferencesCommandHandler(
        IUserPresencePreferencesWriteRepository userPresencePreferencesWriteRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher localEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<ChangeUserPresencePreferencesCommand, UserPresencePreferences>> beforeSaveProcessors)
        : base(localEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _userPresencePreferencesWriteRepository = userPresencePreferencesWriteRepository;
    }

    protected override async Task<FlowChatResult<UserPresencePreferences?>> FetchAggregateRootAsync(
        ChangeUserPresencePreferencesCommand request,
        CancellationToken cancellationToken)
    {
        var preferences = await _userPresencePreferencesWriteRepository.GetByIdAsync(
            request.UserId,
            cancellationToken);

        return FlowChatResult<UserPresencePreferences?>.Success(preferences);
    }

    protected override async Task<FlowChatResult<AggregateMutation<Unit>>> ExecuteAsync(
        ChangeUserPresencePreferencesCommand request,
        CancellationToken cancellationToken)
    {
        var mutationType = FlowChat.Shared.Domain.MutationType.Unchanged;
        if (AggregateRoot is null)
        {
            AggregateRoot = UserPresencePreferences.Create(request.UserId, request.Status);
            await _userPresencePreferencesWriteRepository.AddAsync(AggregateRoot, cancellationToken);
            mutationType = FlowChat.Shared.Domain.MutationType.Created;
        }
        else
        {
            AggregateRoot.SetPreferredStatus(request.Status);
            mutationType = FlowChat.Shared.Domain.MutationType.Updated;
        }

        return Mutation(mutationType, Unit.Value);
    }
}
