import type { RealtimeConnectionStatus } from "../../../types/realtime";
import { Button } from "../atoms/Button";

interface ChatHeaderProps {
  userLogin: string;
  realtimeStatus: RealtimeConnectionStatus;
  onLogout: () => void;
}

function getRealtimeStatusLabel(status: RealtimeConnectionStatus): string {
  switch (status) {
    case "connected":
      return "Realtime online";
    case "connecting":
      return "Realtime laczenie";
    case "reconnecting":
      return "Realtime ponowne laczenie";
    case "error":
      return "Realtime blad";
    default:
      return "Realtime offline";
  }
}

export function ChatHeader({ userLogin, realtimeStatus, onLogout }: ChatHeaderProps) {
  return (
    <header className="chat-header">
      <div className="chat-title-block">
        <p className="eyebrow">FlowChat</p>
        <h1>Wiadomosci</h1>
      </div>
      <div className="header-actions">
        <span className={`realtime-badge realtime-badge-${realtimeStatus}`}>
          {getRealtimeStatusLabel(realtimeStatus)}
        </span>
        <span className="logged-user">{userLogin}</span>
        <Button variant="secondary" type="button" onClick={onLogout}>
          Wyloguj
        </Button>
      </div>
    </header>
  );
}
