using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using MediatR;

namespace FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.UpdateUserProfileProjection;

public sealed class UpdateUserProfileProjectionCommandHandler
    : CommandHandlerBase<UpdateUserProfileProjectionCommand, Unit>
{
    private readonly IUserProfileProjectionWriteRepository _userProfileProjectionWriteRepository;

    public UpdateUserProfileProjectionCommandHandler(
        IUserProfileProjectionWriteRepository userProfileProjectionWriteRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher) : base(domainEventDispatcher, unitOfWork)
    {
        _userProfileProjectionWriteRepository = userProfileProjectionWriteRepository;
    }

    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(
        UpdateUserProfileProjectionCommand request,
        CancellationToken cancellationToken)
    {
        var projection = new UserProfileProjection(
            request.UserProfileId,
            request.FriendlyUserId!.Trim(),
            request.DisplayName!.Trim(),
            NormalizeOptional(request.MainEmail),
            NormalizeOptional(request.MainPhone),
            NormalizeOptional(request.AvatarUrl),
            NormalizeOptional(request.Bio),
            request.IsActive,
            request.LastSeenAtUtc,
            NormalizeOptional(request.FirstName),
            NormalizeOptional(request.LastName),
            NormalizeOptional(request.Organization));

        var wasUpdated = await _userProfileProjectionWriteRepository.UpdateAsync(projection, cancellationToken);
        if (!wasUpdated)
        {
            return FlowChatResult<Unit>.Failure(DomainError.NotFound("User profile projection was not found."));
        }

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Unit> result) => null;

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
