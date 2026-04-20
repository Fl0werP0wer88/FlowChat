using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.ChatService.Application.Features.UserProfile.Commands.UpdateUserProfileProjection;

public sealed class UpdateUserProfileProjectionCommandHandler(
    IUserProfileProjectionWriteRepository userProfileProjectionWriteRepository,
    IUnitOfWork unitOfWork,
    IDomainEventDispatcher domainEventDispatcher)
    : CommandHandlerBase<UpdateUserProfileProjectionCommand, Unit>(domainEventDispatcher, unitOfWork)
{
    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(
        UpdateUserProfileProjectionCommand request,
        CancellationToken cancellationToken)
    {
        var projection = new UserProfileProjectionDto
        {
            UserProfileId = request.UserProfileId,
            FriendlyUserId = request.FriendlyUserId!.Trim(),
            DisplayName = NormalizeOptional(request.DisplayName),
            AvatarUrl = NormalizeOptional(request.AvatarUrl)
        };

        var wasUpdated = await userProfileProjectionWriteRepository.UpdateAsync(projection, cancellationToken);
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
