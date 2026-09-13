import { hashKey, useQueryClient, type QueryKey } from '@tanstack/react-query';
import { useCallback } from 'react';

import {
  catchUpConversationMessages,
  type CatchUpConversationMessagesInput,
  type CatchUpConversationMessagesResponse,
} from '../api/catch-up-conversation-messages';

import {
  completeConversationMessageSnapshot,
  mergeConversationMessage,
  type ConversationMessageBufferState,
} from './conversation-message-buffer';
import { getConversationWorkspaceQueries } from './conversation-workspace-query-cache';

const retryDelays = [250, 500, 1000] as const;
const activeSynchronizations = new Map<string, Promise<number | null>>();

function delay(milliseconds: number) {
  return new Promise<void>((resolve) => {
    setTimeout(resolve, milliseconds);
  });
}

export async function fetchCatchUpPageWithRetry(
  input: CatchUpConversationMessagesInput,
): Promise<CatchUpConversationMessagesResponse> {
  let lastError: unknown;

  for (let attempt = 0; attempt <= retryDelays.length; attempt += 1) {
    try {
      return await catchUpConversationMessages(input);
    } catch (error) {
      lastError = error;
      const retryDelay = retryDelays[attempt];
      if (retryDelay === undefined) break;
      await delay(retryDelay);
    }
  }

  throw lastError;
}

export function runConversationMessageSyncSingleFlight(
  queryKey: QueryKey,
  operation: () => Promise<number | null>,
) {
  const synchronizationKey = hashKey(queryKey);
  const activeSynchronization = activeSynchronizations.get(synchronizationKey);
  if (activeSynchronization) return activeSynchronization;

  const synchronization = operation().finally(() => {
    if (activeSynchronizations.get(synchronizationKey) === synchronization) {
      activeSynchronizations.delete(synchronizationKey);
    }
  });
  activeSynchronizations.set(synchronizationKey, synchronization);

  return synchronization;
}

export function useConversationMessageSync() {
  const queryClient = useQueryClient();

  const synchronizeQuery = useCallback(
    (conversationId: string, queryKey: QueryKey) =>
      runConversationMessageSyncSingleFlight(queryKey, async () => {
        queryClient.setQueryData<ConversationMessageBufferState>(queryKey, (current) =>
          current ? { ...current, syncStatus: 'syncing' } : current,
        );

        try {
          while (true) {
            const snapshot = queryClient.getQueryData<ConversationMessageBufferState>(queryKey);
            if (!snapshot) return null;

            let afterSequenceNum = snapshot.lastContiguousSequenceNum;
            let throughSequenceNum: number | undefined;
            let hasMore = true;

            while (hasMore) {
              const page = await fetchCatchUpPageWithRetry({
                conversationId,
                afterSequenceNum,
                throughSequenceNum,
                limit: 100,
              });
              throughSequenceNum ??= page.throughSequenceNum;

              let requiresRefetch = false;
              for (const message of page.items) {
                queryClient.setQueryData<ConversationMessageBufferState>(queryKey, (current) => {
                  if (!current) return current;

                  const result = mergeConversationMessage(current, message);
                  requiresRefetch ||= result.requiresRefetch;
                  return result.state;
                });

                if (requiresRefetch) break;
              }

              if (requiresRefetch) {
                await queryClient.invalidateQueries({ queryKey, exact: true });
                return null;
              }

              hasMore = page.hasMore;
              if (hasMore) {
                if (page.nextAfterSequenceNum === null) {
                  throw new Error('Catch-up response is missing the next sequence cursor.');
                }
                afterSequenceNum = page.nextAfterSequenceNum;
              }
            }

            if (throughSequenceNum === undefined) return snapshot.lastContiguousSequenceNum;

            queryClient.setQueryData<ConversationMessageBufferState>(queryKey, (current) =>
              current ? completeConversationMessageSnapshot(current, throughSequenceNum) : current,
            );

            const completedSnapshot =
              queryClient.getQueryData<ConversationMessageBufferState>(queryKey);
            if (!completedSnapshot) return null;

            const needsAnotherSnapshot = Object.keys(
              completedSnapshot.pendingMessagesBySequence,
            ).some(
              (sequenceNum) =>
                Number(sequenceNum) > completedSnapshot.lastContiguousSequenceNum + 1,
            );
            if (!needsAnotherSnapshot) {
              return completedSnapshot.lastContiguousSequenceNum;
            }

            queryClient.setQueryData<ConversationMessageBufferState>(queryKey, (current) =>
              current ? { ...current, syncStatus: 'syncing' } : current,
            );
          }
        } catch {
          queryClient.setQueryData<ConversationMessageBufferState>(queryKey, (current) =>
            current ? { ...current, syncStatus: 'error' } : current,
          );
          return null;
        }
      }),
    [queryClient],
  );

  const synchronizeAll = useCallback(
    () =>
      Promise.all(
        getConversationWorkspaceQueries(queryClient).map(([queryKey, current]) =>
          synchronizeQuery(current.conversationId, queryKey),
        ),
      ),
    [queryClient, synchronizeQuery],
  );

  return { synchronizeQuery, synchronizeAll };
}
