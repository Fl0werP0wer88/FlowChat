import { useEffect, useState } from "react";
import type { Contact } from "../../../../types/contacts";
import type { ManualUserStatus, UserStatus } from "../../../../types/realtime";
import type { GroupConversation } from "../../../../api/chatApi";
import { GroupBuilder, GroupConversationsList } from "../../../../components/UI/organisms/Groups";
import { ContactsBuilder, ContactsList } from "../../../../components/UI/organisms/Contacts";
import { SidebarHeader } from "./SidebarHeader";
import type { SidebarTab } from "./SidebarHeader";
import type { SearchUserResult } from "../../../../api/userProfileApi";


export interface GroupBuilderRequest {
  groupName?: string;
  initialUserIds?: string[];
  requestId: number;
}

type ActiveComposer =
  | { type: "contacts" }
  | { type: "group"; groupName?: string; initialUserIds?: string[] }
  | null;

interface SidebarProps {
  activeContactId: string | null;
  activeGroupConversationId: string | null;
  contacts: Contact[];
  currentUserStatus: UserStatus;
  groupBuilderRequest: GroupBuilderRequest | null;
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
  groupBuilderRequest,
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
  const [activeComposer, setActiveComposer] = useState<ActiveComposer>(null);

  useEffect(() => {
    if (!groupBuilderRequest) {
      return;
    }

    setActiveTab("groups");
    setActiveComposer({
      type: "group",
      groupName: groupBuilderRequest.groupName,
      initialUserIds: groupBuilderRequest.initialUserIds,
    });
  }, [groupBuilderRequest]);

  const openContactsComposer = () => {
    setActiveComposer({ type: "contacts" });
  };

  const openGroupBuilder = () => {
    setActiveComposer({ type: "group" });
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
