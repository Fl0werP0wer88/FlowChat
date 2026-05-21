import { useState } from "react";
import type { Contact } from "../../../types/contacts";
import type { ManualUserStatus, UserStatus } from "../../../types/realtime";
import type { GroupConversation } from "../../groups";
import { GroupConversationsList } from "../../groups";
import type { SearchUserResult, SearchUsersCriteria } from "../../contacts";
import { ContactsList } from "../../contacts";
import { UserSearch } from "../../users";
import { SidebarHeader } from "./SidebarHeader";
import type { SidebarTab } from "./SidebarHeader";


interface SidebarProps {
  activeContactId: string | null;
  activeGroupConversationId: string | null;
  addContactNotice: { kind: "error" | "info"; message: string; } | null;
  contacts: Contact[];
  currentUserStatus: UserStatus;
  groupConversations: GroupConversation[];
  isAddingContact: boolean;
  isChangingPresenceStatus: boolean;
  isLoadingContacts: boolean;
  isLoadingGroupConversations: boolean;
  onAddContact: (lookupValue: string) => Promise<boolean>;
  onAddContactByUserId: (userId: string) => Promise<boolean>;
  onChangePresenceStatus: (status: ManualUserStatus) => Promise<void>;
  onContactClick: (contact: Contact) => void;
  onClearNotice: () => void;
  onGroupConversationClick: (conversation: GroupConversation) => void;
  onSearchUsers: (criteria: SearchUsersCriteria, signal?: AbortSignal) => Promise<SearchUserResult[]>;
  presenceNotice: string | null;
}

export function Sidebar({
  activeContactId,
  activeGroupConversationId,
  addContactNotice,
  contacts,
  currentUserStatus,
  groupConversations,
  isAddingContact,
  isChangingPresenceStatus,
  isLoadingContacts,
  isLoadingGroupConversations,
  onAddContact,
  onAddContactByUserId,
  onChangePresenceStatus,
  onContactClick,
  onClearNotice,
  onGroupConversationClick,
  onSearchUsers,
  presenceNotice,
}: SidebarProps) {
  const [activeTab, setActiveTab] = useState<SidebarTab>("contacts");
  const [isUserSearchOpen, setIsUserSearchOpen] = useState(false);

  const openUserSearch = () => {
    setIsUserSearchOpen(true);
    onClearNotice();
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

        {addContactNotice && !isUserSearchOpen
          ? (
            <p className={`alert ${addContactNotice.kind === "error" ? "alert-error" : "alert-info"}`}>
              {addContactNotice.message}
            </p>
          )
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
        addContactNotice={addContactNotice}
        isAddingContact={isAddingContact}
        isOpen={isUserSearchOpen}
        onAddContact={onAddContact}
        onAddContactByUserId={onAddContactByUserId}
        onClearNotice={onClearNotice}
        onClose={() => setIsUserSearchOpen(false)}
        onSearchUsers={onSearchUsers}
      />
    </aside>
  );
}
