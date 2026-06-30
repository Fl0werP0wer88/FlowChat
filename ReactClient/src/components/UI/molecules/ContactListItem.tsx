import type { Contact } from "../../../types/contacts";

interface ContactListItemProps {
  contact: Contact;
  isActive: boolean;
  onClick: (contact: Contact) => void;
}

export function ContactListItem({
  contact,
  isActive,
  onClick,
}: ContactListItemProps) {
  return (
    <li>
      <button
        aria-current={isActive ? "true" : undefined}
        className="contacts-panel__contact-button"
        onClick={() => onClick(contact)}
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
  );
}
