import { useEffect, useState } from "react";
import { useAuthStore } from "../../../../store/authStore";
import type { SearchUserResult } from "../../../users/api";
import { UsersPicker } from "../../../users";
import { createGroupConversation } from "../../api";

type GroupBuilderNotice = { kind: "error" | "info"; message: string };

interface GroupBuilderProps {
  isOpen: boolean;
  onClose: () => void;
}

export function GroupBuilder({
  isOpen,
  onClose,
}: GroupBuilderProps) {
  const accessToken = useAuthStore((s) => s.accessToken) ?? "";
  const [groupName, setGroupName] = useState("");
  const [isCreatingGroup, setIsCreatingGroup] = useState(false);
  const [createNotice, setCreateNotice] = useState<GroupBuilderNotice | null>(null);

  useEffect(() => {
    if (isOpen) {
      return;
    }

    setGroupName("");
    setCreateNotice(null);
    setIsCreatingGroup(false);
  }, [isOpen]);

  const handleCreateGroup = async (selectedMembers: SearchUserResult[]) => {
    const trimmedName = groupName.trim();
    if (selectedMembers.length === 0 || !trimmedName) {
      return;
    }

    if (!accessToken) {
      setCreateNotice({ kind: "error", message: "Brakuje aktywnej sesji potrzebnej do utworzenia grupy." });
      return;
    }

    setIsCreatingGroup(true);
    setCreateNotice(null);
    try {
      await createGroupConversation(
        selectedMembers.map((member) => member.userProfileId),
        trimmedName,
        accessToken,
      );
      onClose();
    } catch (error) {
      const message = error instanceof Error ? error.message : "Nie udalo sie utworzyc grupy.";
      setCreateNotice({ kind: "error", message });
    } finally {
      setIsCreatingGroup(false);
    }
  };

  return (
    <div className={`contacts-composer ${isOpen ? "contacts-composer--open" : ""}`} aria-hidden={!isOpen}>
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
        className="contacts-composer__input group-builder__name-input"
        disabled={isCreatingGroup}
        onChange={(event) => setGroupName(event.target.value)}
        placeholder="Nazwa grupy"
        type="text"
        value={groupName}
      />

      <UsersPicker
        confirmLabel="Wybierz"
        confirmNotice={createNotice}
        isConfirmDisabled={!groupName.trim()}
        isConfirming={isCreatingGroup}
        isOpen={isOpen}
        onConfirm={(members) => void handleCreateGroup(members)}
      />
    </div>
  );
}
