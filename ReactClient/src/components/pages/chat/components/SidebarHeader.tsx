import type { ManualUserStatus, UserStatus } from "../../../../types/realtime";

export type SidebarTab = "contacts" | "groups";

interface SidebarHeaderProps {
  activeTab: SidebarTab;
  currentUserStatus: UserStatus;
  isChangingPresenceStatus: boolean;
  onChangePresenceStatus: (status: ManualUserStatus) => Promise<void>;
  onTabChange: (tab: SidebarTab) => void;
}

const presenceOptions: ManualUserStatus[] = ["Active", "Busy", "Invisible"];

export function SidebarHeader({
  activeTab,
  currentUserStatus,
  isChangingPresenceStatus,
  onChangePresenceStatus,
  onTabChange,
}: SidebarHeaderProps) {
  const handlePresenceStatusChange = async (value: string) => {
    if (value === "AFK") {
      return;
    }

    await onChangePresenceStatus(value as ManualUserStatus);
  };

  return (
    <div className="contacts-panel__header">
      <div className="contacts-panel__header-main">
        <nav className="contacts-panel__tabs" aria-label="Widok paska bocznego">
          <button
            aria-pressed={activeTab === "contacts"}
            className={`contacts-panel__tab${activeTab === "contacts" ? " contacts-panel__tab--active" : ""}`}
            onClick={() => onTabChange("contacts")}
            type="button"
          >
            Kontakty
          </button>
          <button
            aria-pressed={activeTab === "groups"}
            className={`contacts-panel__tab${activeTab === "groups" ? " contacts-panel__tab--active" : ""}`}
            onClick={() => onTabChange("groups")}
            type="button"
          >
            Grupy
          </button>
        </nav>
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
