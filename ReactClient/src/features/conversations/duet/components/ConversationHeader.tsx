import type { Contact } from "../../../../types/contacts";

interface ConversationHeaderProps {
  activeContact: Contact | null;
}

export function ConversationHeader({ activeContact }: ConversationHeaderProps) {
  return (
    <header className="conversation-panel__header">
      <div>
        <p className="eyebrow">Rozmowa</p>
        <h2>{activeContact?.displayName ?? "Wybierz kontakt"}</h2>
      </div>
      {activeContact?.status
        ? <span className={`conversation-panel__status status-${activeContact.status}`}>{activeContact.status}</span>
        : null}
    </header>
  );
}
