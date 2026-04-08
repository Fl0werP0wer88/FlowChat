import { ChatHeader } from "../../components/organisms/ChatHeader";
import { ChatTemplate } from "../../components/templates/ChatTemplate";
import { useRealtimeConnection } from "../../realtime/useRealtimeConnection";
import { ContactsPanel, useContacts } from "../contacts";
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
  const realtime = useRealtimeConnection({
    accessToken,
    onPresenceChanged: chat.receivePresenceChanged,
    onReceiveMessage: chat.receiveRealtimeMessage,
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
      sidebar={(
        <ContactsPanel
          addContactNotice={contacts.notice}
          contacts={contacts.contacts}
          isAddingContact={contacts.isAddingContact}
          isLoadingContacts={contacts.isLoadingContacts}
          onAddContact={contacts.addContactByLookup}
          onClearNotice={contacts.clearNotice}
        />
      )}
    />
  );
}
