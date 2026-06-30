import type { KeyboardEvent } from "react";
import { useEffect, useState } from "react";
import type { ChatMessage } from "../../../../types/chat";
import type { Contact } from "../../../../types/contacts";
import { ConversationBody } from "../../molecules/ConversationBody";
import { ConversationFooter } from "../../molecules/ConversationFooter";
import { DuetConversationHeader } from "../../molecules/DuetConversationHeader";
import { DuetConversationSettings } from "../../molecules/DuetConversationSettings";

interface DuetConversationPanelProps {
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
  onCreateGroupFromDuet: (request: { groupName: string; initialUserIds: string[] }) => void;
}

export function DuetConversationPanel({
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
  onCreateGroupFromDuet,
}: DuetConversationPanelProps) {
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const isComposerDisabled = !activeConversationId || isLoadingConversation || isSendingMessage;
  const isSendDisabled = isComposerDisabled || draft.trim().length === 0;

  useEffect(() => {
    setIsSettingsOpen(false);
  }, [activeConversationId, activeContact?.userId]);

  const handleCreateGroupClick = () => {
    if (!activeContact) {
      return;
    }

    onCreateGroupFromDuet({
      groupName: activeContact.displayName,
      initialUserIds: [activeContact.userId],
    });
  };

  return (
    <div className="conversation-panel">
      <DuetConversationHeader
        activeContact={activeContact}
        isSettingsOpen={isSettingsOpen}
        onTuneClick={() => setIsSettingsOpen((current) => !current)}
      />
      {isSettingsOpen
        ? (
          <DuetConversationSettings
            activeContact={activeContact}
            onCreateGroupClick={handleCreateGroupClick}
          />
        )
        : (
          <ConversationBody
            activeConversationId={activeConversationId}
            conversationError={conversationError}
            hasActiveConversation={Boolean(activeContact)}
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
