import type { SearchUserResult } from "../../../types/users";

interface UsersPickerHeaderProps {
  onRemoveMember: (userProfileId: string) => void;
  selectedMembers: SearchUserResult[];
}

function getInitials(displayName: string): string {
  const parts = displayName.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) {
    return "?";
  }

  const first = parts[0]?.[0] ?? "";
  const last = parts.length > 1 ? parts[parts.length - 1]?.[0] ?? "" : "";
  return (first + last).toUpperCase();
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
        <li className="users-picker__selected-item" key={member.userProfileId} title={member.displayName}>
          <span className="users-picker__selected-initials">{getInitials(member.displayName)}</span>
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
