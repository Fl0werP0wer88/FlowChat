import type { Contact } from "../../../../types/contacts";

interface DuetConversationSettingsProps {
  activeContact: Contact | null;
  createGroupNotice: { kind: "error" | "info"; message: string } | null;
  isCreatingGroup: boolean;
  onCreateGroupClick?: () => void;
}

export function DuetConversationSettings({
  activeContact,
  createGroupNotice,
  isCreatingGroup,
  onCreateGroupClick,
}: DuetConversationSettingsProps) {
  if (!activeContact) {
    return (
      <section className="conversation-settings">
        <p className="conversation-panel__empty">Wybierz kontakt, zeby zobaczyc ustawienia rozmowy.</p>
      </section>
    );
  }

  return (
    <section className="conversation-settings" aria-label="Ustawienia rozmowy">
      <div className="conversation-settings__intro">
        <p className="eyebrow">Ustawienia</p>
      </div>

      <button
        className="conversation-settings__action"
        disabled={isCreatingGroup}
        onClick={onCreateGroupClick}
        type="button"
      >
        <span>
          <strong>{isCreatingGroup ? "Tworzenie..." : "Stwórz grupę"}</strong>
          <span className="conversation-settings__hint">Rozpocznij rozmowę grupową z tym kontaktem</span>
        </span>
        <span aria-hidden="true" className="material-symbols-rounded">group_add</span>
      </button>

      {createGroupNotice
        ? (
          <p className={`alert ${createGroupNotice.kind === "error" ? "alert-error" : "alert-info"}`}>
            {createGroupNotice.message}
          </p>
        )
        : null}

      {activeContact.email
        ? (
          <div className="conversation-settings__group">
            <div>
              <strong>Email</strong>
              <span className="conversation-settings__hint">{activeContact.email}</span>
            </div>
          </div>
        )
        : null}
    </section>
  );
}
