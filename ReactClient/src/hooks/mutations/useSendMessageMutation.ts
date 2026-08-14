import { useMutation, useQueryClient } from "@tanstack/react-query";
import { sendChatMessage } from "../../api/chatService";
import {
  type DuetConversationCacheEntry,
  createDuetMessage,
} from "../caches/duetConversationCache";
import { mergeSequencedMessage } from "../caches/sequencedMessageCache";

interface SendMessageVariables {
  messageId: string;
  conversationId: string;
  text: string;
}

interface UseSendMessageMutationOptions {
  accessToken: string;
  activeContactUserId: string | undefined;
  ownerUserId: string | null;
  onError: (message: string) => void;
  onSequenceGap: () => void;
}

export function useSendMessageMutation({
  accessToken,
  activeContactUserId,
  ownerUserId,
  onError,
  onSequenceGap,
}: UseSendMessageMutationOptions) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ messageId, conversationId, text }: SendMessageVariables) =>
      sendChatMessage({ id: messageId, conversationId, text }, accessToken),
    onMutate: (variables) => {
      queryClient.setQueryData<DuetConversationCacheEntry>(
        ["duetConversation", activeContactUserId],
        (current) => current
          ? mergeSequencedMessage(
              current,
              createDuetMessage(
                "me",
                variables.text,
                new Date().toISOString(),
                variables.messageId,
                variables.conversationId,
                ownerUserId,
              ),
            ).state
          : current,
      );
    },
    onSuccess: (result, variables) => {
      let requiresRefetch = false;
      queryClient.setQueryData<DuetConversationCacheEntry>(
        ["duetConversation", activeContactUserId],
        (current) => {
          if (!current) {
            return current;
          }

          const merged = mergeSequencedMessage(
            current,
            createDuetMessage(
                "me",
                variables.text,
                result.sentAtUtc,
                result.messageId,
                variables.conversationId,
                ownerUserId,
                result.sequenceNum,
              ),
          );
          requiresRefetch = merged.requiresRefetch;
          if (merged.needsCatchUp) onSequenceGap();
          return merged.state;
        },
      );
      if (requiresRefetch) {
        void queryClient.invalidateQueries({
          queryKey: ["duetConversation", activeContactUserId],
        });
      }
    },
    onError: (error, variables) => {
      queryClient.setQueryData<DuetConversationCacheEntry>(
        ["duetConversation", activeContactUserId],
        (current) => current
          ? {
              ...current,
              messages: current.messages.filter(
                (message) => message.id !== variables.messageId,
              ),
            }
          : current,
      );
      onError(error instanceof Error ? error.message : "Nie udalo sie wyslac wiadomosci.");
    },
  });
}
