import { useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { useAuthStore } from "../../../store/authStore";
import type { Contact } from "../../../types/contacts";
import type { PresenceChangedEvent } from "../../../types/realtime";
import { resolveOwnerUserId } from "../../../utils/authUtils";
import type { SearchUserResult } from "../../users/api";
import { useAddContactByUserIdMutation } from "../queries/useAddContactByUserIdMutation";
import { useAddContactMutation } from "../queries/useAddContactMutation";
import { useContactsQuery } from "../queries/useContactsQuery";

type ContactNotification = { kind: "error" | "info"; message: string };

interface UseContactsResult {
  contacts: Contact[];
  activeContact: Contact | null;
  isLoadingContacts: boolean;
  addContact: (user: SearchUserResult) => Promise<ContactNotification>;
  selectContact: (contact: Contact) => void;
  applyPresenceChanged: (payload: PresenceChangedEvent) => void;
  updateContactConversationId: (contactUserId: string, conversationId: string) => void;
}

const noopCallbacks = { onSuccess: () => {}, onError: (_message: string) => {} };

export function useContacts(): UseContactsResult {
  const accessToken = useAuthStore((s) => s.accessToken) ?? "";
  const ownerUserId = resolveOwnerUserId(accessToken);
  const queryClient = useQueryClient();
  const [activeContactId, setActiveContactId] = useState<string | null>(null);

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

  const addContact = async (user: SearchUserResult): Promise<ContactNotification> => {
    if (!accessToken || !ownerUserId) {
      return { kind: "error", message: "Brakuje aktywnej sesji potrzebnej do dodania kontaktu." };
    }

    try {
      if (user.userProfileId) {
        await addContactByUserIdMutation.mutateAsync(user.userProfileId);
      } else {
        const value = user.friendlyUserId.trim();
        if (!value) {
          return { kind: "error", message: "Wpisz email lub User Id uzytkownika." };
        }
        await addContactMutation.mutateAsync(value);
      }
      return { kind: "info", message: "Kontakt zostal dodany." };
    } catch (error) {
      const message = error instanceof Error ? error.message : "Nie udalo sie dodac kontaktu.";
      return { kind: "error", message };
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

  const selectContact = (contact: Contact) => {
    setActiveContactId(contact.id);
  };

  return {
    contacts,
    activeContact,
    isLoadingContacts,
    addContact,
    selectContact,
    applyPresenceChanged,
    updateContactConversationId,
  };
}
