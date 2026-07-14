import { useMutation, useQueryClient } from "@tanstack/react-query";
import { blockConversation, unblockConversation } from "../../api/chatService";
import type { Contact } from "../../types/contacts";

export function useBlockConversationMutation(accessToken: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ conversationId, blocked }: { conversationId: string; blocked: boolean }) =>
      blocked ? blockConversation(conversationId, accessToken) : unblockConversation(conversationId, accessToken),
    onSuccess: (_, { conversationId, blocked }) => {
      queryClient.setQueryData<Contact[]>(["contacts"], (current = []) =>
        current.map((contact) => contact.conversationId === conversationId
          ? { ...contact, isBlocked: blocked }
          : contact));
    },
  });
}
