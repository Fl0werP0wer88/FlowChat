using FlowChat.AuthService.Consumers.AuthApi.Contracts;

namespace FlowChat.AuthService.Consumers.Services;

public interface IAuthInternalApiClient
{
    Task ConfirmEmailAsync(
        AuthEmailConfirmationRequest request,
        CancellationToken cancellationToken);
}
