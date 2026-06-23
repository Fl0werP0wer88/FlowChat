import type { Contact } from "../../../../types/contacts";
import { ContactListItem } from "../molecules/ContactListItem";

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
          <ContactListItem
            contact={contact}
            isActive={activeContactId === contact.id}
            key={contact.id}
            onClick={onContactClick}
          />
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
