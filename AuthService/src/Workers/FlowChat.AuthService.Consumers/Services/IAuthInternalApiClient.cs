using FlowChat.AuthService.Consumers.AuthApi.Contracts;

namespace FlowChat.AuthService.Consumers.Services;

public interface IAuthInternalApiClient
{
    Task ChangeAuthEmailAsync(
        AuthEmailChangeRequest request,
        CancellationToken cancellationToken);

    Task ConfirmEmailAsync(
        AuthEmailConfirmationRequest request,
        CancellationToken cancellationToken);
}
