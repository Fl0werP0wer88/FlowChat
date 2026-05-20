import { useState } from "react";
import type { Contact } from "../../../types/contacts";
import type { ManualUserStatus, UserStatus } from "../../../types/realtime";
import type { SearchUserResult, SearchUsersCriteria } from "../api";
import { ContactSearch } from "./ContactSearch";
import { ContactsList } from "./ContactsList";
import { SidebarHeader } from "./SidebarHeader";

interface SidebarBodyProps {
  activeContactId: string | null;
  addContactNotice: { kind: "error" | "info"; message: string; } | null;
  contacts: Contact[];
  currentUserStatus: UserStatus;
  isAddingContact: boolean;
  isChangingPresenceStatus: boolean;
  isLoadingContacts: boolean;
  onAddContact: (lookupValue: string) => Promise<boolean>;
  onAddContactByUserId: (userId: string) => Promise<boolean>;
  onChangePresenceStatus: (status: ManualUserStatus) => Promise<void>;
  onContactClick: (contact: Contact) => void;
  onClearNotice: () => void;
  onSearchUsers: (criteria: SearchUsersCriteria, signal?: AbortSignal) => Promise<SearchUserResult[]>;
  presenceNotice: string | null;
}

export function SidebarBody({
  activeContactId,
  addContactNotice,
  contacts,
  currentUserStatus,
  isAddingContact,
  isChangingPresenceStatus,
  isLoadingContacts,
  onAddContact,
  onAddContactByUserId,
  onChangePresenceStatus,
  onContactClick,
  onClearNotice,
  onSearchUsers,
  presenceNotice,
}: SidebarBodyProps) {
  const [isContactSearchOpen, setIsContactSearchOpen] = useState(false);

  const openContactSearch = () => {
    setIsContactSearchOpen(true);
    onClearNotice();
  };

  return (
    <aside className={`contacts-panel ${isContactSearchOpen ? "contacts-panel--composer-open" : ""}`}>
      <div className="contacts-panel__main">
        <SidebarHeader
          currentUserStatus={currentUserStatus}
          isChangingPresenceStatus={isChangingPresenceStatus}
          onChangePresenceStatus={onChangePresenceStatus}
        />

        {presenceNotice
          ? <p className="alert alert-error">{presenceNotice}</p>
          : null}

        {addContactNotice && !isContactSearchOpen
          ? (
            <p className={`alert ${addContactNotice.kind === "error" ? "alert-error" : "alert-info"}`}>
              {addContactNotice.message}
            </p>
          )
          : null}

        <ContactsList
          activeContactId={activeContactId}
          contacts={contacts}
          isLoadingContacts={isLoadingContacts}
          onAddContactClick={openContactSearch}
          onContactClick={onContactClick}
        />
      </div>

      <ContactSearch
        addContactNotice={addContactNotice}
        isAddingContact={isAddingContact}
        isOpen={isContactSearchOpen}
        onAddContact={onAddContact}
        onAddContactByUserId={onAddContactByUserId}
        onClearNotice={onClearNotice}
        onClose={() => setIsContactSearchOpen(false)}
        onSearchUsers={onSearchUsers}
      />
    </aside>
  );
}
