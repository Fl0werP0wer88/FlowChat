import { useQueryClient } from "@tanstack/react-query";
import { useMemo } from "react";
import { toast } from "sonner";
import { useAuthStore } from "../store/authStore";
import { useChatSelectionStore } from "../store/chatSelectionStore";
import type { Contact } from "../types/contacts";
import type { PresenceChangedEvent } from "../types/realtime";
import { resolveOwnerUserId } from "../utils/authUtils";
import type { SearchUserResult } from "../api/userProfileService";
import { useAddContactByUserIdMutation } from "./mutations/useAddContactByUserIdMutation";
import { useAddContactMutation } from "./mutations/useAddContactMutation";
import { useContactsQuery } from "./queries/useContactsQuery";

interface UseContactsResult {
  contacts: Contact[];
  activeContact: Contact | null;
  isLoadingContacts: boolean;
  addContact: (user: SearchUserResult) => Promise<void>;
  applyPresenceChanged: (payload: PresenceChangedEvent) => void;
  updateContactConversationId: (contactUserId: string, conversationId: string) => void;
}

const noopCallbacks = { onSuccess: () => {}, onError: (_message: string) => {} };

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
    () => contacts.find((contact) => contact.id === activeContactId) ?? null,
    [activeContactId, contacts],
  );

  const addContactMutation = useAddContactMutation(accessToken, noopCallbacks);
  const addContactByUserIdMutation = useAddContactByUserIdMutation(accessToken, noopCallbacks);

  const addContact = async (user: SearchUserResult): Promise<void> => {
    if (!accessToken || !ownerUserId) {
      toast.error("Brakuje aktywnej sesji potrzebnej do dodania kontaktu.");
      return;
    }

    try {
      if (user.userProfileId) {
        await addContactByUserIdMutation.mutateAsync(user.userProfileId);
      } else {
        const value = user.friendlyUserId.trim();
        if (!value) {
          toast.error("Wpisz email lub User Id uzytkownika.");
          return;
        }
        await addContactMutation.mutateAsync(value);
      }
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
    updateContactConversationId,
  };
}
