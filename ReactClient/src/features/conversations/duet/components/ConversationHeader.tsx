import type { Contact } from "../../../../types/contacts";

interface ConversationHeaderProps {
  activeContact: Contact | null;
  onTuneClick?: () => void;
}

export function ConversationHeader({
  activeContact,
  onTuneClick,
}: ConversationHeaderProps) {
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
          aria-label="Ustawienia rozmowy"
          className="conversation-panel__tune-button"
          onClick={onTuneClick}
          type="button"
        >
          <span aria-hidden="true" className="material-symbols-rounded">tune</span>
        </button>
      </div>
    </header>
  );
}
