import { useMutation, useQueryClient } from "@tanstack/react-query";
import { sendGroupChatMessage } from "../api";
import { type GroupConversationCacheEntry, createGroupMessage, sortGroupMessages } from "./groupConversationCache";

interface SendGroupMessageVariables {
  messageId: string;
  conversationId: string;
  text: string;
  senderDisplayName: string;
}

interface UseSendGroupMessageMutationOptions {
  accessToken: string;
  activeGroupConversationId: string | undefined;
  ownerUserId: string | null;
  userLogin: string;
  onError: (message: string) => void;
}

export function useSendGroupMessageMutation({
  accessToken,
  activeGroupConversationId,
  ownerUserId,
  userLogin,
  onError,
}: UseSendGroupMessageMutationOptions) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ messageId, conversationId, text, senderDisplayName }: SendGroupMessageVariables) =>
      sendGroupChatMessage({ id: messageId, conversationId, senderDisplayName, text }, accessToken),
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
                userLogin,
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
