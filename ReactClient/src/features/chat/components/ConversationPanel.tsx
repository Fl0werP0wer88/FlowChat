import type { KeyboardEvent } from "react";
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
  messages: ChatMessage[];
  draft: string;
  onDraftChange: (value: string) => void;
  onDraftKeyDown: (event: KeyboardEvent<HTMLTextAreaElement>) => void;
  onSendDraft: () => void;
}

export function ConversationPanel({
  activeContact,
  activeConversationId,
  conversationError,
  isLoadingConversation,
  messages,
  draft,
  onDraftChange,
  onDraftKeyDown,
  onSendDraft,
}: ConversationPanelProps) {
  const isComposerDisabled = true;

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

      <div className="history">
        {!activeContact
          ? <p className="conversation-panel__empty">Kliknij kontakt, zeby otworzyc rozmowe.</p>
          : isLoadingConversation
          ? <p className="conversation-panel__empty">Ladowanie rozmowy...</p>
          : conversationError
          ? <p className="alert alert-error">{conversationError}</p>
          : messages.length === 0
          ? <p className="conversation-panel__empty">Brak wiadomosci w tej rozmowie.</p>
          : messages.map((message) => (
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
              <time>{formatLocalTime(message.createdAt)}</time>
            </article>
          ))}
      </div>

      <div className="composer">
        <TextArea
          value={draft}
          onChange={(event) => onDraftChange(event.target.value)}
          onKeyDown={onDraftKeyDown}
          rows={2}
          placeholder={activeConversationId ? "Wysylanie wiadomosci bedzie podpiete w kolejnym kroku" : "Wybierz kontakt, aby rozpoczac rozmowe"}
          disabled={isComposerDisabled}
        />
        <Button type="button" onClick={onSendDraft} disabled={isComposerDisabled}>
          Wyslij
        </Button>
      </div>
    </div>
  );
}
