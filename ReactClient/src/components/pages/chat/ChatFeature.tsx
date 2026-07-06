import { useState } from "react";
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

  const [duetDraft, setDuetDraft] = useState("");
  const [groupDraft, setGroupDraft] = useState("");

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
    onMessageReceived: (payload) => {
      contacts.applyRealtimeMessage(payload, chat.activeConversationId);
      groupConversations.applyRealtimeMessage(payload, activeGroup?.conversationId ?? null);
      chat.messageReceived(payload);
      groupChat.messageReceived(payload);
    },
  });

  const sendDuetDraft = async (): Promise<void> => {
    const sent = await chat.sendDraft(duetDraft);
    if (sent) setDuetDraft("");
  };

  const sendGroupDraft = async (): Promise<void> => {
    const sent = await groupChat.sendDraft(groupDraft);
    if (sent) setGroupDraft("");
  };

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
            draft={groupDraft}
            onDraftChange={setGroupDraft}
            onDraftKeyDown={(event) => {
              if (event.key === "Enter" && !event.shiftKey) {
                event.preventDefault();
                void sendGroupDraft();
              }
            }}
            onSendDraft={sendGroupDraft}
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
            draft={duetDraft}
            onDraftChange={setDuetDraft}
            onDraftKeyDown={(event) => {
              if (event.key === "Enter" && !event.shiftKey) {
                event.preventDefault();
                void sendDuetDraft();
              }
            }}
            onSendDraft={sendDuetDraft}
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
            void groupChat.markActiveGroupConversationAsRead(conversation);
          }}
          onProcessUser={contacts.addContact}
        />
      }
    />
  );
}
