import { useNavigate } from "react-router-dom";
import { ChatHeader } from "../../UI/organisms/ChatHeader";
import { ChatTemplate } from "../../templates";
import { useAuthStore } from "../../../store/authStore";
import { useChatSelectionStore } from "../../../store/chatSelectionStore";
import {
  useChatMessages,
  useContacts,
  useGroupChatMessages,
  useGroupConversations,
  usePresenceStatus,
  useRealtimeConnection,
} from "../../../hooks";
import { DuetConversationPanel, GroupConversationPanel } from "../../UI/organisms/Conversations";
import { Sidebar } from "./components/Sidebar";

export function ChatFeature() {
  const userLogin = useAuthStore((s) => s.login) ?? "Uzytkownik";
  const signOut = useAuthStore((s) => s.signOut);
  const navigate = useNavigate();

  const activeConversationMode = useChatSelectionStore((s) => s.activeConversationMode);
  const activeGroupConversation = useChatSelectionStore((s) => s.activeGroupConversation);
  const { selectDuetContact, selectGroupConversation, openGroupBuilder } = useChatSelectionStore.getState();

  const handleLogout = () => {
    signOut();
    navigate("/login", { replace: true });
  };

  const contacts = useContacts();
  const activeDuetContact = activeConversationMode === "duet" ? contacts.activeContact : null;
  const activeGroup = activeConversationMode === "group" ? activeGroupConversation : null;
  const chat = useChatMessages(activeDuetContact);
  const groupChat = useGroupChatMessages(activeGroup);
  const presence = usePresenceStatus();
  const groupConversations = useGroupConversations();
  useRealtimeConnection({
    onGroupConversationChanged: groupConversations.applyGroupConversationChanged,
    onPresenceChanged: contacts.applyPresenceChanged,
    onReceiveMessage: (payload) => {
      chat.receiveRealtimeMessage(payload);
      groupChat.receiveRealtimeMessage(payload);
    },
  });

  return (
    <ChatTemplate
      header={<ChatHeader userLogin={userLogin} onLogout={handleLogout} />}
      conversation={activeConversationMode === "group"
        ? (
          <GroupConversationPanel
            activeGroupConversation={activeGroup}
            activeConversationId={groupChat.activeConversationId}
            activeConversationName={groupChat.activeConversationName}
            hasConversationError={groupChat.hasConversationError}
            isLoadingConversation={groupChat.isLoadingConversation}
            isSendingMessage={groupChat.isSendingMessage}
            hasOlderMessages={groupChat.hasOlderMessages}
            isLoadingOlderMessages={groupChat.isLoadingOlderMessages}
            messages={groupChat.messages}
            draft={groupChat.draft}
            onDraftChange={groupChat.setDraft}
            onDraftKeyDown={groupChat.handleDraftKeyDown}
            onSendDraft={groupChat.sendDraft}
            onLoadOlderMessages={groupChat.loadOlderMessages}
          />
        )
        : (
          <DuetConversationPanel
            activeContact={activeDuetContact}
            activeConversationId={chat.activeConversationId}
            hasConversationError={chat.hasConversationError}
            isLoadingConversation={chat.isLoadingConversation}
            isSendingMessage={chat.isSendingMessage}
            hasOlderMessages={chat.hasOlderMessages}
            isLoadingOlderMessages={chat.isLoadingOlderMessages}
            messages={chat.messages}
            draft={chat.draft}
            onDraftChange={chat.setDraft}
            onDraftKeyDown={chat.handleDraftKeyDown}
            onSendDraft={chat.sendDraft}
            onLoadOlderMessages={chat.loadOlderMessages}
            onCreateGroupFromDuet={(request) => openGroupBuilder(request.groupName, request.initialUserIds)}
          />
        )}
      sidebar={
        <Sidebar
          contacts={contacts.contacts}
          groupConversations={groupConversations.groupConversations}
          isLoadingContacts={contacts.isLoadingContacts}
          isLoadingGroupConversations={groupConversations.isLoadingGroupConversations}
          onChangePresenceStatus={presence.changeManualPresenceStatus}
          onContactClick={(contact) => {
            selectDuetContact(contact);
            chat.openContactConversation(contact, contacts.updateContactConversationId);
          }}
          onGroupConversationClick={(conversation) => {
            selectGroupConversation(conversation);
            groupChat.openGroupConversation();
          }}
          onProcessUser={contacts.addContact}
        />
      }
    />
  );
}
