import type { QueryClient, QueryKey } from '@tanstack/react-query';

import type { ConversationMessageBufferState } from './conversation-message-buffer';

export const conversationWorkspaceQueryKey = ['conversation-workspace'] as const;

export interface ConversationWorkspaceCacheEntry extends ConversationMessageBufferState {
  conversationId: string;
}

export function getConversationWorkspaceQueries(
  queryClient: QueryClient,
  conversationId?: string,
): Array<[QueryKey, ConversationWorkspaceCacheEntry]> {
  return queryClient
    .getQueriesData<ConversationWorkspaceCacheEntry>({ queryKey: conversationWorkspaceQueryKey })
    .filter(
      (entry): entry is [QueryKey, ConversationWorkspaceCacheEntry] =>
        entry[1] !== undefined &&
        (conversationId === undefined || entry[1].conversationId === conversationId),
    );
}

export async function invalidateConversationWorkspaceQueries(
  queryClient: QueryClient,
  conversationId: string,
) {
  const invalidations = getConversationWorkspaceQueries(queryClient, conversationId).map(
    ([queryKey]) => queryClient.invalidateQueries({ queryKey, exact: true }),
  );
  await Promise.all(invalidations);
}
