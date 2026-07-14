import type { KeyboardEvent } from "react";
import { useEffect, useState } from "react";
import { toast } from "sonner";
import { useBlockConversationMutation, useHideConversationMutation, useMuteConversationMutation } from "../../../../hooks";
import { useAuthStore } from "../../../../store/authStore";
import { useChatSelectionStore } from "../../../../store/chatSelectionStore";
import type { ChatMessage, DuetConversationParticipant } from "../../../../types/chat";
import type { Contact } from "../../../../types/contacts";
import { ConversationBody } from "../../molecules/ConversationBody";
import { ConversationFooter } from "../../molecules/ConversationFooter";
import { DuetConversationHeader } from "../../molecules/DuetConversationHeader";
import { DuetConversationSettings } from "../../molecules/DuetConversationSettings";

interface DuetConversationPanelProps {
  activeContact: Contact | null;
  activeConversationId: string | null;
  hasConversationError: boolean;
  isLoadingConversation: boolean;
  isSendingMessage: boolean;
  hasOlderMessages: boolean;
  isLoadingOlderMessages: boolean;
  messages: ChatMessage[];
  participants: DuetConversationParticipant[];
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
  hasConversationError,
  isLoadingConversation,
  isSendingMessage,
  hasOlderMessages,
  isLoadingOlderMessages,
  messages,
  participants,
  draft,
  onDraftChange,
  onDraftKeyDown,
  onSendDraft,
  onLoadOlderMessages,
  onCreateGroupFromDuet,
}: DuetConversationPanelProps) {
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const accessToken = useAuthStore((state) => state.accessToken) ?? "";
  const clearDuetSelection = useChatSelectionStore((state) => state.clearDuetSelection);
  const muteMutation = useMuteConversationMutation(accessToken);
  const blockMutation = useBlockConversationMutation(accessToken);
  const hideMutation = useHideConversationMutation(accessToken);
  const isBlocked = Boolean(activeContact?.isBlocked || activeContact?.isBlockedByPartner);
  const isComposerDisabled = !activeConversationId || isLoadingConversation || isSendingMessage || isBlocked;
  const isSendDisabled = isComposerDisabled || draft.trim().length === 0;
  const disabledMessage = activeContact?.isBlocked
    ? "Odblokuj kontakt, aby ponownie wysyłać wiadomości."
    : activeContact?.isBlockedByPartner
      ? "Ten kontakt zablokował możliwość wysyłania wiadomości."
      : null;

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
            isBlockPending={blockMutation.isPending}
            isHidePending={hideMutation.isPending}
            isMutePending={muteMutation.isPending}
            onBlockChange={async (blocked) => {
              if (!activeContact) return;
              try {
                await blockMutation.mutateAsync({ conversationId: activeContact.conversationId, blocked });
                toast.success(blocked ? "Kontakt został zablokowany." : "Kontakt został odblokowany.");
              } catch (error) {
                toast.error(error instanceof Error ? error.message : "Nie udało się zmienić blokady kontaktu.");
                throw error;
              }
            }}
            onCreateGroupClick={handleCreateGroupClick}
            onHide={async () => {
              if (!activeContact) return;
              try {
                await hideMutation.mutateAsync(activeContact.conversationId);
                clearDuetSelection();
                setIsSettingsOpen(false);
                toast.success("Rozmowa została ukryta.");
              } catch (error) {
                toast.error(error instanceof Error ? error.message : "Nie udało się ukryć rozmowy.");
              }
            }}
            onMuteChange={async (muted) => {
              if (!activeContact) return;
              try {
                await muteMutation.mutateAsync({ conversationId: activeContact.conversationId, muted });
                toast.success(muted ? "Rozmowa została wyciszona." : "Wyciszenie zostało wyłączone.");
              } catch (error) {
                toast.error(error instanceof Error ? error.message : "Nie udało się zmienić wyciszenia.");
              }
            }}
          />
        )
        : (
          <ConversationBody
            activeConversationId={activeConversationId}
            hasConversationError={hasConversationError}
            hasActiveConversation={Boolean(activeContact)}
            isLoadingConversation={isLoadingConversation}
            hasOlderMessages={hasOlderMessages}
            isLoadingOlderMessages={isLoadingOlderMessages}
            messages={messages}
            participants={participants}
            onLoadOlderMessages={onLoadOlderMessages}
          />
        )}
      <ConversationFooter
        activeConversationId={activeConversationId}
        draft={draft}
        isComposerDisabled={isComposerDisabled}
        isSendDisabled={isSendDisabled}
        isSendingMessage={isSendingMessage}
        disabledMessage={disabledMessage}
        onDraftChange={onDraftChange}
        onDraftKeyDown={onDraftKeyDown}
        onSendDraft={onSendDraft}
      />
    </div>
  );
}
