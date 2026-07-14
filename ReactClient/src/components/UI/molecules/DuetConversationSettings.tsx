import type { Contact } from "../../../types/contacts";
import { useState } from "react";
import { ConfirmDialog } from "./ConfirmDialog";

interface DuetConversationSettingsProps {
  activeContact: Contact | null;
  onCreateGroupClick?: () => void;
  isMutePending: boolean;
  isBlockPending: boolean;
  isHidePending: boolean;
  onMuteChange: (muted: boolean) => Promise<void>;
  onBlockChange: (blocked: boolean) => Promise<void>;
  onHide: () => Promise<void>;
}

export function DuetConversationSettings({
  activeContact,
  onCreateGroupClick,
  isMutePending,
  isBlockPending,
  isHidePending,
  onMuteChange,
  onBlockChange,
  onHide,
}: DuetConversationSettingsProps) {
  const [isBlockConfirmationOpen, setIsBlockConfirmationOpen] = useState(false);
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
        onClick={onCreateGroupClick}
        type="button"
      >
        <span>
          <strong>Stwórz grupę</strong>
          <span className="conversation-settings__hint">Rozpocznij rozmowę grupową z tym kontaktem</span>
        </span>
        <span aria-hidden="true" className="material-symbols-rounded">group_add</span>
      </button>

      <div className="conversation-settings__group">
        <div>
          <strong>{activeContact.isMuted ? "Wyłącz wyciszenie" : "Wycisz rozmowę"}</strong>
          <span className="conversation-settings__hint">Steruj powiadomieniami z tej rozmowy</span>
        </div>
        <button
          aria-checked={activeContact.isMuted}
          aria-label="Wycisz rozmowę"
          className="conversation-settings__switch"
          disabled={isMutePending}
          onClick={() => void onMuteChange(!activeContact.isMuted)}
          role="switch"
          type="button"
        ><span /></button>
      </div>

      {activeContact.isBlockedByPartner ? (
        <p className="conversation-settings__notice">Ten kontakt zablokował możliwość wysyłania wiadomości.</p>
      ) : null}

      <button
        className="conversation-settings__action conversation-settings__action--danger"
        disabled={isHidePending}
        onClick={() => void onHide()}
        type="button"
      >
        <span>
          <strong>{isHidePending ? "Ukrywanie..." : "Ukryj rozmowę"}</strong>
          <span className="conversation-settings__hint">Usuń rozmowę z listy kontaktów</span>
        </span>
        <span aria-hidden="true" className="material-symbols-rounded">visibility_off</span>
      </button>

      <button
        className="conversation-settings__action conversation-settings__action--danger"
        disabled={isBlockPending}
        onClick={() => {
          if (activeContact.isBlocked) void onBlockChange(false);
          else setIsBlockConfirmationOpen(true);
        }}
        type="button"
      >
        <span>
          <strong>{activeContact.isBlocked ? "Odblokuj kontakt" : "Zablokuj kontakt"}</strong>
          <span className="conversation-settings__hint">{activeContact.isBlocked
            ? "Przywróć możliwość wysyłania wiadomości"
            : "Zatrzymaj wiadomości w tej rozmowie"}</span>
        </span>
        <span aria-hidden="true" className="material-symbols-rounded">block</span>
      </button>

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
      <ConfirmDialog
        confirmLabel="Zablokuj kontakt"
        description={`Nie będziecie mogli wysyłać do siebie wiadomości, dopóki nie odblokujesz kontaktu ${activeContact.displayName}.`}
        isOpen={isBlockConfirmationOpen}
        isPending={isBlockPending}
        onClose={() => setIsBlockConfirmationOpen(false)}
        onConfirm={() => void onBlockChange(true)
          .then(() => setIsBlockConfirmationOpen(false))
          .catch(() => undefined)}
        title="Zablokować kontakt?"
      />
    </section>
  );
}
