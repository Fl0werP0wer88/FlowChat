import { useRef, useState } from "react";
import type { KeyboardEventHandler } from "react";
import type { Contact } from "../../../types/contacts";

interface ContactsPanelProps {
  addContactNotice: { kind: "error" | "info"; message: string } | null;
  contacts: Contact[];
  isAddingContact: boolean;
  isLoadingContacts: boolean;
  onAddContact: (lookupValue: string) => Promise<boolean>;
  onClearNotice: () => void;
}

export function ContactsPanel({
  addContactNotice,
  contacts,
  isAddingContact,
  isLoadingContacts,
  onAddContact,
  onClearNotice,
}: ContactsPanelProps) {
  const [isComposerOpen, setIsComposerOpen] = useState(false);
  const [lookupValue, setLookupValue] = useState("");
  const inputRef = useRef<HTMLInputElement>(null);

  const openComposer = () => {
    setIsComposerOpen(true);
    onClearNotice();
    window.requestAnimationFrame(() => inputRef.current?.focus());
  };

  const closeComposer = () => {
    setIsComposerOpen(false);
    setLookupValue("");
    onClearNotice();
  };

  const submitLookup = async () => {
    const wasAdded = await onAddContact(lookupValue);
    if (wasAdded) {
      setLookupValue("");
      setIsComposerOpen(false);
    }
  };

  const handleLookupKeyDown: KeyboardEventHandler<HTMLInputElement> = async (event) => {
    if (event.key !== "Enter") {
      return;
    }

    event.preventDefault();
    await submitLookup();
  };

  return (
    <aside className={`contacts-panel ${isComposerOpen ? "contacts-panel--composer-open" : ""}`}>
      <div className="contacts-panel__main">
        <div className="contacts-panel__header">
          <h2>Kontakty</h2>
          <button
            aria-label="Dodaj kontakt"
            className="contacts-panel__icon-button"
            onClick={openComposer}
            type="button"
          >
            <span aria-hidden="true" className="material-symbols-rounded">add_box</span>
          </button>
        </div>

        {addContactNotice && !isComposerOpen
          ? <p className={`alert ${addContactNotice.kind === "error" ? "alert-error" : "alert-info"}`}>{addContactNotice.message}</p>
          : null}

        {isLoadingContacts
          ? <p className="contacts-panel__status">Ladowanie kontaktow...</p>
          : contacts.length === 0
          ? <p className="contacts-panel__status">Nie masz jeszcze kontaktow.</p>
          : (
            <ul className="contacts-panel__list">
              {contacts.map((contact) => (
                <li key={contact.id}>
                  <span className={`status-dot status-${contact.status}`} />
                  <span>{contact.displayName}</span>
                </li>
              ))}
            </ul>
          )}
      </div>

      <div className="contacts-composer" aria-hidden={!isComposerOpen}>
        <div className="contacts-composer__header">
          <button
            aria-label="Wroc do kontaktow"
            className="contacts-composer__back-button"
            onClick={closeComposer}
            type="button"
          >
            <span aria-hidden="true" className="material-symbols-rounded">arrow_back</span>
          </button>
          <strong>Nowy kontakt</strong>
        </div>

        <div className="contacts-composer__search">
          <button
            aria-label="Dodaj kontakt z podanej wartosci"
            className="contacts-composer__search-button"
            disabled={isAddingContact}
            onClick={() => void submitLookup()}
            type="button"
          >
            <span aria-hidden="true" className="material-symbols-rounded">person_add</span>
          </button>
          <input
            className="contacts-composer__input"
            disabled={isAddingContact}
            onChange={(event) => setLookupValue(event.target.value)}
            onKeyDown={(event) => void handleLookupKeyDown(event)}
            placeholder="User Id or email"
            ref={inputRef}
            type="text"
            value={lookupValue}
          />
        </div>

        {addContactNotice
          ? <p className={`alert ${addContactNotice.kind === "error" ? "alert-error" : "alert-info"}`}>{addContactNotice.message}</p>
          : null}

        <button
          className="contacts-composer__action"
          onClick={() => inputRef.current?.focus()}
          type="button"
        >
          <span aria-hidden="true" className="material-symbols-rounded">person_search</span>
          <span>Search User</span>
        </button>
      </div>
    </aside>
  );
}
