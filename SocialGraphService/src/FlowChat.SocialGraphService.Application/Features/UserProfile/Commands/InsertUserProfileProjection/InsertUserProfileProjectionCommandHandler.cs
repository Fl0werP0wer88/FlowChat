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
        var projection = new UserProfileProjectionDto
        {
            UserProfileId = request.UserProfileId,
            FriendlyUserId = request.FriendlyUserId!.Trim(),
            FirstName = NormalizeOptional(request.FirstName),
            LastName = NormalizeOptional(request.LastName),
            Organization = NormalizeOptional(request.Organization),
            MainEmail = CreateMainEmail(
                request.MainEmailAddress,
                request.MainEmailIsConfirmed,
                request.MainEmailIsVisible),
            MainPhone = CreateMainPhone(
                request.MainPhoneNumber,
                request.MainPhoneIsConfirmed,
                request.MainPhoneIsVisible),
            AvatarUrl = NormalizeOptional(request.AvatarUrl),
            Bio = NormalizeOptional(request.Bio),
            IsActive = request.IsActive,
            LastSeenAtUtc = request.LastSeenAtUtc
        };

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

    private static UserProfileProjectionEmailDto? CreateMainEmail(
        string? address,
        bool? isConfirmed,
        bool? isVisible)
    {
        var normalizedAddress = NormalizeOptional(address);
        return normalizedAddress == null
            ? null
            : new UserProfileProjectionEmailDto
            {
                Address = normalizedAddress,
                IsConfirmed = isConfirmed ?? false,
                IsVisible = isVisible ?? false
            };
    }

    private static UserProfileProjectionPhoneDto? CreateMainPhone(
        string? number,
        bool? isConfirmed,
        bool? isVisible)
    {
        var normalizedNumber = NormalizeOptional(number);
        return normalizedNumber == null
            ? null
            : new UserProfileProjectionPhoneDto
            {
                Number = normalizedNumber,
                IsConfirmed = isConfirmed ?? false,
                IsVisible = isVisible ?? false
            };
    }
}

