import type { KeyboardEvent } from "react";
import type { ChatMessage } from "../../../../types/chat";
import type { GroupConversation } from "../../../groups";
import { ConversationBody } from "../../duet/components/ConversationBody";
import { ConversationFooter } from "../../duet/components/ConversationFooter";
import { GroupConversationHeader } from "./GroupConversationHeader";

interface GroupConversationPanelProps {
  activeGroupConversation: GroupConversation | null;
  activeConversationId: string | null;
  activeConversationName: string | null;
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

export function GroupConversationPanel({
  activeGroupConversation,
  activeConversationId,
  activeConversationName,
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
}: GroupConversationPanelProps) {
  const isComposerDisabled = !activeConversationId || isLoadingConversation || isSendingMessage;
  const isSendDisabled = isComposerDisabled || draft.trim().length === 0;

  return (
    <div className="conversation-panel">
      <GroupConversationHeader
        activeGroupConversation={activeGroupConversation}
        activeConversationName={activeConversationName}
      />
      <ConversationBody
        activeConversationId={activeConversationId}
        conversationError={conversationError}
        emptySelectionMessage="Kliknij grupe, zeby otworzyc rozmowe."
        hasActiveConversation={Boolean(activeGroupConversation)}
        isLoadingConversation={isLoadingConversation}
        hasOlderMessages={hasOlderMessages}
        isLoadingOlderMessages={isLoadingOlderMessages}
        olderMessagesError={olderMessagesError}
        messages={messages}
        onLoadOlderMessages={onLoadOlderMessages}
      />
      <ConversationFooter
        activeConversationId={activeConversationId}
        draft={draft}
        emptyPlaceholder="Wybierz grupe, aby rozpoczac rozmowe"
        isComposerDisabled={isComposerDisabled}
        isSendDisabled={isSendDisabled}
        isSendingMessage={isSendingMessage}
        sendError={sendError}
        onDraftChange={onDraftChange}
        onDraftKeyDown={onDraftKeyDown}
        onSendDraft={onSendDraft}
      />
    </div>
  );
}
