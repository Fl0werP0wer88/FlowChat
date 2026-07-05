import { useMutation, useQueryClient } from "@tanstack/react-query";
import { sendChatMessage } from "../../api/chatService";
import {
  type DuetConversationCacheEntry,
  createDuetMessage,
  sortDuetMessages,
} from "../caches/duetConversationCache";

interface SendMessageVariables {
  messageId: string;
  conversationId: string;
  text: string;
  senderDisplayName: string;
}

interface UseSendMessageMutationOptions {
  accessToken: string;
  activeContactUserId: string | undefined;
  ownerUserId: string | null;
  userLogin: string;
  onError: (message: string) => void;
}

export function useSendMessageMutation({
  accessToken,
  activeContactUserId,
  ownerUserId,
  userLogin,
  onError,
}: UseSendMessageMutationOptions) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ messageId, conversationId, text, senderDisplayName }: SendMessageVariables) =>
      sendChatMessage({ id: messageId, conversationId, senderDisplayName, text }, accessToken),
    onSuccess: (result, variables) => {
      queryClient.setQueryData<DuetConversationCacheEntry>(
        ["duetConversation", activeContactUserId],
        (current) => {
          if (!current) {
            return current;
          }

          if (current.messages.some((m) => m.id === result.messageId)) {
            return current;
          }

          return {
            ...current,
            messages: sortDuetMessages([
              ...current.messages,
              createDuetMessage(
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
