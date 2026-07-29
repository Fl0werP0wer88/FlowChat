import { useQuery } from "@tanstack/react-query";
import { openGroupConversation } from "../../api/gatewayService";
import type { GroupConversation } from "../../types/chat";
import {
  type GroupConversationCacheEntry,
  mapGroupConversationMessage,
} from "../caches/groupConversationCache";

export function useGroupConversationQuery(
  activeGroupConversation: GroupConversation | null,
  accessToken: string,
  ownerUserId: string | null,
) {
  return useQuery<GroupConversationCacheEntry>({
    queryKey: ["groupConversation", activeGroupConversation?.conversationId],
    queryFn: async ({ signal }) => {
      const result = await openGroupConversation(
        activeGroupConversation!.conversationId,
        accessToken,
        signal,
      );
      const orderedMessages = [...result.messages].reverse();
      return {
        conversationId: result.conversationId,
        name: result.name || activeGroupConversation!.name,
        participants: result.participants,
        messages: orderedMessages.map((msg) => mapGroupConversationMessage(msg, ownerUserId)),
        nextBeforeSequenceNum: result.nextBeforeSequenceNum,
        hasMore: result.hasMore,
        lastContiguousSequenceNum: result.currentSequenceNum,
        pendingMessagesBySequence: {},
        syncStatus: "idle",
      };
    },
    enabled: Boolean(activeGroupConversation && accessToken),
    staleTime: Infinity,
    gcTime: 10 * 60 * 1000,
  });
}
