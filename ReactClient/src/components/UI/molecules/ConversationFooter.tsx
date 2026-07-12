import type { KeyboardEvent } from "react";
import { Button } from "../atoms/Button";
import { TextArea } from "../atoms/TextArea";

interface ConversationFooterProps {
  activeConversationId: string | null;
  draft: string;
  emptyPlaceholder?: string;
  isComposerDisabled: boolean;
  isSendDisabled: boolean;
  isSendingMessage: boolean;
  disabledMessage?: string | null;
  onDraftChange: (value: string) => void;
  onDraftKeyDown: (event: KeyboardEvent<HTMLTextAreaElement>) => void;
  onSendDraft: () => Promise<void>;
}

export function ConversationFooter({
  activeConversationId,
  draft,
  emptyPlaceholder = "Wybierz kontakt, aby rozpoczac rozmowe",
  isComposerDisabled,
  isSendDisabled,
  isSendingMessage,
  disabledMessage = null,
  onDraftChange,
  onDraftKeyDown,
  onSendDraft,
}: ConversationFooterProps) {
  return (
    <div className="composer">
      {disabledMessage ? <p className="composer__notice">{disabledMessage}</p> : null}
      <TextArea
        value={draft}
        onChange={(event) => onDraftChange(event.target.value)}
        onKeyDown={onDraftKeyDown}
        rows={2}
        placeholder={activeConversationId ? "Napisz wiadomosc..." : emptyPlaceholder}
        disabled={isComposerDisabled}
      />
      <Button type="button" onClick={() => void onSendDraft()} disabled={isSendDisabled}>
        {isSendingMessage ? "Wysylanie..." : "Wyslij"}
      </Button>
    </div>
  );
}
