import type { ManualUserStatus, UserStatus } from "../../../../types/realtime";

interface ContactsSidebarHeaderProps {
  currentUserStatus: UserStatus;
  isChangingPresenceStatus: boolean;
  onChangePresenceStatus: (status: ManualUserStatus) => Promise<void>;
}

const presenceOptions: ManualUserStatus[] = ["Active", "Busy", "Invisible"];

export function ContactsSidebarHeader({
  currentUserStatus,
  isChangingPresenceStatus,
  onChangePresenceStatus,
}: ContactsSidebarHeaderProps) {
  const handlePresenceStatusChange = async (value: string) => {
    if (value === "AFK") {
      return;
    }

    await onChangePresenceStatus(value as ManualUserStatus);
  };

  return (
    <div className="contacts-panel__header">
      <div className="contacts-panel__header-main">
        <h2>Kontakty</h2>
        <label className="contacts-panel__presence-control">
          <span className="contacts-panel__presence-label">Status</span>
          <select
            aria-label="Ustaw status Presence"
            className="contacts-panel__presence-select"
            disabled={isChangingPresenceStatus}
            onChange={(event) => void handlePresenceStatusChange(event.target.value)}
            value={currentUserStatus}
          >
            {currentUserStatus === "AFK"
              ? <option value="AFK">AFK (auto)</option>
              : null}
            {presenceOptions.map((status) => (
              <option key={status} value={status}>
                {status}
              </option>
            ))}
          </select>
        </label>
      </div>
    </div>
  );
}
