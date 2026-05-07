using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.ChatService.Application.Features.UserProfile.Commands.InsertUserProfileProjection;

public sealed class InsertUserProfileProjectionCommandHandler(
    IUserProfileProjectionWriteRepository userProfileProjectionWriteRepository,
    IUnitOfWork unitOfWork,
    IDomainEventDispatcher domainEventDispatcher,
    IDbUpdateExceptionClassifier dbUpdateExceptionClassifier)
    : IdempotentCommandHandlerBase<InsertUserProfileProjectionCommand, Unit>(domainEventDispatcher, unitOfWork, dbUpdateExceptionClassifier)
{
    protected override async Task<FlowChatResult<Unit>> ExecuteCommandAsync(
        InsertUserProfileProjectionCommand request,
        CancellationToken cancellationToken)
    {
        var projection = new UserProfileProjectionDto
        {
            UserProfileId = request.UserProfileId,
            FriendlyUserId = request.FriendlyUserId!.Trim(),
            DisplayName = NormalizeOptional(request.DisplayName),
            AvatarUrl = NormalizeOptional(request.AvatarUrl)
        };

        await userProfileProjectionWriteRepository.InsertAsync(projection, cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    protected override async Task<(bool Found, Unit Value)> TryGetExistingResponseAsync(
        InsertUserProfileProjectionCommand request,
        CancellationToken cancellationToken)
    {
        var exists = await userProfileProjectionWriteRepository.ExistsAsync(request.UserProfileId, cancellationToken);
        return (exists, Unit.Value);
    }

    protected override IAggregateRoot? GetExecutedAggregateRoot(IdempotentCommandResult<Unit> result) => null;

    protected override string GetIdempotencyConflictKey(InsertUserProfileProjectionCommand request) =>
        InsertUserProfileProjectionCommand.IdempotencyConflictKey;

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
