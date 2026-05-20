import { useNavigate } from "react-router-dom";
import { ChatHeader } from "../../components/organisms/ChatHeader";
import { ChatTemplate } from "../../components/templates/ChatTemplate";
import { useRealtimeConnection } from "../../realtime/useRealtimeConnection";
import { useAuthStore } from "../../store/authStore";
import { SidebarBody, useContacts } from "../contacts";
import { usePresenceStatus } from "../presence/hooks/usePresenceStatus";
import { ConversationPanel } from "./components/ConversationPanel";
import { useChatMessages } from "./hooks/useChatMessages";

export function ChatFeature() {
  const userLogin = useAuthStore((s) => s.login) ?? "Uzytkownik";
  const signOut = useAuthStore((s) => s.signOut);
  const navigate = useNavigate();

  const handleLogout = () => {
    signOut();
    navigate("/login", { replace: true });
  };

  const contacts = useContacts();
  const chat = useChatMessages(contacts.activeContact);
  const presence = usePresenceStatus();
  const realtime = useRealtimeConnection({
    onPresenceChanged: contacts.applyPresenceChanged,
    onReceiveMessage: chat.receiveRealtimeMessage,
  });

  return (
    <ChatTemplate
      header={<ChatHeader userLogin={userLogin} realtimeStatus={realtime.status} onLogout={handleLogout} />}
      conversation={
        <ConversationPanel
          activeContact={contacts.activeContact}
          activeConversationId={chat.activeConversationId}
          conversationError={chat.conversationError}
          isLoadingConversation={chat.isLoadingConversation}
          isSendingMessage={chat.isSendingMessage}
          hasOlderMessages={chat.hasOlderMessages}
          isLoadingOlderMessages={chat.isLoadingOlderMessages}
          sendError={chat.sendError}
          olderMessagesError={chat.olderMessagesError}
          messages={chat.messages}
          draft={chat.draft}
          onDraftChange={chat.setDraft}
          onDraftKeyDown={chat.handleDraftKeyDown}
          onSendDraft={chat.sendDraft}
          onLoadOlderMessages={chat.loadOlderMessages}
        />
      }
      sidebar={
        <SidebarBody
          addContactNotice={contacts.notice}
          contacts={contacts.contacts}
          activeContactId={contacts.activeContact?.id ?? null}
          currentUserStatus={presence.currentStatus}
          isAddingContact={contacts.isAddingContact}
          isChangingPresenceStatus={presence.isUpdatingStatus}
          isLoadingContacts={contacts.isLoadingContacts}
          onAddContact={contacts.addContactByLookup}
          onAddContactByUserId={contacts.addContactByUserId}
          onChangePresenceStatus={presence.changeManualPresenceStatus}
          onContactClick={(contact) => {
            contacts.selectContact(contact);
            chat.openContactConversation(contact, contacts.updateContactConversationId);
          }}
          onClearNotice={contacts.clearNotice}
          onSearchUsers={contacts.searchUsers}
          presenceNotice={presence.errorMessage}
        />
      }
    />
  );
}
