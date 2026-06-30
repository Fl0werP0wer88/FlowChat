import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { ChatHeader } from "../../UI/organisms/ChatHeader";
import { ChatTemplate } from "../../templates";
import { useAuthStore } from "../../../store/authStore";
import {
  useChatMessages,
  useContacts,
  useGroupChatMessages,
  useGroupConversations,
  usePresenceStatus,
  useRealtimeConnection,
} from "../../../hooks";
import { DuetConversationPanel, GroupConversationPanel } from "../../UI/organisms/Conversations";
import type { GroupConversation } from "../../../api/chatApi";
import { Sidebar } from "./components/Sidebar";
import type { GroupBuilderRequest } from "./components/Sidebar";

type ActiveConversationMode = "duet" | "group";

export function ChatFeature() {
  const userLogin = useAuthStore((s) => s.login) ?? "Uzytkownik";
  const signOut = useAuthStore((s) => s.signOut);
  const navigate = useNavigate();
  const [activeConversationMode, setActiveConversationMode] = useState<ActiveConversationMode>("duet");
  const [activeGroupConversation, setActiveGroupConversation] = useState<GroupConversation | null>(null);
  const [groupBuilderRequest, setGroupBuilderRequest] = useState<GroupBuilderRequest | null>(null);

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
  const realtime = useRealtimeConnection({
    onGroupConversationChanged: groupConversations.applyGroupConversationChanged,
    onPresenceChanged: contacts.applyPresenceChanged,
    onReceiveMessage: (payload) => {
      chat.receiveRealtimeMessage(payload);
      groupChat.receiveRealtimeMessage(payload);
    },
  });

  const openGroupBuilderFromDuet = (request: Omit<GroupBuilderRequest, "requestId">) => {
    setGroupBuilderRequest((current) => ({
      ...request,
      requestId: (current?.requestId ?? 0) + 1,
    }));
  };

  return (
    <ChatTemplate
      header={<ChatHeader userLogin={userLogin} realtimeStatus={realtime.status} onLogout={handleLogout} />}
      conversation={activeConversationMode === "group"
        ? (
          <GroupConversationPanel
            activeGroupConversation={activeGroup}
            activeConversationId={groupChat.activeConversationId}
            activeConversationName={groupChat.activeConversationName}
            conversationError={groupChat.conversationError}
            isLoadingConversation={groupChat.isLoadingConversation}
            isSendingMessage={groupChat.isSendingMessage}
            hasOlderMessages={groupChat.hasOlderMessages}
            isLoadingOlderMessages={groupChat.isLoadingOlderMessages}
            sendError={groupChat.sendError}
            olderMessagesError={groupChat.olderMessagesError}
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
            onCreateGroupFromDuet={openGroupBuilderFromDuet}
          />
        )}
      sidebar={
        <Sidebar
          contacts={contacts.contacts}
          activeContactId={activeConversationMode === "duet" ? contacts.activeContact?.id ?? null : null}
          currentUserStatus={presence.currentStatus}
          groupBuilderRequest={groupBuilderRequest}
          isChangingPresenceStatus={presence.isUpdatingStatus}
          isLoadingContacts={contacts.isLoadingContacts}
          onChangePresenceStatus={presence.changeManualPresenceStatus}
          activeGroupConversationId={activeConversationMode === "group"
            ? activeGroupConversation?.conversationId ?? null
            : null}
          groupConversations={groupConversations.groupConversations}
          isLoadingGroupConversations={groupConversations.isLoadingGroupConversations}
          onContactClick={(contact) => {
            setActiveConversationMode("duet");
            contacts.selectContact(contact);
            chat.openContactConversation(contact, contacts.updateContactConversationId);
          }}
          onGroupConversationClick={(conversation) => {
            setActiveConversationMode("group");
            setActiveGroupConversation(conversation);
            groupChat.openGroupConversation();
          }}
          onProcessUser={contacts.addContact}
          presenceNotice={presence.errorMessage}
        />
      }
    />
  );
}
