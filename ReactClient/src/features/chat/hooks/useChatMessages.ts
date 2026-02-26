import type { KeyboardEvent } from "react";
import { useState } from "react";
import type { ChatMessage, MessageSender } from "../../../types/chat";

function createMessage(sender: MessageSender, text: string): ChatMessage {
  return {
    id: crypto.randomUUID(),
    sender,
    text,
    createdAt: new Date().toISOString(),
  };
}

export function useChatMessages() {
  const [messages, setMessages] = useState<ChatMessage[]>([
    createMessage("system", "Witaj w FlowChat. Po uruchomieniu backendu tutaj pojawi sie historia rozmow."),
  ]);
  const [draft, setDraft] = useState("");

  const appendMessage = (sender: MessageSender, text: string) => {
    setMessages((current) => [...current, createMessage(sender, text)]);
  };

  const sendDraft = () => {
    const trimmedDraft = draft.trim();
    if (!trimmedDraft) {
      return;
    }

    appendMessage("me", trimmedDraft);
    setDraft("");

    setTimeout(() => {
      appendMessage("system", "Echo: backend chat endpoint nie jest jeszcze podpiety.");
    }, 200);
  };

  const handleDraftKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      sendDraft();
    }
  };

  return {
    messages,
    draft,
    setDraft,
    sendDraft,
    handleDraftKeyDown,
  };
}
