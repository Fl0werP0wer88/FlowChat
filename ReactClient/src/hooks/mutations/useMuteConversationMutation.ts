import { useMutation, useQueryClient } from "@tanstack/react-query";
import { muteConversation, unmuteConversation } from "../../api/chatService";
import type { Contact } from "../../types/contacts";

export function useMuteConversationMutation(accessToken: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ conversationId, muted }: { conversationId: string; muted: boolean }) =>
      muted ? muteConversation(conversationId, accessToken) : unmuteConversation(conversationId, accessToken),
    onSuccess: (_, { conversationId, muted }) => {
      queryClient.setQueryData<Contact[]>(["contacts"], (current = []) =>
        current.map((contact) => contact.conversationId === conversationId
          ? { ...contact, isMuted: muted }
          : contact));
    },
  });
}
