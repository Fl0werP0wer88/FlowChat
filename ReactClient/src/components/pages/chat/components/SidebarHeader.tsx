import { useChatSelectionStore } from "../../../../store/chatSelectionStore";
import { usePresenceStore } from "../../../../store/presenceStore";
import type { ManualUserStatus } from "../../../../types/realtime";

interface SidebarHeaderProps {
  onChangePresenceStatus: (status: ManualUserStatus) => Promise<void>;
}

const presenceOptions: ManualUserStatus[] = ["Active", "Busy", "Invisible"];

export function SidebarHeader({ onChangePresenceStatus }: SidebarHeaderProps) {
  const activeTab = useChatSelectionStore((s) => s.activeTab);
  const setActiveTab = useChatSelectionStore((s) => s.setActiveTab);
  const currentStatus = usePresenceStore((s) => s.currentStatus);
  const isUpdatingStatus = usePresenceStore((s) => s.isUpdatingStatus);

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
            onClick={() => setActiveTab("contacts")}
            type="button"
          >
            Kontakty
          </button>
          <button
            aria-pressed={activeTab === "groups"}
            className={`contacts-panel__tab${activeTab === "groups" ? " contacts-panel__tab--active" : ""}`}
            onClick={() => setActiveTab("groups")}
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
            disabled={isUpdatingStatus}
            onChange={(event) => void handlePresenceStatusChange(event.target.value)}
            value={currentStatus}
          >
            {currentStatus === "AFK"
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
