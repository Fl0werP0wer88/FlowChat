import type { Contact } from "../../../types/contacts";

interface ContactsListProps {
  activeContactId: string | null;
  contacts: Contact[];
  isLoadingContacts: boolean;
  onAddContactClick: () => void;
  onContactClick: (contact: Contact) => void;
}

export function ContactsList({
  activeContactId,
  contacts,
  isLoadingContacts,
  onAddContactClick,
  onContactClick,
}: ContactsListProps) {
  const renderContent = () => {
    if (isLoadingContacts) {
      return <p className="contacts-panel__status">Ladowanie kontaktow...</p>;
    }

    if (contacts.length === 0) {
      return <p className="contacts-panel__status">Nie masz jeszcze kontaktow.</p>;
    }

    return (
      <ul className="contacts-panel__list">
        {contacts.map((contact) => (
          <li key={contact.id}>
            <button
              aria-current={activeContactId === contact.id ? "true" : undefined}
              className="contacts-panel__contact-button"
              onClick={() => onContactClick(contact)}
              type="button"
            >
              <span className={`status-dot status-${contact.status}`} />
              <span className="contacts-panel__contact-copy">
                <span className="contacts-panel__contact-name">{contact.displayName}</span>
                {contact.email
                  ? <span className="contacts-panel__contact-email">{contact.email}</span>
                  : null}
              </span>
            </button>
          </li>
        ))}
      </ul>
    );
  };

  return (
    <div className="contacts-panel__contacts">
      <div className="contacts-panel__list-toolbar">
        <button
          aria-label="Dodaj kontakt"
          className="contacts-panel__icon-button contacts-panel__add-contact-button"
          onClick={onAddContactClick}
          type="button"
        >
          <span aria-hidden="true" className="material-symbols-rounded">person_add</span>
        </button>
      </div>

      {renderContent()}
    </div>
  );
}
