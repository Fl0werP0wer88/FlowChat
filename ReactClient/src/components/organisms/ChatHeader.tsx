import { Button } from "../atoms/Button";

interface ChatHeaderProps {
  userLogin: string;
  onLogout: () => void;
}

export function ChatHeader({ userLogin, onLogout }: ChatHeaderProps) {
  return (
    <header className="chat-header">
      <div className="chat-title-block">
        <p className="eyebrow">FlowChat</p>
        <h1>Wiadomosci</h1>
      </div>
      <div className="header-actions">
        <span className="logged-user">{userLogin}</span>
        <Button variant="secondary" type="button" onClick={onLogout}>
          Wyloguj
        </Button>
      </div>
    </header>
  );
}
