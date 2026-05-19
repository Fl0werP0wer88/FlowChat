import { useEffect, useLayoutEffect, useRef, type KeyboardEvent } from "react";
import { Button } from "../../../components/atoms/Button";
import { TextArea } from "../../../components/atoms/TextArea";
import type { ChatMessage } from "../../../types/chat";
import type { Contact } from "../../../types/contacts";
import { formatLocalTime } from "../../../utils/dateUtils";

interface ConversationPanelProps {
  activeContact: Contact | null;
  activeConversationId: string | null;
  conversationError: string | null;
  isLoadingConversation: boolean;
  isSendingMessage: boolean;
  hasOlderMessages: boolean;
  isLoadingOlderMessages: boolean;
  sendError: string | null;
  olderMessagesError: string | null;
  messages: ChatMessage[];
  draft: string;
  onDraftChange: (value: string) => void;
  onDraftKeyDown: (event: KeyboardEvent<HTMLTextAreaElement>) => void;
  onSendDraft: () => Promise<void>;
  onLoadOlderMessages: () => Promise<void>;
}

export function ConversationPanel({
  activeContact,
  activeConversationId,
  conversationError,
  isLoadingConversation,
  isSendingMessage,
  hasOlderMessages,
  isLoadingOlderMessages,
  sendError,
  olderMessagesError,
  messages,
  draft,
  onDraftChange,
  onDraftKeyDown,
  onSendDraft,
  onLoadOlderMessages,
}: ConversationPanelProps) {
  const historyRef = useRef<HTMLDivElement | null>(null);
  const messagesEndRef = useRef<HTMLDivElement | null>(null);
  const olderScrollSnapshotRef = useRef<{ scrollHeight: number; scrollTop: number } | null>(null);
  const previousConversationIdRef = useRef<string | null>(null);
  const previousLastMessageIdRef = useRef<string | null>(null);
  const isNearBottomRef = useRef(true);
  const isRequestingOlderMessagesRef = useRef(false);
  const isComposerDisabled = !activeConversationId || isLoadingConversation || isSendingMessage;
  const isSendDisabled = isComposerDisabled || draft.trim().length === 0;
  const lastMessageId = messages.at(-1)?.id ?? null;

  useLayoutEffect(() => {
    const history = historyRef.current;
    const olderSnapshot = olderScrollSnapshotRef.current;

    if (history && olderSnapshot) {
      history.scrollTop = olderSnapshot.scrollTop + (history.scrollHeight - olderSnapshot.scrollHeight);
      isNearBottomRef.current = history.scrollHeight - history.scrollTop - history.clientHeight < 80;
      olderScrollSnapshotRef.current = null;
      isRequestingOlderMessagesRef.current = false;
      previousConversationIdRef.current = activeConversationId;
      previousLastMessageIdRef.current = lastMessageId;
      return;
    }

    const previousConversationId = previousConversationIdRef.current;
    const previousLastMessageId = previousLastMessageIdRef.current;
    const isOpeningConversation = activeConversationId && activeConversationId !== previousConversationId;
    const hasNewBottomMessage = lastMessageId && lastMessageId !== previousLastMessageId;
    const isNearBottom = isNearBottomRef.current;

    if (isOpeningConversation || (hasNewBottomMessage && isNearBottom)) {
      messagesEndRef.current?.scrollIntoView({ block: "end" });
      isNearBottomRef.current = true;
    }

    previousConversationIdRef.current = activeConversationId;
    previousLastMessageIdRef.current = lastMessageId;
  }, [activeConversationId, lastMessageId, messages.length]);

  useEffect(() => {
    if (!activeConversationId) {
      previousConversationIdRef.current = null;
      previousLastMessageIdRef.current = null;
      olderScrollSnapshotRef.current = null;
      isNearBottomRef.current = true;
      isRequestingOlderMessagesRef.current = false;
    }
  }, [activeConversationId]);

  const handleHistoryScroll = () => {
    const history = historyRef.current;
    if (history) {
      isNearBottomRef.current = history.scrollHeight - history.scrollTop - history.clientHeight < 80;
    }

    if (
      !history
      || history.scrollTop > 48
      || !hasOlderMessages
      || isLoadingOlderMessages
      || isRequestingOlderMessagesRef.current
    ) {
      return;
    }

    isRequestingOlderMessagesRef.current = true;
    olderScrollSnapshotRef.current = {
      scrollHeight: history.scrollHeight,
      scrollTop: history.scrollTop,
    };

    void onLoadOlderMessages().finally(() => {
      requestAnimationFrame(() => {
        const snapshot = olderScrollSnapshotRef.current;
        const currentHistory = historyRef.current;
        if (!snapshot || !currentHistory) {
          isRequestingOlderMessagesRef.current = false;
          return;
        }

        currentHistory.scrollTop = snapshot.scrollTop + (currentHistory.scrollHeight - snapshot.scrollHeight);
        isNearBottomRef.current = currentHistory.scrollHeight - currentHistory.scrollTop - currentHistory.clientHeight < 80;
        olderScrollSnapshotRef.current = null;
        isRequestingOlderMessagesRef.current = false;
      });
    });
  };

  return (
    <div className="conversation-panel">
      <header className="conversation-panel__header">
        <div>
          <p className="eyebrow">Rozmowa</p>
          <h2>{activeContact?.displayName ?? "Wybierz kontakt"}</h2>
        </div>
        {activeContact?.status
          ? <span className={`conversation-panel__status status-${activeContact.status}`}>{activeContact.status}</span>
          : null}
      </header>

      <div className="history" ref={historyRef} onScroll={handleHistoryScroll}>
        {!activeContact
          ? <p className="conversation-panel__empty">Kliknij kontakt, zeby otworzyc rozmowe.</p>
          : isLoadingConversation
          ? <p className="conversation-panel__empty">Ladowanie rozmowy...</p>
          : conversationError
          ? <p className="alert alert-error">{conversationError}</p>
          : messages.length === 0
          ? <p className="conversation-panel__empty">Brak wiadomosci w tej rozmowie.</p>
          : <>
            {isLoadingOlderMessages
              ? <p className="history__status">Ladowanie starszych wiadomosci...</p>
              : olderMessagesError
              ? <p className="alert alert-error history__status">{olderMessagesError}</p>
              : null}
            {messages.map((message) => (
              <article
                key={message.id}
                className={message.sender === "me"
                  ? "message message-me"
                  : message.sender === "other"
                  ? "message message-other"
                  : "message message-system"}
              >
                {message.sender === "other" && message.senderDisplayName
                  ? <strong>{message.senderDisplayName}</strong>
                  : null}
                <p>{message.text}</p>
                <time>{formatLocalTime(message.sentAtUtc)}</time>
              </article>
            ))}
          </>}
        <div ref={messagesEndRef} />
      </div>

      <div className="composer">
        {sendError
          ? <p className="alert alert-error composer__error">{sendError}</p>
          : null}
        <TextArea
          value={draft}
          onChange={(event) => onDraftChange(event.target.value)}
          onKeyDown={onDraftKeyDown}
          rows={2}
          placeholder={activeConversationId ? "Napisz wiadomosc..." : "Wybierz kontakt, aby rozpoczac rozmowe"}
          disabled={isComposerDisabled}
        />
        <Button type="button" onClick={() => void onSendDraft()} disabled={isSendDisabled}>
          {isSendingMessage ? "Wysylanie..." : "Wyslij"}
        </Button>
      </div>
    </div>
  );
}
