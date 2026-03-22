using FlowChat.UserProfileService.Consumers.UserProfileApi.Contracts;

namespace FlowChat.UserProfileService.Consumers.Services;

public interface IUserProfileInternalApiClient
{
    Task CreateInitialUserProfileAsync(
        CreateInitialUserProfileRequest request,
        CancellationToken cancellationToken);
}
