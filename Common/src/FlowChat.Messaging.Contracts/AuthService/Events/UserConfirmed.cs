namespace FlowChat.Messaging.Contracts.AuthService.Events
{
    public sealed class UserConfirmed
    {
        public Guid UserId { get; init; }
    }
}