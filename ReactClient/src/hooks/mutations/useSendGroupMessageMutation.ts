import { useMutation, useQueryClient } from "@tanstack/react-query";
import { sendGroupChatMessage } from "../../api/chatService";
import {
  type GroupConversationCacheEntry,
  createGroupMessage,
} from "../caches/groupConversationCache";
import { mergeSequencedMessage } from "../caches/sequencedMessageCache";

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
  onSequenceGap: () => void;
}

export function useSendGroupMessageMutation({
  accessToken,
  activeGroupConversationId,
  ownerUserId,
  onError,
  onSequenceGap,
}: UseSendGroupMessageMutationOptions) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ messageId, conversationId, text }: SendGroupMessageVariables) =>
      sendGroupChatMessage({ id: messageId, conversationId, text }, accessToken),
    onMutate: (variables) => {
      queryClient.setQueryData<GroupConversationCacheEntry>(
        ["groupConversation", activeGroupConversationId],
        (current) => current
          ? mergeSequencedMessage(
              current,
              createGroupMessage(
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
      queryClient.setQueryData<GroupConversationCacheEntry>(
        ["groupConversation", activeGroupConversationId],
        (current) => {
          if (!current) {
            return current;
          }

          const merged = mergeSequencedMessage(
            current,
            createGroupMessage(
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
          queryKey: ["groupConversation", activeGroupConversationId],
        });
      }
    },
    onError: (error, variables) => {
      queryClient.setQueryData<GroupConversationCacheEntry>(
        ["groupConversation", activeGroupConversationId],
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
