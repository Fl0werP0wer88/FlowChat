import { useCallback, useEffect, useRef } from "react";

const DefaultDebounceMs = 500;

type MarkConversationAsReadCallback = (conversationId: string) => void | Promise<void>;

export function useDebouncedMarkConversationAsRead(debounceMs = DefaultDebounceMs) {
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const cancel = useCallback(() => {
    if (timerRef.current) {
      clearTimeout(timerRef.current);
      timerRef.current = null;
    }
  }, []);

  const schedule = useCallback((
    conversationId: string | null | undefined,
    markAsRead: MarkConversationAsReadCallback,
  ) => {
    if (!conversationId) {
      return;
    }

    cancel();
    timerRef.current = setTimeout(() => {
      timerRef.current = null;
      void markAsRead(conversationId);
    }, debounceMs);
  }, [cancel, debounceMs]);

  useEffect(() => cancel, [cancel]);

  return { schedule, cancel };
}
