import { useState } from "react";
import type { Contact } from "../../../types/contacts";
import type { ManualUserStatus, UserStatus } from "../../../types/realtime";
import type { GroupConversation } from "../../groups";
import { GroupBuilder, GroupConversationsList } from "../../groups";
import { ContactsBuilder, ContactsList } from "../../contacts";
import { SidebarHeader } from "./SidebarHeader";
import type { SidebarTab } from "./SidebarHeader";
import type { SearchUserResult } from "../../users/api";


interface SidebarProps {
  activeContactId: string | null;
  activeGroupConversationId: string | null;
  contacts: Contact[];
  currentUserStatus: UserStatus;
  groupConversations: GroupConversation[];
  isChangingPresenceStatus: boolean;
  isLoadingContacts: boolean;
  isLoadingGroupConversations: boolean;
  onChangePresenceStatus: (status: ManualUserStatus) => Promise<void>;
  onContactClick: (contact: Contact) => void;
  onGroupConversationClick: (conversation: GroupConversation) => void;
  onProcessUser: (user: SearchUserResult) => Promise<{ kind: "error" | "info"; message: string }>;
  presenceNotice: string | null;
}

export function Sidebar({
  activeContactId,
  activeGroupConversationId,
  contacts,
  currentUserStatus,
  groupConversations,
  isChangingPresenceStatus,
  isLoadingContacts,
  isLoadingGroupConversations,
  onChangePresenceStatus,
  onContactClick,
  onGroupConversationClick,
  onProcessUser,
  presenceNotice,
}: SidebarProps) {
  const [activeTab, setActiveTab] = useState<SidebarTab>("contacts");
  const [activeComposer, setActiveComposer] = useState<"contacts" | "group" | null>(null);

  const openContactsComposer = () => {
    setActiveComposer("contacts");
  };

  const openGroupBuilder = () => {
    setActiveComposer("group");
  };

  const closeComposer = () => {
    setActiveComposer(null);
  };

  return (
    <aside className={`contacts-panel ${activeComposer ? "contacts-panel--composer-open" : ""}`}>
      <div className="contacts-panel__main">
        <SidebarHeader
          activeTab={activeTab}
          currentUserStatus={currentUserStatus}
          isChangingPresenceStatus={isChangingPresenceStatus}
          onChangePresenceStatus={onChangePresenceStatus}
          onTabChange={setActiveTab}
        />

        {presenceNotice
          ? <p className="alert alert-error">{presenceNotice}</p>
          : null}


        {activeTab === "contacts"
          ? (
            <ContactsList
              activeContactId={activeContactId}
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
              onAddGroupClick={openGroupBuilder}
              onGroupConversationClick={onGroupConversationClick}
            />
          )}
      </div>

      <ContactsBuilder
        isOpen={activeComposer === "contacts"}
        onClose={closeComposer}
        onProcessUser={onProcessUser}
      />

      <GroupBuilder
        isOpen={activeComposer === "group"}
        onClose={closeComposer}
      />
    </aside>
  );
}
