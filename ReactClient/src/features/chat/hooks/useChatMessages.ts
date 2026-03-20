import type { KeyboardEvent } from "react";
import { useState } from "react";
import type { ChatMessage, MessageSender } from "../../../types/chat";
import type { PresenceChangedEvent, RealtimeChatMessage } from "../../../types/realtime";

function createMessage(
  sender: MessageSender,
  text: string,
  createdAt = new Date().toISOString(),
  id: string = crypto.randomUUID(),
): ChatMessage {
  return {
    id,
    sender,
    text,
    createdAt,
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

  const receiveRealtimeMessage = (payload: RealtimeChatMessage) => {
    setMessages((current) => {
      if (current.some((message) => message.id === payload.messageId)) {
        return current;
      }

      return [
        ...current,
        createMessage(
          "other",
          `${payload.senderDisplayName}: ${payload.text}`,
          payload.sentAtUtc,
          payload.messageId,
        ),
      ];
    });
  };

  const receivePresenceChanged = (payload: PresenceChangedEvent) => {
    appendMessage("system", `Obecnosc uzytkownika ${payload.userId} zmienila sie na ${payload.status}.`);
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
    receiveRealtimeMessage,
    receivePresenceChanged,
  };
}
