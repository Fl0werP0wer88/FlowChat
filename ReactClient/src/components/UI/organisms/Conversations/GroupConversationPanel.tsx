import type { KeyboardEvent } from "react";
import { useEffect, useState } from "react";
import type { ChatMessage } from "../../../../types/chat";
import type { GroupConversation } from "../../../../api/chatApi";
import { ConversationBody } from "../../molecules/ConversationBody";
import { ConversationFooter } from "../../molecules/ConversationFooter";
import { GroupConversationHeader } from "../../molecules/GroupConversationHeader";
import { GroupConversationSettings } from "../../molecules/GroupConversationSettings";

interface GroupConversationPanelProps {
  activeGroupConversation: GroupConversation | null;
  activeConversationId: string | null;
  activeConversationName: string | null;
  hasConversationError: boolean;
  isLoadingConversation: boolean;
  isSendingMessage: boolean;
  hasOlderMessages: boolean;
  isLoadingOlderMessages: boolean;
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
  hasConversationError,
  isLoadingConversation,
  isSendingMessage,
  hasOlderMessages,
  isLoadingOlderMessages,
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
            hasConversationError={hasConversationError}
            emptySelectionMessage="Kliknij grupe, zeby otworzyc rozmowe."
            hasActiveConversation={Boolean(activeGroupConversation)}
            isLoadingConversation={isLoadingConversation}
            hasOlderMessages={hasOlderMessages}
            isLoadingOlderMessages={isLoadingOlderMessages}
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
        onDraftChange={onDraftChange}
        onDraftKeyDown={onDraftKeyDown}
        onSendDraft={onSendDraft}
      />
    </div>
  );
}
