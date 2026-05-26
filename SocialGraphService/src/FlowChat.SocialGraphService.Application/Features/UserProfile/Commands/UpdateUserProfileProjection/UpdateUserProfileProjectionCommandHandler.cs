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

        var wasUpdated = await _userProfileProjectionWriteRepository.UpdateAsync(projection, cancellationToken);
        if (!wasUpdated)
        {
            return FlowChatResult<Unit>.Failure(DomainError.NotFound("User profile projection was not found."));
        }

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

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

