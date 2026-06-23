import type { Contact } from "../../../../types/contacts";

interface DuetConversationHeaderProps {
  activeContact: Contact | null;
  isSettingsOpen?: boolean;
  onTuneClick?: () => void;
}

export function DuetConversationHeader({
  activeContact,
  isSettingsOpen = false,
  onTuneClick,
}: DuetConversationHeaderProps) {
  return (
    <header className="conversation-panel__header">
      <div>
        <p className="eyebrow">Rozmowa</p>
        <h2>{activeContact?.displayName ?? "Wybierz kontakt"}</h2>
      </div>
      <div className="conversation-panel__header-actions">
        {activeContact?.status
          ? <span className={`conversation-panel__status status-${activeContact.status}`}>{activeContact.status}</span>
          : null}
        <button
          aria-pressed={isSettingsOpen}
          aria-label="Ustawienia rozmowy"
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
