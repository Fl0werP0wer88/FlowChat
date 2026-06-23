namespace FlowChat.GatewayService.Api.Services;

public sealed record ContactClientDto(
    Guid Id,
    Guid OwnerUserId,
    Guid ContactUserId,
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Email,
    bool IsBlocked);

public interface ISocialGraphServiceClient
{
    Task<IReadOnlyList<ContactClientDto>> GetContactsAsync(CancellationToken cancellationToken);
}
