import { useQuery } from "@tanstack/react-query";
import type { Contact } from "../../types/contacts";
import { openDuetConversation } from "../../api/gatewayService";
import {
  type DuetConversationCacheEntry,
  mapDuetConversationMessage,
} from "../caches/duetConversationCache";

export function useDuetConversationQuery(
  activeContact: Contact | null,
  accessToken: string,
  ownerUserId: string | null,
) {
  return useQuery<DuetConversationCacheEntry>({
    queryKey: ["duetConversation", activeContact?.userId],
    queryFn: async ({ signal }) => {
      const result = await openDuetConversation(
        activeContact!.userId,
        activeContact!.conversationId,
        accessToken,
        signal,
      );
      const orderedMessages = [...result.messages].reverse();
      return {
        conversationId: result.conversationId,
        participants: result.participants,
        messages: orderedMessages.map((msg) => mapDuetConversationMessage(msg, ownerUserId)),
        nextBeforeSentAtUtc: result.nextBeforeSentAtUtc,
        nextBeforeMessageId: result.nextBeforeMessageId,
        hasMore: result.hasMore,
      };
    },
    enabled: Boolean(activeContact && accessToken),
    // Duet conversations are kept fresh via realtime events, so background refetching stays disabled
    staleTime: Infinity,
    gcTime: 10 * 60 * 1000,
  });
}
