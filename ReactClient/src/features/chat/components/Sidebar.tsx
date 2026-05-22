import { useState } from "react";
import type { Contact } from "../../../types/contacts";
import type { ManualUserStatus, UserStatus } from "../../../types/realtime";
import type { GroupConversation } from "../../groups";
import { GroupConversationsList } from "../../groups";
import { ContactsList } from "../../contacts";
import { UserSearch } from "../../users";
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
  isUserProcessDisabled: boolean;
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
  isUserProcessDisabled,
  onChangePresenceStatus,
  onContactClick,
  onGroupConversationClick,
  onProcessUser,
  presenceNotice,
}: SidebarProps) {
  const [activeTab, setActiveTab] = useState<SidebarTab>("contacts");
  const [isUserSearchOpen, setIsUserSearchOpen] = useState(false);

  const openUserSearch = () => {
    setIsUserSearchOpen(true);
  };

  return (
    <aside className={`contacts-panel ${isUserSearchOpen ? "contacts-panel--composer-open" : ""}`}>
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
              onAddContactClick={openUserSearch}
              onContactClick={onContactClick}
            />
          )
          : (
            <GroupConversationsList
              activeGroupConversationId={activeGroupConversationId}
              groupConversations={groupConversations}
              isLoading={isLoadingGroupConversations}
              onGroupConversationClick={onGroupConversationClick}
            />
          )}
      </div>

      <UserSearch
        isDisabled={isUserProcessDisabled}
        isOpen={isUserSearchOpen}
        onClose={() => setIsUserSearchOpen(false)}
        onProcessUser={onProcessUser}
      />
    </aside>
  );
}
