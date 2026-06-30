using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Domain.Entities.UserPresencePreferences;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.ChangeUserPresencePreferences;

public sealed class ChangeUserPresencePreferencesCommandHandler
    : AggregateRootUpsertCommandHandlerBaseV2<ChangeUserPresencePreferencesCommand, Unit, UserPresencePreferences>
{
    private readonly IUserPresencePreferencesWriteRepository _userPresencePreferencesWriteRepository;
    private UserPresencePreferences? _preferences;
    private bool _wasCreated;

    public ChangeUserPresencePreferencesCommandHandler(
        IUserPresencePreferencesWriteRepository userPresencePreferencesWriteRepository,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher localEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<ChangeUserPresencePreferencesCommand, UserPresencePreferences>> beforeSaveProcessors)
        : base(localEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _userPresencePreferencesWriteRepository = userPresencePreferencesWriteRepository;
    }

    protected override bool WasAggregateCreated => _wasCreated;

    protected override UserPresencePreferences GetAggregateRoot() =>
        _preferences ?? throw new InvalidOperationException("Aggregate root instance is not available.");

    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(
        ChangeUserPresencePreferencesCommand request,
        CancellationToken cancellationToken)
    {
        _preferences = await _userPresencePreferencesWriteRepository.GetByIdAsync(
            request.UserId,
            cancellationToken);

        if (_preferences is null)
        {
            _wasCreated = true;
            _preferences = UserPresencePreferences.Create(request.UserId, request.Status);
            await _userPresencePreferencesWriteRepository.AddAsync(_preferences, cancellationToken);
        }
        else
        {
            _wasCreated = false;
            _preferences.SetPreferredStatus(request.Status);
        }

        return FlowChatResult<Unit>.Success(Unit.Value);
    }
}
