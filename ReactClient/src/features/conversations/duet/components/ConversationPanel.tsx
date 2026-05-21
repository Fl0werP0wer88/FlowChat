import type { KeyboardEvent } from "react";
import { useEffect, useState } from "react";
import { useAuthStore } from "../../../../store/authStore";
import type { ChatMessage } from "../../../../types/chat";
import type { Contact } from "../../../../types/contacts";
import { ConversationBody } from "./ConversationBody";
import { ConversationFooter } from "./ConversationFooter";
import { ConversationHeader } from "./ConversationHeader";
import { ConversationSettings } from "./ConversationSettings";
import { useCopyDuetAsGroupMutation } from "../queries/useCopyDuetAsGroupMutation";

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
  const accessToken = useAuthStore((s) => s.accessToken) ?? "";
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [createGroupNotice, setCreateGroupNotice] = useState<{ kind: "error" | "info"; message: string } | null>(null);
  const isComposerDisabled = !activeConversationId || isLoadingConversation || isSendingMessage;
  const isSendDisabled = isComposerDisabled || draft.trim().length === 0;
  const copyDuetAsGroupMutation = useCopyDuetAsGroupMutation(accessToken, {
    onSuccess: () => setCreateGroupNotice({ kind: "info", message: "Grupa została utworzona." }),
    onError: (message) => setCreateGroupNotice({ kind: "error", message }),
  });

  useEffect(() => {
    setIsSettingsOpen(false);
    setCreateGroupNotice(null);
  }, [activeConversationId, activeContact?.userId]);

  const handleCreateGroupClick = async () => {
    if (!activeContact || copyDuetAsGroupMutation.isPending) {
      return;
    }

    setCreateGroupNotice(null);

    try {
      await copyDuetAsGroupMutation.mutateAsync(activeContact.userId);
    } catch {
      // error is handled in onError
    }
  };

  return (
    <div className="conversation-panel">
      <ConversationHeader
        activeContact={activeContact}
        isSettingsOpen={isSettingsOpen}
        onTuneClick={() => setIsSettingsOpen((current) => !current)}
      />
      {isSettingsOpen
        ? (
          <ConversationSettings
            activeContact={activeContact}
            createGroupNotice={createGroupNotice}
            isCreatingGroup={copyDuetAsGroupMutation.isPending}
            onCreateGroupClick={() => void handleCreateGroupClick()}
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
