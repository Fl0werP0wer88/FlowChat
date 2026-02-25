import type { Contact } from "../../../types/contacts";

interface ContactsPanelProps {
  contacts: Contact[];
}

export function ContactsPanel({ contacts }: ContactsPanelProps) {
  return (
    <aside className="contacts-panel">
      <details open>
        <summary>Kontakty</summary>
        <ul>
          {contacts.map((contact) => (
            <li key={contact.id}>
              <span className={`status-dot status-${contact.status}`} />
              <span>{contact.displayName}</span>
            </li>
          ))}
        </ul>
      </details>
    </aside>
  );
}
