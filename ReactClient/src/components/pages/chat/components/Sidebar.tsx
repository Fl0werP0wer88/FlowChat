import { useChatSelectionStore } from "../../../../store/chatSelectionStore";
import type { Contact } from "../../../../types/contacts";
import type { ManualUserStatus } from "../../../../types/realtime";
import type { GroupConversation } from "../../../../api/chatApi";
import { GroupBuilder, GroupConversationsList } from "../../../../components/UI/organisms/Groups";
import { ContactsBuilder, ContactsList } from "../../../../components/UI/organisms/Contacts";
import { SidebarHeader } from "./SidebarHeader";
import type { SearchUserResult } from "../../../../api/userProfileApi";

interface SidebarProps {
  contacts: Contact[];
  groupConversations: GroupConversation[];
  isLoadingContacts: boolean;
  isLoadingGroupConversations: boolean;
  onChangePresenceStatus: (status: ManualUserStatus) => Promise<void>;
  onContactClick: (contact: Contact) => void;
  onGroupConversationClick: (conversation: GroupConversation) => void;
  onProcessUser: (user: SearchUserResult) => Promise<void>;
}

export function Sidebar({
  contacts,
  groupConversations,
  isLoadingContacts,
  isLoadingGroupConversations,
  onChangePresenceStatus,
  onContactClick,
  onGroupConversationClick,
  onProcessUser,
}: SidebarProps) {
  const activeTab = useChatSelectionStore((s) => s.activeTab);
  const activeComposer = useChatSelectionStore((s) => s.activeComposer);
  const activeContactId = useChatSelectionStore((s) => s.activeContactId);
  const activeConversationMode = useChatSelectionStore((s) => s.activeConversationMode);
  const activeGroupConversation = useChatSelectionStore((s) => s.activeGroupConversation);
  const { openContactsComposer, openGroupBuilder, closeComposer } = useChatSelectionStore.getState();

  const activeGroupConversationId = activeConversationMode === "group"
    ? activeGroupConversation?.conversationId ?? null
    : null;

  const contactListActiveContactId = activeConversationMode === "duet" ? activeContactId : null;

  return (
    <aside className={`contacts-panel ${activeComposer ? "contacts-panel--composer-open" : ""}`}>
      <div className="contacts-panel__main">
        <SidebarHeader onChangePresenceStatus={onChangePresenceStatus} />

        {activeTab === "contacts"
          ? (
            <ContactsList
              activeContactId={contactListActiveContactId}
              contacts={contacts}
              isLoadingContacts={isLoadingContacts}
              onAddContactClick={openContactsComposer}
              onContactClick={onContactClick}
            />
          )
          : (
            <GroupConversationsList
              activeGroupConversationId={activeGroupConversationId}
              groupConversations={groupConversations}
              isLoading={isLoadingGroupConversations}
              onAddGroupClick={() => openGroupBuilder()}
              onGroupConversationClick={onGroupConversationClick}
            />
          )}
      </div>

      <ContactsBuilder
        isOpen={activeComposer?.type === "contacts"}
        onClose={closeComposer}
        onProcessUser={onProcessUser}
      />

      <GroupBuilder
        groupName={activeComposer?.type === "group" ? activeComposer.groupName : undefined}
        initialUserIds={activeComposer?.type === "group" ? activeComposer.initialUserIds : undefined}
        isOpen={activeComposer?.type === "group"}
        onClose={closeComposer}
      />
    </aside>
  );
}
