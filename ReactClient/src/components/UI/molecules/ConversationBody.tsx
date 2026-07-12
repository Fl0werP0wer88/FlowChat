import { useEffect, useMemo, useRef, useState } from "react";
import { Virtuoso, type VirtuosoHandle } from "react-virtuoso";
import { Spinner } from "../atoms/Spinner";
import { useMinDuration } from "../../../hooks";
import type { ChatMessage } from "../../../types/chat";
import { formatLocalTime } from "../../../utils/dateUtils";

interface ConversationBodyProps {
  activeConversationId: string | null;
  hasConversationError: boolean;
  emptySelectionMessage?: string;
  hasActiveConversation: boolean;
  isLoadingConversation: boolean;
  hasOlderMessages: boolean;
  isLoadingOlderMessages: boolean;
  messages: ChatMessage[];
  participants: Array<{ userId: string; displayName: string | null }>;
  onLoadOlderMessages: () => Promise<void>;
}

const START_INDEX = 100_000;

export function ConversationBody({
  activeConversationId,
  hasConversationError,
  emptySelectionMessage = "Kliknij kontakt, zeby otworzyc rozmowe.",
  hasActiveConversation,
  isLoadingConversation,
  hasOlderMessages,
  isLoadingOlderMessages,
  messages,
  participants,
  onLoadOlderMessages,
}: ConversationBodyProps) {
  const showOlderMessagesSpinner = useMinDuration(isLoadingOlderMessages, 500);
  const virtuosoRef = useRef<VirtuosoHandle>(null);
  const [firstItemIndex, setFirstItemIndex] = useState(START_INDEX);
  const messagesLengthRef = useRef(messages.length);
  const participantDisplayNames = useMemo(
    () => new Map(participants.map((participant) => [participant.userId, participant.displayName?.trim() || "Nieznany użytkownik"])),
    [participants],
  );

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

  if (!hasActiveConversation) {
    return <p className="conversation-panel__empty history">{emptySelectionMessage}</p>;
  }

  if (isLoadingConversation) {
    return <p className="conversation-panel__empty history">Ladowanie rozmowy...</p>;
  }

  if (hasConversationError) {
    return <p className="conversation-panel__empty history">Nie udalo sie zaladowac rozmowy.</p>;
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
          {message.sender === "other"
            ? <strong>{participantDisplayNames.get(message.senderUserId ?? "") ?? "Nieznany użytkownik"}</strong>
            : null}
          <p>{message.text}</p>
          <time>{formatLocalTime(message.sentAtUtc)}</time>
        </article>
      )}
    />
  );
}
