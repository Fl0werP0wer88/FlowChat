import { useQuery } from "@tanstack/react-query";
import type { Contact } from "../../types/contacts";
import { openDuetConversation } from "../../api/gatewayApi";
import {
  type ConversationCacheEntry,
  mapConversationMessage,
} from "./conversationCache";

export function useConversationQuery(
  activeContact: Contact | null,
  accessToken: string,
  ownerUserId: string | null,
) {
  return useQuery<ConversationCacheEntry>({
    queryKey: ["conversation", activeContact?.userId],
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
        messages: orderedMessages.map((msg) => mapConversationMessage(msg, ownerUserId)),
        nextBeforeSentAtUtc: result.nextBeforeSentAtUtc,
        nextBeforeMessageId: result.nextBeforeMessageId,
        hasMore: result.hasMore,
      };
    },
    enabled: Boolean(activeContact && accessToken),
    // Conversations are kept fresh via realtime events — disable background refetching
    staleTime: Infinity,
    gcTime: 10 * 60 * 1000,
  });
}
