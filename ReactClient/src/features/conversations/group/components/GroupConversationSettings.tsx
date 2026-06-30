import type { GroupConversation } from "../../../../api/chatApi";

interface GroupConversationSettingsProps {
  activeGroupConversation: GroupConversation | null;
  activeConversationName: string | null;
}

export function GroupConversationSettings({
  activeGroupConversation,
  activeConversationName,
}: GroupConversationSettingsProps) {
  if (!activeGroupConversation) {
    return (
      <section className="conversation-settings">
        <p className="conversation-panel__empty">Wybierz grupe, zeby zobaczyc ustawienia grupy.</p>
      </section>
    );
  }

  const displayName = activeConversationName ?? activeGroupConversation.name;

  return (
    <section className="conversation-settings" aria-label="Ustawienia grupy">
      <div className="conversation-settings__intro">
        <p className="eyebrow">Ustawienia</p>
      </div>

      <div className="conversation-settings__group">
        <div>
          <strong>Nazwa grupy</strong>
          <span className="conversation-settings__hint">{displayName}</span>
        </div>
      </div>

      <div className="conversation-settings__group">
        <div>
          <strong>Uczestnicy</strong>
          <span className="conversation-settings__hint">
            {activeGroupConversation.participantCount} uczestnikow
          </span>
        </div>
      </div>
    </section>
  );
}
