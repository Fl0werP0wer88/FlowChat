import { useQueryClient } from "@tanstack/react-query";
import type { KeyboardEvent } from "react";
import { useEffect, useRef, useState } from "react";
import { useAuthStore } from "../../../store/authStore";
import type { Contact } from "../../../types/contacts";
import type { RealtimeChatMessage } from "../../../types/realtime";
import { resolveOwnerUserId } from "../../../utils/authUtils";
import type { ConversationCacheEntry } from "../queries/conversationCache";
import { createMessage } from "../queries/conversationCache";
import { useConversationQuery } from "../queries/useConversationQuery";
import { useSendMessageMutation } from "../queries/useSendMessageMutation";

export function useChatMessages() {
  const accessToken = useAuthStore((s) => s.accessToken) ?? "";
  const userLogin = useAuthStore((s) => s.login) ?? "Uzytkownik";
  const ownerUserId = resolveOwnerUserId(accessToken);
  const queryClient = useQueryClient();

  const [draft, setDraft] = useState("");
  const [activeContact, setActiveContact] = useState<Contact | null>(null);
  const [sendError, setSendError] = useState<string | null>(null);

  // Stores the callback supplied per-call to openContactConversation, fired once when query resolves
  const onConversationOpenedRef = useRef<((contactUserId: string, conversationId: string) => void) | null>(null);
  const lastNotifiedKeyRef = useRef<string | null>(null);

  const {
    data: conversationData,
    isLoading: isLoadingConversation,
    error: conversationQueryError,
  } = useConversationQuery(activeContact, accessToken, ownerUserId);

  useEffect(() => {
    if (!conversationData || !activeContact) {
      return;
    }

    const key = `${activeContact.userId}:${conversationData.conversationId}`;
    if (key === lastNotifiedKeyRef.current) {
      return;
    }

    lastNotifiedKeyRef.current = key;
    onConversationOpenedRef.current?.(activeContact.userId, conversationData.conversationId);
    onConversationOpenedRef.current = null;
  }, [conversationData, activeContact]);

  const sendMessageMutation = useSendMessageMutation({
    accessToken,
    activeContactUserId: activeContact?.userId,
    ownerUserId,
    userLogin,
    onError: setSendError,
  });

  const receiveRealtimeMessage = (payload: RealtimeChatMessage) => {
    if (!conversationData || payload.conversationId !== conversationData.conversationId) {
      return;
    }

    queryClient.setQueryData<ConversationCacheEntry>(
      ["conversation", activeContact?.userId],
      (current) => {
        if (!current) {
          return current;
        }

        if (current.messages.some((m) => m.id === payload.messageId)) {
          return current;
        }

        const sender = ownerUserId && payload.senderUserId === ownerUserId ? "me" : "other";
        return {
          ...current,
          messages: [
            ...current.messages,
            createMessage(
              sender,
              payload.text,
              payload.sentAtUtc,
              payload.messageId,
              payload.conversationId,
              payload.senderUserId,
              payload.senderDisplayName,
            ),
          ],
        };
      },
    );
  };

  const sendDraft = async () => {
    const text = draft.trim();
    const conversationId = conversationData?.conversationId;

    if (!text || !conversationId || !accessToken || !ownerUserId || sendMessageMutation.isPending) {
      return;
    }

    const messageId = crypto.randomUUID();
    const sentAtUtc = new Date().toISOString();
    setSendError(null);

    try {
      await sendMessageMutation.mutateAsync({ messageId, conversationId, text, senderDisplayName: userLogin, sentAtUtc });
      setDraft("");
    } catch {
      // error is handled in onError
    }
  };

  const handleDraftKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      void sendDraft();
    }
  };

  const openContactConversation = (
    contact: Contact,
    onConversationOpened?: (contactUserId: string, conversationId: string) => void,
  ) => {
    onConversationOpenedRef.current = onConversationOpened ?? null;
    setSendError(null);
    setActiveContact(contact);
  };

  const conversationError =
    conversationQueryError instanceof Error ? conversationQueryError.message : null;

  return {
    messages: conversationData?.messages ?? [],
    draft,
    activeContact,
    activeConversationId: conversationData?.conversationId ?? null,
    conversationError,
    isLoadingConversation,
    isSendingMessage: sendMessageMutation.isPending,
    sendError,
    setDraft,
    sendDraft,
    handleDraftKeyDown,
    receiveRealtimeMessage,
    openContactConversation,
  };
}
