import type { KeyboardEvent } from "react";
import { useState } from "react";
import type { ChatMessage, MessageSender } from "../../../types/chat";
import type { Contact } from "../../../types/contacts";
import type { RealtimeChatMessage } from "../../../types/realtime";
import { resolveOwnerUserId } from "../../../utils/authUtils";
import { openDuetConversation, sendChatMessage } from "../api";
import type { ConversationMessage } from "../api";

function createMessage(
  sender: MessageSender,
  text: string,
  createdAt = new Date().toISOString(),
  id: string = crypto.randomUUID(),
  conversationId: string | null = null,
  senderUserId: string | null = null,
  senderDisplayName: string | null = null,
): ChatMessage {
  return {
    id,
    conversationId,
    senderUserId,
    senderDisplayName,
    sender,
    text,
    createdAt,
  };
}

function mapConversationMessage(message: ConversationMessage, ownerUserId: string | null): ChatMessage {
  const sender = ownerUserId && message.senderUserId === ownerUserId ? "me" : "other";

  return createMessage(
    sender,
    message.text,
    message.sentAtUtc,
    message.id,
    message.conversationId,
    message.senderUserId,
    message.senderDisplayName,
  );
}

export function useChatMessages(accessToken: string, userLogin: string) {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [draft, setDraft] = useState("");
  const [activeContact, setActiveContact] = useState<Contact | null>(null);
  const [activeConversationId, setActiveConversationId] = useState<string | null>(null);
  const [conversationError, setConversationError] = useState<string | null>(null);
  const [isLoadingConversation, setIsLoadingConversation] = useState(false);
  const [isSendingMessage, setIsSendingMessage] = useState(false);
  const [sendError, setSendError] = useState<string | null>(null);
  const ownerUserId = resolveOwnerUserId(accessToken);

  const receiveRealtimeMessage = (payload: RealtimeChatMessage) => {
    if (payload.conversationId !== activeConversationId) {
      return;
    }

    setMessages((current) => {
      if (current.some((message) => message.id === payload.messageId)) {
        return current;
      }

      const sender = ownerUserId && payload.senderUserId === ownerUserId ? "me" : "other";

      return [
        ...current,
        createMessage(
          sender,
          payload.text,
          payload.sentAtUtc,
          payload.messageId,
          payload.conversationId,
          payload.senderUserId,
          payload.senderDisplayName,
        ),
      ];
    });
  };

  const sendDraft = async () => {
    const text = draft.trim();
    if (!text || !activeConversationId || !accessToken || !ownerUserId || isSendingMessage) {
      return;
    }

    const messageId = crypto.randomUUID();
    const sentAtUtc = new Date().toISOString();
    setIsSendingMessage(true);
    setSendError(null);

    try {
      const result = await sendChatMessage(
        {
          id: messageId,
          conversationId: activeConversationId,
          senderDisplayName: userLogin,
          text,
        },
        accessToken,
      );

      setMessages((current) => {
        if (current.some((message) => message.id === result.messageId)) {
          return current;
        }

        return [
          ...current,
          createMessage(
            "me",
            text,
            sentAtUtc,
            result.messageId,
            activeConversationId,
            ownerUserId,
            userLogin,
          ),
        ];
      });
      setDraft("");
    } catch (error) {
      const message = error instanceof Error ? error.message : "Nie udalo sie wyslac wiadomosci.";
      setSendError(message);
    } finally {
      setIsSendingMessage(false);
    }
  };

  const handleDraftKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      void sendDraft();
    }
  };

  const openContactConversation = async (
    contact: Contact,
    onConversationOpened?: (contactUserId: string, conversationId: string) => void,
  ) => {
    if (!accessToken || !ownerUserId) {
      setConversationError("Brakuje aktywnej sesji potrzebnej do otwarcia rozmowy.");
      return;
    }

    setActiveContact(contact);
    setConversationError(null);
    setSendError(null);
    setIsLoadingConversation(true);

    try {
      const result = await openDuetConversation(contact.userId, contact.conversationId, accessToken);
      const orderedMessages = [...result.messages].reverse();

      setActiveConversationId(result.conversationId);
      setActiveContact({
        ...contact,
        conversationId: result.conversationId,
      });
      setMessages(orderedMessages.map((message) => mapConversationMessage(message, ownerUserId)));
      onConversationOpened?.(contact.userId, result.conversationId);
    } catch (error) {
      const message = error instanceof Error ? error.message : "Nie udalo sie otworzyc rozmowy.";
      setConversationError(message);
      setMessages([]);
      setActiveConversationId(null);
    } finally {
      setIsLoadingConversation(false);
    }
  };

  return {
    messages,
    draft,
    activeContact,
    activeConversationId,
    conversationError,
    isLoadingConversation,
    isSendingMessage,
    sendError,
    setDraft,
    sendDraft,
    handleDraftKeyDown,
    receiveRealtimeMessage,
    openContactConversation,
  };
}
