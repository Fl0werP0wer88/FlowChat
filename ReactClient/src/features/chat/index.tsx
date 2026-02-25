import { ChatHeader } from "../../components/organisms/ChatHeader";
import { ChatTemplate } from "../../components/templates/ChatTemplate";
import { ContactsPanel, useContacts } from "../contacts";
import { ConversationPanel } from "./components/ConversationPanel";
import { useChatMessages } from "./hooks/useChatMessages";

interface ChatFeatureProps {
  userLogin: string;
  onLogout: () => void;
}

export function ChatFeature({ userLogin, onLogout }: ChatFeatureProps) {
  const contacts = useContacts();
  const chat = useChatMessages();

  return (
    <ChatTemplate
      header={<ChatHeader userLogin={userLogin} onLogout={onLogout} />}
      conversation={
        <ConversationPanel
          messages={chat.messages}
          draft={chat.draft}
          onDraftChange={chat.setDraft}
          onDraftKeyDown={chat.handleDraftKeyDown}
          onSendDraft={chat.sendDraft}
        />
      }
      sidebar={<ContactsPanel contacts={contacts} />}
    />
  );
}
