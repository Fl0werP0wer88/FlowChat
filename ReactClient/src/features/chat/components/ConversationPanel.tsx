import type { KeyboardEvent } from "react";
import { Button } from "../../../components/atoms/Button";
import { TextArea } from "../../../components/atoms/TextArea";
import type { ChatMessage } from "../../../types/chat";
import { formatLocalTime } from "../../../utils/dateUtils";

interface ConversationPanelProps {
  messages: ChatMessage[];
  draft: string;
  onDraftChange: (value: string) => void;
  onDraftKeyDown: (event: KeyboardEvent<HTMLTextAreaElement>) => void;
  onSendDraft: () => void;
}

export function ConversationPanel({
  messages,
  draft,
  onDraftChange,
  onDraftKeyDown,
  onSendDraft,
}: ConversationPanelProps) {
  return (
    <div className="conversation-panel">
      <div className="history">
        {messages.map((message) => (
          <article
            key={message.id}
            className={message.sender === "me" ? "message message-me" : "message message-system"}
          >
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
          placeholder="Napisz wiadomosc..."
        />
        <Button type="button" onClick={onSendDraft}>
          Wyslij
        </Button>
      </div>
    </div>
  );
}
