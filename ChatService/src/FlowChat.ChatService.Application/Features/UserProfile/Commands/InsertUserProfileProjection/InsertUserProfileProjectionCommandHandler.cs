using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
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
            AvatarUrl = NormalizeOptional(request.AvatarUrl),
            CreatedBy = request.CreatedBy!.Trim(),
            CreatedAtUtc = request.CreatedAtUtc,
            LastModifiedBy = request.LastModifiedBy!.Trim(),
            LastModifiedAtUtc = request.LastModifiedAtUtc
        };

        await userProfileProjectionWriteRepository.InsertAsync(projection, cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    protected override Task<(bool Found, Unit Value)> TryGetExistingResponseAsync(
        InsertUserProfileProjectionCommand request,
        CancellationToken cancellationToken)
        => Task.FromResult((true, Unit.Value));

    protected override string GetIdempotencyConflictKey(InsertUserProfileProjectionCommand request) =>
        InsertUserProfileProjectionCommand.IdempotencyConflictKey;

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
