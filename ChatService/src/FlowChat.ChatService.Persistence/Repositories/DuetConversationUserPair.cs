namespace FlowChat.ChatService.Persistence.Repositories;

internal static class DuetConversationUserPair
{
    public static (Guid First, Guid Second) Normalize(Guid userId1, Guid userId2) =>
        userId1 < userId2 ? (userId1, userId2) : (userId2, userId1);
}
