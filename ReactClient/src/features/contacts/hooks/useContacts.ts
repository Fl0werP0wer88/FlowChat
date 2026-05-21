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

interface ContactsNotice {
  kind: "error" | "info";
  message: string;
}

interface UseContactsResult {
  contacts: Contact[];
  activeContact: Contact | null;
  isAddingContact: boolean;
  isLoadingContacts: boolean;
  notice: ContactsNotice | null;
  addContact: (user: SearchUserResult) => Promise<boolean>;
  selectContact: (contact: Contact) => void;
  clearNotice: () => void;
  applyPresenceChanged: (payload: PresenceChangedEvent) => void;
  updateContactConversationId: (contactUserId: string, conversationId: string) => void;
}

export function useContacts(): UseContactsResult {
  const accessToken = useAuthStore((s) => s.accessToken) ?? "";
  const ownerUserId = resolveOwnerUserId(accessToken);
  const queryClient = useQueryClient();
  const [notice, setNotice] = useState<ContactsNotice | null>(null);
  const [activeContactId, setActiveContactId] = useState<string | null>(null);

  const clearNotice = () => setNotice(null);

  const noticeCallbacks = {
    onSuccess: () => setNotice({ kind: "info", message: "Kontakt zostal dodany." }),
    onError: (message: string) => setNotice({ kind: "error", message }),
  };

  const { data: contacts = [], isLoading: isLoadingContacts } = useContactsQuery(
    accessToken,
    Boolean(accessToken && ownerUserId),
  );
  const activeContact = useMemo(
    () => contacts.find((contact) => contact.id === activeContactId) ?? null,
    [activeContactId, contacts],
  );

  const addContactMutation = useAddContactMutation(accessToken, noticeCallbacks);
  const addContactByUserIdMutation = useAddContactByUserIdMutation(accessToken, noticeCallbacks);

  const addContact = async (user: SearchUserResult): Promise<boolean> => {
    if (!accessToken || !ownerUserId) {
      setNotice({ kind: "error", message: "Brakuje aktywnej sesji potrzebnej do dodania kontaktu." });
      return false;
    }

    setNotice(null);

    try {
      if (user.userProfileId) {
        await addContactByUserIdMutation.mutateAsync(user.userProfileId);
      } else {
        const value = user.friendlyUserId.trim();
        if (!value) {
          setNotice({ kind: "error", message: "Wpisz email lub User Id uzytkownika." });
          return false;
        }
        await addContactMutation.mutateAsync(value);
      }
      return true;
    } catch {
      return false;
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
    isAddingContact: addContactMutation.isPending || addContactByUserIdMutation.isPending,
    isLoadingContacts,
    notice,
    addContact,
    selectContact,
    applyPresenceChanged,
    clearNotice,
    updateContactConversationId,
  };
}
