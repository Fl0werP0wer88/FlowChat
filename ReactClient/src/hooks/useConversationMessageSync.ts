import { useQueryClient, type QueryKey } from "@tanstack/react-query";
import { useCallback, useEffect } from "react";
import { catchUpConversationMessages } from "../api/chatService";
import type { ChatMessage, DuetConversationMessage } from "../types/chat";
import {
  completeSequenceSnapshot,
  mergeSequencedMessage,
  type SequencedMessageState,
} from "./caches/sequencedMessageCache";

const activeSynchronizations = new Map<string, Promise<number | null>>();
const retryDelays = [250, 500, 1000];

export async function fetchWithRetry(
  operation: () => ReturnType<typeof catchUpConversationMessages>,
) {
  let lastError: unknown;
  for (let attempt = 0; attempt <= retryDelays.length; attempt += 1) {
    try {
      return await operation();
    } catch (error) {
      lastError = error;
      if (attempt === retryDelays.length) break;
      await new Promise((resolve) => setTimeout(resolve, retryDelays[attempt]));
    }
  }
  throw lastError;
}

export function runMessageSyncSingleFlight(
  conversationId: string,
  operation: () => Promise<number | null>,
): Promise<number | null> {
  const running = activeSynchronizations.get(conversationId);
  if (running) return running;

  const synchronization = operation().finally(() => {
    activeSynchronizations.delete(conversationId);
  });
  activeSynchronizations.set(conversationId, synchronization);
  return synchronization;
}

interface UseConversationMessageSyncOptions<TState extends SequencedMessageState> {
  conversationId: string | null;
  queryKey: QueryKey;
  accessToken: string;
  mapMessage: (message: DuetConversationMessage) => ChatMessage;
}

export function useConversationMessageSync<TState extends SequencedMessageState>({
  conversationId,
  queryKey,
  accessToken,
  mapMessage,
}: UseConversationMessageSyncOptions<TState>) {
  const queryClient = useQueryClient();

  const synchronizeMessages = useCallback((): Promise<number | null> => {
    if (!conversationId || !accessToken) {
      return Promise.resolve(null);
    }

    return runMessageSyncSingleFlight(conversationId, async () => {
      queryClient.setQueryData<TState>(queryKey, (current) =>
        current ? { ...current, syncStatus: "syncing" } : current);

      try {
        let startAnotherSnapshot = true;
        while (startAnotherSnapshot) {
          const initial = queryClient.getQueryData<TState>(queryKey);
          if (!initial) return null;

          let afterSequenceNum = initial.lastContiguousSequenceNum;
          let throughSequenceNum: number | null = null;
          let hasMore = true;

          while (hasMore) {
            const page = await fetchWithRetry(() =>
              catchUpConversationMessages(
                conversationId,
                afterSequenceNum,
                throughSequenceNum,
                accessToken,
              ));
            if (page.throughSequenceNum === null) {
              throw new Error("Catch-up response did not include ThroughSequenceNum.");
            }
            throughSequenceNum = page.throughSequenceNum;
            let requiresRefetch = false;

            queryClient.setQueryData<TState>(queryKey, (current) => {
              if (!current) return current;
              let merged = current;
              for (const item of page.messages) {
                const result = mergeSequencedMessage(merged, mapMessage(item));
                merged = result.state;
                requiresRefetch ||= result.requiresRefetch;
              }
              return merged;
            });

            if (requiresRefetch) {
              await queryClient.invalidateQueries({ queryKey });
              return null;
            }

            hasMore = page.hasMore;
            afterSequenceNum = page.nextAfterSequenceNum ?? throughSequenceNum;
          }

          queryClient.setQueryData<TState>(queryKey, (current) =>
            current && throughSequenceNum !== null
              ? completeSequenceSnapshot(current, throughSequenceNum)
              : current);

          const completed = queryClient.getQueryData<TState>(queryKey);
          const pendingSequences = completed
            ? Object.keys(completed.pendingMessagesBySequence).map(Number)
            : [];
          startAnotherSnapshot = Boolean(
            completed &&
            pendingSequences.some((sequenceNum) =>
              sequenceNum > completed.lastContiguousSequenceNum + 1),
          );
        }

        return queryClient.getQueryData<TState>(queryKey)
          ?.lastContiguousSequenceNum ?? null;
      } catch {
        queryClient.setQueryData<TState>(queryKey, (current) =>
          current ? { ...current, syncStatus: "error" } : current);
        return null;
      }
    });
  }, [accessToken, conversationId, mapMessage, queryClient, queryKey]);

  useEffect(() => {
    if (conversationId) {
      void synchronizeMessages();
    }
  }, [conversationId, synchronizeMessages]);

  return synchronizeMessages;
}
