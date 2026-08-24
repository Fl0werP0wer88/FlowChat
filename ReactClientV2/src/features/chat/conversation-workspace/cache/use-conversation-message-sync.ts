import { useQueryClient, type QueryKey } from '@tanstack/react-query';
import { useCallback, useEffect, useRef } from 'react';

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
  conversationId: string,
  operation: () => Promise<number | null>,
) {
  const activeSynchronization = activeSynchronizations.get(conversationId);
  if (activeSynchronization) return activeSynchronization;

  const synchronization = operation().finally(() => {
    if (activeSynchronizations.get(conversationId) === synchronization) {
      activeSynchronizations.delete(conversationId);
    }
  });
  activeSynchronizations.set(conversationId, synchronization);

  return synchronization;
}

export interface UseConversationMessageSyncOptions {
  conversationId: string | null;
  queryKey: QueryKey;
  enabled: boolean;
}

export function useConversationMessageSync<
  TState extends ConversationMessageBufferState = ConversationMessageBufferState,
>({ conversationId, queryKey, enabled }: UseConversationMessageSyncOptions) {
  const queryClient = useQueryClient();
  const queryKeyRef = useRef(queryKey);

  useEffect(() => {
    queryKeyRef.current = queryKey;
  }, [queryKey]);

  const synchronizeMessages = useCallback(() => {
    if (!enabled || !conversationId) return Promise.resolve(null);

    return runConversationMessageSyncSingleFlight(conversationId, async () => {
      const activeQueryKey = queryKeyRef.current;
      queryClient.setQueryData<TState>(activeQueryKey, (current) =>
        current ? { ...current, syncStatus: 'syncing' } : current,
      );

      try {
        while (true) {
          const snapshot = queryClient.getQueryData<TState>(activeQueryKey);
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
              queryClient.setQueryData<TState>(activeQueryKey, (current) => {
                if (!current) return current;

                const result = mergeConversationMessage(current, message);
                requiresRefetch ||= result.requiresRefetch;
                return result.state;
              });

              if (requiresRefetch) break;
            }

            if (requiresRefetch) {
              await queryClient.invalidateQueries({ queryKey: activeQueryKey, exact: true });
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

          queryClient.setQueryData<TState>(activeQueryKey, (current) =>
            current ? completeConversationMessageSnapshot(current, throughSequenceNum) : current,
          );

          const completedSnapshot = queryClient.getQueryData<TState>(activeQueryKey);
          if (!completedSnapshot) return null;

          const needsAnotherSnapshot = Object.keys(
            completedSnapshot.pendingMessagesBySequence,
          ).some(
            (sequenceNum) => Number(sequenceNum) > completedSnapshot.lastContiguousSequenceNum + 1,
          );
          if (!needsAnotherSnapshot) {
            return completedSnapshot.lastContiguousSequenceNum;
          }

          queryClient.setQueryData<TState>(activeQueryKey, (current) =>
            current ? { ...current, syncStatus: 'syncing' } : current,
          );
        }
      } catch {
        queryClient.setQueryData<TState>(activeQueryKey, (current) =>
          current ? { ...current, syncStatus: 'error' } : current,
        );
        return null;
      }
    });
  }, [conversationId, enabled, queryClient]);

  useEffect(() => {
    if (enabled && conversationId) void synchronizeMessages();
  }, [conversationId, enabled, synchronizeMessages]);

  return synchronizeMessages;
}
