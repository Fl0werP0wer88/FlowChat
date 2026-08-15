import type { QueryClient } from "@tanstack/react-query";
import type { GroupConversationCacheEntry } from "./caches/groupConversationCache";

export function resolveGroupConversationReadSequence(
  queryClient: QueryClient,
  conversationId: string,
  sequenceNum?: number,
): number {
  return sequenceNum
    ?? queryClient.getQueryData<GroupConversationCacheEntry>([
      "groupConversation",
      conversationId,
    ])?.lastContiguousSequenceNum
    ?? 0;
}
