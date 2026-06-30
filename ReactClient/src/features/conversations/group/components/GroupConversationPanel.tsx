import type { KeyboardEvent } from "react";
import { useEffect, useState } from "react";
import type { ChatMessage } from "../../../../types/chat";
import type { GroupConversation } from "../../../../api/chatApi";
import { ConversationBody } from "../../components/ConversationBody";
import { ConversationFooter } from "../../components/ConversationFooter";
import { GroupConversationHeader } from "./GroupConversationHeader";
import { GroupConversationSettings } from "./GroupConversationSettings";

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
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const isComposerDisabled = !activeConversationId || isLoadingConversation || isSendingMessage;
  const isSendDisabled = isComposerDisabled || draft.trim().length === 0;

  useEffect(() => {
    setIsSettingsOpen(false);
  }, [activeConversationId, activeGroupConversation?.conversationId]);

  return (
    <div className="conversation-panel">
      <GroupConversationHeader
        activeGroupConversation={activeGroupConversation}
        activeConversationName={activeConversationName}
        isSettingsOpen={isSettingsOpen}
        onTuneClick={() => setIsSettingsOpen((current) => !current)}
      />
      {isSettingsOpen
        ? (
          <GroupConversationSettings
            activeGroupConversation={activeGroupConversation}
            activeConversationName={activeConversationName}
          />
        )
        : (
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
        )}
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
