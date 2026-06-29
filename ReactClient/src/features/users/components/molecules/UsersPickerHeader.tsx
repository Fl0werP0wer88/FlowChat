import type { SearchUserResult } from "../../api";

interface UsersPickerHeaderProps {
  onRemoveMember: (userProfileId: string) => void;
  selectedMembers: SearchUserResult[];
}

export function UsersPickerHeader({
  onRemoveMember,
  selectedMembers,
}: UsersPickerHeaderProps) {
  if (selectedMembers.length === 0) {
    return null;
  }

  return (
    <ul className="users-picker__selected-list">
      {selectedMembers.map((member) => (
        <li className="users-picker__selected-item" key={member.userProfileId}>
          <span>{member.displayName}</span>
          <button
            aria-label={`Usun ${member.displayName} z listy`}
            className="users-picker__remove-button"
            onClick={() => onRemoveMember(member.userProfileId)}
            type="button"
          >
            <span aria-hidden="true" className="material-symbols-rounded">person_remove</span>
          </button>
        </li>
      ))}
    </ul>
  );
}
