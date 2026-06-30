import type { GroupConversation } from "../../../../api/chatApi";
import { GroupConversationListItem } from "../molecules/GroupConversationListItem";

interface GroupConversationsListProps {
  activeGroupConversationId: string | null;
  groupConversations: GroupConversation[];
  isLoading: boolean;
  onAddGroupClick: () => void;
  onGroupConversationClick: (conversation: GroupConversation) => void;
}

export function GroupConversationsList({
  activeGroupConversationId,
  groupConversations,
  isLoading,
  onAddGroupClick,
  onGroupConversationClick,
}: GroupConversationsListProps) {
  const renderContent = () => {
    if (isLoading) {
      return <p className="contacts-panel__status">Ladowanie grup...</p>;
    }

    if (groupConversations.length === 0) {
      return <p className="contacts-panel__status">Nie nalezysz do zadnej grupy.</p>;
    }

    return (
      <ul className="contacts-panel__list">
        {groupConversations.map((conversation) => (
          <GroupConversationListItem
            conversation={conversation}
            isActive={activeGroupConversationId === conversation.conversationId}
            key={conversation.conversationId}
            onClick={onGroupConversationClick}
          />
        ))}
      </ul>
    );
  };

  return (
    <div className="contacts-panel__contacts">
      <div className="contacts-panel__list-toolbar">
        <button
          aria-label="Dodaj grupe"
          className="contacts-panel__icon-button contacts-panel__add-contact-button"
          onClick={onAddGroupClick}
          type="button"
        >
          <span aria-hidden="true" className="material-symbols-rounded">group_add</span>
        </button>
      </div>

      {renderContent()}
    </div>
  );
}
