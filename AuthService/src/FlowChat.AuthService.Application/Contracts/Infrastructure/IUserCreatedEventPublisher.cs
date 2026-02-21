using FlowChat.AuthService.Application.Models;

namespace FlowChat.AuthService.Application.Contracts.Infrastructure;

public interface IUserCreatedEventPublisher
{
    Task PublishAsync(UserCreatedEvent message, CancellationToken cancellationToken);
}
