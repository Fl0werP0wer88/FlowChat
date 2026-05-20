import { useEffect, useRef, useState } from "react";
import { Virtuoso, type VirtuosoHandle } from "react-virtuoso";
import { Spinner } from "../../../../components/atoms/Spinner";
import { useMinDuration } from "../../../../hooks/useMinDuration";
import type { ChatMessage } from "../../../../types/chat";
import type { Contact } from "../../../../types/contacts";
import { formatLocalTime } from "../../../../utils/dateUtils";

interface ConversationBodyProps {
  activeContact: Contact | null;
  activeConversationId: string | null;
  conversationError: string | null;
  isLoadingConversation: boolean;
  hasOlderMessages: boolean;
  isLoadingOlderMessages: boolean;
  olderMessagesError: string | null;
  messages: ChatMessage[];
  onLoadOlderMessages: () => Promise<void>;
}

const START_INDEX = 100_000;

export function ConversationBody({
  activeContact,
  activeConversationId,
  conversationError,
  isLoadingConversation,
  hasOlderMessages,
  isLoadingOlderMessages,
  olderMessagesError,
  messages,
  onLoadOlderMessages,
}: ConversationBodyProps) {
  const showOlderMessagesSpinner = useMinDuration(isLoadingOlderMessages, 500);
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

  if (!activeContact) {
    return <p className="conversation-panel__empty history">Kliknij kontakt, zeby otworzyc rozmowe.</p>;
  }

  if (isLoadingConversation) {
    return <p className="conversation-panel__empty history">Ladowanie rozmowy...</p>;
  }

  if (conversationError) {
    return <p className="alert alert-error history">{conversationError}</p>;
  }

  if (messages.length === 0) {
    return <p className="conversation-panel__empty history">Brak wiadomosci w tej rozmowie.</p>;
  }

  return (
    <Virtuoso
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
        Header: () => showOlderMessagesSpinner
          ? <div className="history__status"><Spinner /></div>
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
    />
  );
}
