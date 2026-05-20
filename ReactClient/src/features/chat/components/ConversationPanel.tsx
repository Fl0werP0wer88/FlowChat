import { useEffect, useRef, useState, type KeyboardEvent } from "react";
import { Virtuoso, type VirtuosoHandle } from "react-virtuoso";
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

const START_INDEX = 100_000;

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
  const isComposerDisabled = !activeConversationId || isLoadingConversation || isSendingMessage;
  const isSendDisabled = isComposerDisabled || draft.trim().length === 0;

  const virtuosoRef = useRef<VirtuosoHandle>(null);
  const [firstItemIndex, setFirstItemIndex] = useState(START_INDEX);
  const messagesLengthRef = useRef(messages.length);

  useEffect(() => {
    setFirstItemIndex(START_INDEX);
    messagesLengthRef.current = messages.length;
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [activeConversationId]);

  useEffect(() => {
    if (isLoadingOlderMessages) {
      messagesLengthRef.current = messages.length;
      return;
    }
    const prepended = messages.length - messagesLengthRef.current;
    if (prepended > 0) {
      setFirstItemIndex((prev) => prev - prepended);
      messagesLengthRef.current = messages.length;
    }
  }, [isLoadingOlderMessages, messages.length]);

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

      {!activeContact
        ? <p className="conversation-panel__empty history">Kliknij kontakt, zeby otworzyc rozmowe.</p>
        : isLoadingConversation
        ? <p className="conversation-panel__empty history">Ladowanie rozmowy...</p>
        : conversationError
        ? <p className="alert alert-error history">{conversationError}</p>
        : messages.length === 0
        ? <p className="conversation-panel__empty history">Brak wiadomosci w tej rozmowie.</p>
        : <Virtuoso
            ref={virtuosoRef}
            className="history"
            data={messages}
            firstItemIndex={firstItemIndex}
            initialTopMostItemIndex={messages.length - 1}
            followOutput={activeConversationId ? "smooth" : false}
            startReached={hasOlderMessages && !isLoadingOlderMessages
              ? () => void onLoadOlderMessages()
              : undefined}
            components={{
              Header: () => isLoadingOlderMessages
                ? <p className="history__status">Ladowanie starszych wiadomosci...</p>
                : olderMessagesError
                ? <p className="alert alert-error history__status">{olderMessagesError}</p>
                : null,
            }}
            itemContent={(_index: number, message: ChatMessage) => (
              <article
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
            )}
          />}

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
