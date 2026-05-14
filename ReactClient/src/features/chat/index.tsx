import { ChatHeader } from "../../components/organisms/ChatHeader";
import { ChatTemplate } from "../../components/templates/ChatTemplate";
import { useRealtimeConnection } from "../../realtime/useRealtimeConnection";
import { useAuthStore } from "../../store/authStore";
import { ContactsPanel, useContacts } from "../contacts";
import { usePresenceStatus } from "../presence/hooks/usePresenceStatus";
import { ConversationPanel } from "./components/ConversationPanel";
import { useChatMessages } from "./hooks/useChatMessages";

interface ChatFeatureProps {
  onLogout: () => void;
}

export function ChatFeature({ onLogout }: ChatFeatureProps) {
  const userLogin = useAuthStore((s) => s.login) ?? "Uzytkownik";

  const contacts = useContacts();
  const chat = useChatMessages();
  const presence = usePresenceStatus();
  const realtime = useRealtimeConnection({
    onPresenceChanged: contacts.applyPresenceChanged,
    onReceiveMessage: chat.receiveRealtimeMessage,
  });

  return (
    <ChatTemplate
      header={<ChatHeader userLogin={userLogin} realtimeStatus={realtime.status} onLogout={onLogout} />}
      conversation={
        <ConversationPanel
          activeContact={chat.activeContact}
          activeConversationId={chat.activeConversationId}
          conversationError={chat.conversationError}
          isLoadingConversation={chat.isLoadingConversation}
          isSendingMessage={chat.isSendingMessage}
          sendError={chat.sendError}
          messages={chat.messages}
          draft={chat.draft}
          onDraftChange={chat.setDraft}
          onDraftKeyDown={chat.handleDraftKeyDown}
          onSendDraft={chat.sendDraft}
        />
      }
      sidebar={
        <ContactsPanel
          addContactNotice={contacts.notice}
          contacts={contacts.contacts}
          activeContactId={chat.activeContact?.id ?? null}
          currentUserStatus={presence.currentStatus}
          isAddingContact={contacts.isAddingContact}
          isChangingPresenceStatus={presence.isUpdatingStatus}
          isLoadingContacts={contacts.isLoadingContacts}
          onAddContact={contacts.addContactByLookup}
          onAddContactByUserId={contacts.addContactByUserId}
          onChangePresenceStatus={presence.changeManualPresenceStatus}
          onContactClick={(contact) => void chat.openContactConversation(contact, contacts.updateContactConversationId)}
          onClearNotice={contacts.clearNotice}
          onSearchUsers={contacts.searchUsers}
          presenceNotice={presence.errorMessage}
        />
      }
    />
  );
}
