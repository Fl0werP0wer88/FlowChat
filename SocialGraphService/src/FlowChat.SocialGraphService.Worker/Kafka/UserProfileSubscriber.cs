using FlowChat.Application.Abstractions;
using FlowChat.Messaging.Contracts.UserProfileService.Events;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.UserProfiles;
using Silverback.Messaging.Subscribers;

namespace FlowChat.SocialGraphService.Worker.Kafka;

public sealed class UserProfileSubscriber(
    IUserProfileReadModelRepository userProfileReadModelRepository,
    IUnitOfWork unitOfWork,
    ILogger<UserProfileSubscriber> logger)
{
    private readonly IUserProfileReadModelRepository _userProfileReadModelRepository = userProfileReadModelRepository
        ?? throw new ArgumentNullException(nameof(userProfileReadModelRepository));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly ILogger<UserProfileSubscriber> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    [Subscribe]
    public Task HandleAsync(
        UserProfileCreatedIntegrationEvent message,
        CancellationToken cancellationToken) =>
        UpsertAsync(
            Map(message),
            nameof(UserProfileCreatedIntegrationEvent),
            cancellationToken);

    [Subscribe]
    public Task HandleAsync(
        UserProfileStateChangedIntegrationEvent message,
        CancellationToken cancellationToken) =>
        UpsertAsync(
            Map(message),
            nameof(UserProfileStateChangedIntegrationEvent),
            cancellationToken);

    private async Task UpsertAsync(
        UserProfileReadModel readModel,
        string eventType,
        CancellationToken cancellationToken)
    {
        Validate(readModel);

        var created = await _userProfileReadModelRepository.UpsertAsync(readModel, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "{Operation} user profile read model for profile {UserProfileId} from {EventType}.",
            created ? "Created" : "Updated",
            readModel.UserProfileId,
            eventType);
    }

    private static UserProfileReadModel Map(UserProfileCreatedIntegrationEvent message) =>
        new(
            message.UserProfileId,
            NormalizeRequired(message.UserName, nameof(message.UserName)),
            NormalizeRequired(message.DisplayName, nameof(message.DisplayName)),
            NormalizeOptional(message.MainEmail),
            NormalizeOptional(message.MainPhone),
            NormalizeOptional(message.AvatarUrl),
            NormalizeOptional(message.Bio),
            message.IsActive,
            message.LastSeenAtUtc,
            message.IsEmailVisible,
            message.IsPhoneVisible);

    private static UserProfileReadModel Map(UserProfileStateChangedIntegrationEvent message) =>
        new(
            message.UserProfileId,
            NormalizeRequired(message.UserName, nameof(message.UserName)),
            NormalizeRequired(message.DisplayName, nameof(message.DisplayName)),
            NormalizeOptional(message.MainEmail),
            NormalizeOptional(message.MainPhone),
            NormalizeOptional(message.AvatarUrl),
            NormalizeOptional(message.Bio),
            message.IsActive,
            message.LastSeenAtUtc,
            message.IsEmailVisible,
            message.IsPhoneVisible);

    private static void Validate(UserProfileReadModel readModel)
    {
        if (readModel.UserProfileId == Guid.Empty)
        {
            throw new InvalidOperationException("Payload does not contain valid UserProfileId.");
        }
    }

    private static string NormalizeRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Payload does not contain valid {fieldName}.");
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
