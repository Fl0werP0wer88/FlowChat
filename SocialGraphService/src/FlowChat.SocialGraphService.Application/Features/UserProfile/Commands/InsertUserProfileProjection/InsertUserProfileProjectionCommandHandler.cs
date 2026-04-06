using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using MediatR;

namespace FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.InsertUserProfileProjection;

public sealed class InsertUserProfileProjectionCommandHandler
    : CommandHandlerBase<InsertUserProfileProjectionCommand, Unit>
{
    private readonly IUserProfileProjectionWriteRepository _userProfileProjectionWriteRepository;

    public InsertUserProfileProjectionCommandHandler(
        IUserProfileProjectionWriteRepository userProfileProjectionWriteRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher) : base(domainEventDispatcher, unitOfWork)
    {
        _userProfileProjectionWriteRepository = userProfileProjectionWriteRepository;
    }

    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(
        InsertUserProfileProjectionCommand request,
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
            request.IsEmailVisible,
            request.IsPhoneVisible);

        var wasInserted = await _userProfileProjectionWriteRepository.InsertAsync(projection, cancellationToken);
        if (!wasInserted)
        {
            return FlowChatResult<Unit>.Failure(DomainError.Conflict("User profile projection already exists."));
        }

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Unit> result) => null;

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
