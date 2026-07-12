import { useMutation, useQueryClient } from "@tanstack/react-query";
import { hideConversation } from "../../api/chatService";
import type { Contact } from "../../types/contacts";

export function useHideConversationMutation(accessToken: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (conversationId: string) => hideConversation(conversationId, accessToken),
    onSuccess: (_, conversationId) => {
      queryClient.setQueryData<Contact[]>(["contacts"], (current = []) =>
        current.filter((contact) => contact.conversationId !== conversationId));
    },
  });
}
