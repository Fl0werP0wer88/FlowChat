import { useEffect, useState } from "react";
import { useAuthStore } from "../../../../store/authStore";
import type { SearchUserResult } from "../../../../api/userProfileApi";
import { UsersPicker } from "../../../users";
import { createGroupConversation } from "../../../../api/chatApi";

interface GroupBuilderProps {
  groupName?: string;
  initialUserIds?: string[];
  isOpen: boolean;
  onClose: () => void;
}

export function GroupBuilder({
  groupName: initialGroupName = "",
  initialUserIds = [],
  isOpen,
  onClose,
}: GroupBuilderProps) {
  const accessToken = useAuthStore((s) => s.accessToken) ?? "";
  const [groupName, setGroupName] = useState("");

  useEffect(() => {
    if (isOpen) {
      setGroupName(initialGroupName);
      return;
    }

    setGroupName("");
  }, [initialGroupName, isOpen]);

  const handleCreateGroup = async (selectedMembers: SearchUserResult[]) => {
    const trimmedName = groupName.trim();
    if (!trimmedName) {
      throw new Error("Podaj nazwe grupy.");
    }

    if (!accessToken) {
      throw new Error("Brakuje aktywnej sesji potrzebnej do utworzenia grupy.");
    }

    await createGroupConversation(
      selectedMembers.map((member) => member.userProfileId),
      trimmedName,
      accessToken,
    );
    onClose();
  };

  return (
    <div className={`contacts-composer group-builder ${isOpen ? "contacts-composer--open" : ""}`} aria-hidden={!isOpen}>
      <div className="contacts-composer__header">
        <button
          aria-label="Wroc do grup"
          className="contacts-composer__back-button"
          onClick={onClose}
          type="button"
        >
          <span aria-hidden="true" className="material-symbols-rounded">arrow_back</span>
        </button>
        <strong>Nowa grupa</strong>
      </div>

      <input
        className="field-shell group-builder__name-input"
        onChange={(event) => setGroupName(event.target.value)}
        placeholder="Nazwa grupy"
        type="text"
        value={groupName}
      />

      <UsersPicker
        confirmLabel="Wybierz"
        initialUserIds={initialUserIds}
        isOpen={isOpen}
        onConfirm={handleCreateGroup}
      />
    </div>
  );
}
