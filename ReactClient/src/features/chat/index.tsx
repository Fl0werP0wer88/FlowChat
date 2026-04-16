import { ChatHeader } from "../../components/organisms/ChatHeader";
import { ChatTemplate } from "../../components/templates/ChatTemplate";
import { useRealtimeConnection } from "../../realtime/useRealtimeConnection";
import { ContactsPanel, useContacts } from "../contacts";
import { usePresenceStatus } from "../presence/hooks/usePresenceStatus";
import { ConversationPanel } from "./components/ConversationPanel";
import { useChatMessages } from "./hooks/useChatMessages";

interface ChatFeatureProps {
  accessToken: string;
  userLogin: string;
  onLogout: () => void;
}

export function ChatFeature({ accessToken, userLogin, onLogout }: ChatFeatureProps) {
  const contacts = useContacts(accessToken);
  const chat = useChatMessages();
  const presence = usePresenceStatus(accessToken);
  const realtime = useRealtimeConnection({
    accessToken,
    onPresenceChanged: contacts.applyPresenceChanged,
    onReceiveContactPresenceStatuses: contacts.initializePresenceStatuses,
    onReceiveMessage: chat.receiveRealtimeMessage,
    onReceivePresencePreferences: presence.applyPresencePreferences,
  });

  return (
    <ChatTemplate
      header={<ChatHeader userLogin={userLogin} realtimeStatus={realtime.status} onLogout={onLogout} />}
      conversation={
        <ConversationPanel
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
          currentUserStatus={presence.currentStatus}
          isAddingContact={contacts.isAddingContact}
          isChangingPresenceStatus={presence.isUpdatingStatus}
          isLoadingContacts={contacts.isLoadingContacts}
          onAddContact={contacts.addContactByLookup}
          onAddContactByUserId={contacts.addContactByUserId}
          onChangePresenceStatus={presence.changeManualPresenceStatus}
          onClearNotice={contacts.clearNotice}
          onSearchUsers={contacts.searchUsers}
          presenceNotice={presence.errorMessage}
        />
      }
    />
  );
}
