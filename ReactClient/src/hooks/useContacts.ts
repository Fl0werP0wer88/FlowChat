import { useQueryClient } from "@tanstack/react-query";
import { useMemo } from "react";
import { toast } from "sonner";
import { useAuthStore } from "../store/authStore";
import { useChatSelectionStore } from "../store/chatSelectionStore";
import type { Contact } from "../types/contacts";
import type { ChatMessageReceivedEvent, PresenceChangedEvent } from "../types/realtime";
import type { SearchUserResult } from "../types/users";
import { resolveOwnerUserId } from "../utils/authUtils";
import { calculateUnreadCount } from "../utils/chatUtils";
import { useAddContactMutation } from "./mutations/useAddContactMutation";
import { useContactsQuery } from "./queries/useContactsQuery";

interface UseContactsResult {
  contacts: Contact[];
  activeContact: Contact | null;
  isLoadingContacts: boolean;
  addContact: (user: SearchUserResult) => Promise<void>;
  applyPresenceChanged: (payload: PresenceChangedEvent) => void;
  applyRealtimeMessage: (payload: ChatMessageReceivedEvent, activeDuetConversationId: string | null) => void;
  invalidateDuetConversationsList: () => void;
  updateContactConversationId: (contactUserId: string, conversationId: string) => void;
}

const noopCallbacks = { onSuccess: () => {}, onError: (_message: string) => {} };

function withUnreadCount(contact: Contact): Contact {
  return {
    ...contact,
    unreadCount: calculateUnreadCount(contact.currentMsgSeqNum, contact.lastReadMsgSeqNum),
  };
}

export function useContacts(): UseContactsResult {
  const accessToken = useAuthStore((s) => s.accessToken) ?? "";
  const ownerUserId = resolveOwnerUserId(accessToken);
  const queryClient = useQueryClient();
  const activeContactId = useChatSelectionStore((s) => s.activeContactId);

  const { data: contacts = [], isLoading: isLoadingContacts } = useContactsQuery(
    accessToken,
    Boolean(accessToken && ownerUserId),
  );
  const activeContact = useMemo(
    () => contacts.find((contact) => contact.conversationId === activeContactId) ?? null,
    [activeContactId, contacts],
  );

  const addContactMutation = useAddContactMutation(accessToken, noopCallbacks);

  const addContact = async (user: SearchUserResult): Promise<void> => {
    if (!accessToken || !ownerUserId) {
      toast.error("Brakuje aktywnej sesji potrzebnej do dodania kontaktu.");
      return;
    }

    try {
      await addContactMutation.mutateAsync(user.userProfileId);
      toast.success("Kontakt zostal dodany.");
    } catch (error) {
      const message = error instanceof Error ? error.message : "Nie udalo sie dodac kontaktu.";
      toast.error(message);
    }
  };

  const applyPresenceChanged = (payload: PresenceChangedEvent) => {
    queryClient.setQueryData<Contact[]>(["contacts"], (current = []) =>
      current.map((contact) =>
        contact.userId === payload.userId ? { ...contact, status: payload.status } : contact
      )
    );
  };

  const applyRealtimeMessage = (
    payload: ChatMessageReceivedEvent,
    _activeDuetConversationId: string | null,
  ) => {
    queryClient.setQueryData<Contact[]>(["contacts"], (current = []) =>
      current.map((contact) => {
        if (contact.conversationId !== payload.conversationId) {
          return contact;
        }

        const currentMsgSeqNum = Math.max(contact.currentMsgSeqNum, payload.sequenceNum);
        return withUnreadCount({
          ...contact,
          currentMsgSeqNum,
        });
      }),
    );
  };

  const invalidateDuetConversationsList = () => {
    void queryClient.invalidateQueries({ queryKey: ["contacts"] });
  };

  const updateContactConversationId = (contactUserId: string, conversationId: string) => {
    queryClient.setQueryData<Contact[]>(["contacts"], (current = []) =>
      current.map((contact) =>
        contact.userId === contactUserId ? { ...contact, conversationId } : contact
      )
    );
  };

  return {
    contacts,
    activeContact,
    isLoadingContacts,
    addContact,
    applyPresenceChanged,
    applyRealtimeMessage,
    invalidateDuetConversationsList,
    updateContactConversationId,
  };
}
