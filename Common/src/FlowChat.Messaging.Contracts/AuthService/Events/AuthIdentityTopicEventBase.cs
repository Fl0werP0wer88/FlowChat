using System.Text.Json.Serialization;

namespace FlowChat.Messaging.Contracts.AuthService.Events;

[
    JsonPolymorphic(TypeDiscriminatorPropertyName = "eventType"),
    JsonDerivedType(typeof(UserCreatedIntegrationEvent), typeDiscriminator: "user-created"),
    JsonDerivedType(typeof(UserConfirmedIntegrationEvent), typeDiscriminator: "user-confirmed"),
    JsonDerivedType(typeof(EmailVerificationRequestIntegrationEvent), typeDiscriminator: "email-verification-requested")
]
public abstract class AuthIdentityTopicEventBase : IntegrationEventBase
{
}
