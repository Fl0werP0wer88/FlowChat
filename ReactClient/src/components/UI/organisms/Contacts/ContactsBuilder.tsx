import type { SearchUserResult } from "../../../../api/userProfileService";
import { UsersPicker } from "../UserPicker";

interface ContactsBuilderProps {
  isOpen: boolean;
  onClose: () => void;
  onProcessUser: (user: SearchUserResult) => Promise<void>;
}

export function ContactsBuilder({
  isOpen,
  onClose,
  onProcessUser,
}: ContactsBuilderProps) {
  return (
    <div className={`contacts-composer ${isOpen ? "contacts-composer--open" : ""}`} aria-hidden={!isOpen}>
      <div className="contacts-composer__header">
        <button
          aria-label="Wroc do kontaktow"
          className="contacts-composer__back-button"
          onClick={onClose}
          type="button"
        >
          <span aria-hidden="true" className="material-symbols-rounded">arrow_back</span>
        </button>
        <strong>Nowy kontakt</strong>
      </div>

      <UsersPicker
        isOpen={isOpen}
        onConfirm={(users) => onProcessUser(users[0])}
        singlePick
      />
    </div>
  );
}
