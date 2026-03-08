using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities;
using MediatR;

namespace FlowChat.UserProfileService.Application.UserProfiles.Commands.CreateInitialUserProfile;

public sealed class CreateInitialUserProfileCommandHandler
    : IRequestHandler<CreateInitialUserProfileCommand, Guid>
{
    private readonly IUserProfileRepository _userProfileRepository;

    public CreateInitialUserProfileCommandHandler(IUserProfileRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;
    }

    public async Task<Guid> Handle(
        CreateInitialUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        var userName = request.UserName.Trim();
        var displayName = request.DisplayName.Trim();

        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new ArgumentException("UserName is required.", nameof(request.UserName));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("DisplayName is required.", nameof(request.DisplayName));
        }

        var exists = await _userProfileRepository
            .UserNameExistsAsync(userName, cancellationToken: cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"UserName '{userName}' already exists.");
        }

        var entity = new UserProfile
        {
            Id = request.UserId,
            UserName = userName,
            DisplayName = displayName,
            AvatarUrl = request.AvatarUrl?.Trim(),
            Bio = request.Bio?.Trim(),
            IsActive = true,
            CreatedBy = "application"
        };

        await _userProfileRepository.AddAsync(entity, cancellationToken);

        return entity.Id;
    }
}
