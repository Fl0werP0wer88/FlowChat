import type { GroupConversation } from "../../api";

interface GroupConversationListItemProps {
  conversation: GroupConversation;
  isActive: boolean;
  onClick: (conversation: GroupConversation) => void;
}

export function GroupConversationListItem({
  conversation,
  isActive,
  onClick,
}: GroupConversationListItemProps) {
  return (
    <li>
      <button
        aria-current={isActive ? "true" : undefined}
        className="contacts-panel__contact-button"
        onClick={() => onClick(conversation)}
        type="button"
      >
        <span className="contacts-panel__contact-copy">
          <span className="contacts-panel__contact-name">{conversation.name}</span>
          <span className="contacts-panel__contact-email">
            {conversation.participantCount} uczestnikow
          </span>
        </span>
      </button>
    </li>
  );
}
