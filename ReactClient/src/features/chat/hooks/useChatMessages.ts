import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { KeyboardEvent } from "react";
import { useEffect, useRef, useState } from "react";
import { useAuthStore } from "../../../store/authStore";
import type { ChatMessage, MessageSender } from "../../../types/chat";
import type { Contact } from "../../../types/contacts";
import type { RealtimeChatMessage } from "../../../types/realtime";
import { resolveOwnerUserId } from "../../../utils/authUtils";
import { openDuetConversation, sendChatMessage } from "../api";
import type { ConversationMessage } from "../api";

interface ConversationCacheEntry {
  conversationId: string;
  messages: ChatMessage[];
}

function createMessage(
  sender: MessageSender,
  text: string,
  createdAt = new Date().toISOString(),
  id: string = crypto.randomUUID(),
  conversationId: string | null = null,
  senderUserId: string | null = null,
  senderDisplayName: string | null = null,
): ChatMessage {
  return { id, conversationId, senderUserId, senderDisplayName, sender, text, createdAt };
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
  } = useQuery<ConversationCacheEntry>({
    queryKey: ["conversation", activeContact?.userId],
    queryFn: async ({ signal }) => {
      const result = await openDuetConversation(
        activeContact!.userId,
        activeContact!.conversationId,
        accessToken,
        signal,
      );
      const orderedMessages = [...result.messages].reverse();
      return {
        conversationId: result.conversationId,
        messages: orderedMessages.map((msg) => mapConversationMessage(msg, ownerUserId)),
      };
    },
    enabled: Boolean(activeContact && accessToken),
    // Conversations are kept fresh via realtime events — disable background refetching
    staleTime: Infinity,
    gcTime: 5 * 60 * 1000,
  });

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

  const sendMessageMutation = useMutation({
    mutationFn: ({
      messageId,
      conversationId,
      text,
      senderDisplayName,
    }: {
      messageId: string;
      conversationId: string;
      text: string;
      senderDisplayName: string;
      sentAtUtc: string;
    }) => sendChatMessage({ id: messageId, conversationId, senderDisplayName, text }, accessToken),
    onSuccess: (result, variables) => {
      queryClient.setQueryData<ConversationCacheEntry>(
        ["conversation", activeContact?.userId],
        (current) => {
          if (!current) {
            return current;
          }

          if (current.messages.some((m) => m.id === result.messageId)) {
            return current;
          }

          return {
            ...current,
            messages: [
              ...current.messages,
              createMessage(
                "me",
                variables.text,
                variables.sentAtUtc,
                result.messageId,
                variables.conversationId,
                ownerUserId,
                userLogin,
              ),
            ],
          };
        },
      );
    },
    onError: (error) => {
      const message = error instanceof Error ? error.message : "Nie udalo sie wyslac wiadomosci.";
      setSendError(message);
    },
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
