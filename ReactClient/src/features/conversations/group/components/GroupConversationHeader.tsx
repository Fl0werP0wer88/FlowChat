import type { GroupConversation } from "../../../../api/chatApi";

interface GroupConversationHeaderProps {
  activeGroupConversation: GroupConversation | null;
  activeConversationName: string | null;
  isSettingsOpen?: boolean;
  onTuneClick?: () => void;
}

export function GroupConversationHeader({
  activeGroupConversation,
  activeConversationName,
  isSettingsOpen = false,
  onTuneClick,
}: GroupConversationHeaderProps) {
  return (
    <header className="conversation-panel__header">
      <div>
        <p className="eyebrow">Grupa</p>
        <h2>{activeConversationName ?? activeGroupConversation?.name ?? "Wybierz grupe"}</h2>
      </div>
      <div className="conversation-panel__header-actions">
        {activeGroupConversation
          ? (
            <span className="conversation-panel__status">
              {activeGroupConversation.participantCount} uczestnikow
            </span>
          )
          : null}
        <button
          aria-pressed={isSettingsOpen}
          aria-label="Ustawienia grupy"
          className={isSettingsOpen
            ? "conversation-panel__tune-button conversation-panel__tune-button--active"
            : "conversation-panel__tune-button"}
          onClick={onTuneClick}
          type="button"
        >
          <span aria-hidden="true" className="material-symbols-rounded">tune</span>
        </button>
      </div>
    </header>
  );
}
