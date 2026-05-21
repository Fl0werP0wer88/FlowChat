import type { GroupConversation } from "../../../groups";

interface GroupConversationHeaderProps {
  activeGroupConversation: GroupConversation | null;
  activeConversationName: string | null;
}

export function GroupConversationHeader({
  activeGroupConversation,
  activeConversationName,
}: GroupConversationHeaderProps) {
  return (
    <header className="conversation-panel__header">
      <div>
        <p className="eyebrow">Grupa</p>
        <h2>{activeConversationName ?? activeGroupConversation?.name ?? "Wybierz grupe"}</h2>
      </div>
      {activeGroupConversation
        ? (
          <div className="conversation-panel__header-actions">
            <span className="conversation-panel__status">
              {activeGroupConversation.participantCount} uczestnikow
            </span>
          </div>
        )
        : null}
    </header>
  );
}
