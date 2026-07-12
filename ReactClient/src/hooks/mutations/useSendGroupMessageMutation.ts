import { useMutation, useQueryClient } from "@tanstack/react-query";
import { sendGroupChatMessage } from "../../api/chatService";
import {
  type GroupConversationCacheEntry,
  createGroupMessage,
  sortGroupMessages,
} from "../caches/groupConversationCache";

interface SendGroupMessageVariables {
  messageId: string;
  conversationId: string;
  text: string;
}

interface UseSendGroupMessageMutationOptions {
  accessToken: string;
  activeGroupConversationId: string | undefined;
  ownerUserId: string | null;
  onError: (message: string) => void;
}

export function useSendGroupMessageMutation({
  accessToken,
  activeGroupConversationId,
  ownerUserId,
  onError,
}: UseSendGroupMessageMutationOptions) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ messageId, conversationId, text }: SendGroupMessageVariables) =>
      sendGroupChatMessage({ id: messageId, conversationId, text }, accessToken),
    onSuccess: (result, variables) => {
      queryClient.setQueryData<GroupConversationCacheEntry>(
        ["groupConversation", activeGroupConversationId],
        (current) => {
          if (!current) {
            return current;
          }

          if (current.messages.some((m) => m.id === result.messageId)) {
            return current;
          }

          return {
            ...current,
            messages: sortGroupMessages([
              ...current.messages,
              createGroupMessage(
                "me",
                variables.text,
                result.sentAtUtc,
                result.messageId,
                variables.conversationId,
                ownerUserId,
              ),
            ]),
          };
        },
      );
    },
    onError: (error) => {
      onError(error instanceof Error ? error.message : "Nie udalo sie wyslac wiadomosci.");
    },
  });
}
