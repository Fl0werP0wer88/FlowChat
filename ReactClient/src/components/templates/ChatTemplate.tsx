import type { ReactNode } from "react";

interface ChatTemplateProps {
  header: ReactNode;
  conversation: ReactNode;
  sidebar: ReactNode;
}

export function ChatTemplate({ header, conversation, sidebar }: ChatTemplateProps) {
  return (
    <section className="chat-shell">
      {header}
      <div className="chat-content">
        {conversation}
        {sidebar}
      </div>
    </section>
  );
}
