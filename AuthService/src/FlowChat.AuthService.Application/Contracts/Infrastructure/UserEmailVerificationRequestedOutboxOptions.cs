using FlowChat.Messaging.Contracts.AuthService.Events;

namespace FlowChat.AuthService.Application.Contracts.Infrastructure;

public sealed class UserEmailVerificationRequestedOutboxOptions : IOutboxRepositoryOptions<UserEmailVerificationRequested>
{
    public const string SectionName = "Kafka:UserEmailVerificationRequestedProducer";
    public const string FallbackSectionName = "Kafka:UserCreatedProducer";

    public string Topic { get; set; } = "dev.flowchat.identity.user.v1";
    public Func<UserEmailVerificationRequested, string> KeySelector { get; private set; } = (message) => message.UserId.ToString();
}
