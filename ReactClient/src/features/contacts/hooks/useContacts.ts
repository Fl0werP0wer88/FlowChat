import { useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { useAuthStore } from "../../../store/authStore";
import type { Contact } from "../../../types/contacts";
import type { PresenceChangedEvent } from "../../../types/realtime";
import { resolveOwnerUserId } from "../../../utils/authUtils";
import type { SearchUserResult, SearchUsersCriteria } from "../api";
import { searchUsers } from "../api";
import { useAddContactByUserIdMutation } from "../queries/useAddContactByUserIdMutation";
import { useAddContactMutation } from "../queries/useAddContactMutation";
import { useContactsQuery } from "../queries/useContactsQuery";

interface ContactsNotice {
  kind: "error" | "info";
  message: string;
}

interface UseContactsResult {
  contacts: Contact[];
  isAddingContact: boolean;
  isLoadingContacts: boolean;
  notice: ContactsNotice | null;
  addContactByLookup: (lookupValue: string) => Promise<boolean>;
  addContactByUserId: (userId: string) => Promise<boolean>;
  clearNotice: () => void;
  applyPresenceChanged: (payload: PresenceChangedEvent) => void;
  updateContactConversationId: (contactUserId: string, conversationId: string) => void;
  searchUsers: (criteria: SearchUsersCriteria, signal?: AbortSignal) => Promise<SearchUserResult[]>;
}

export function useContacts(): UseContactsResult {
  const accessToken = useAuthStore((s) => s.accessToken) ?? "";
  const ownerUserId = resolveOwnerUserId(accessToken);
  const queryClient = useQueryClient();
  const [notice, setNotice] = useState<ContactsNotice | null>(null);

  const clearNotice = () => setNotice(null);

  const noticeCallbacks = {
    onSuccess: () => setNotice({ kind: "info", message: "Kontakt zostal dodany." }),
    onError: (message: string) => setNotice({ kind: "error", message }),
  };

  const { data: contacts = [], isLoading: isLoadingContacts } = useContactsQuery(
    accessToken,
    Boolean(accessToken && ownerUserId),
  );

  const addContactMutation = useAddContactMutation(accessToken, noticeCallbacks);
  const addContactByUserIdMutation = useAddContactByUserIdMutation(accessToken, noticeCallbacks);

  const addContactByLookup = async (lookupValue: string): Promise<boolean> => {
    const trimmedLookupValue = lookupValue.trim();

    if (!trimmedLookupValue) {
      setNotice({ kind: "error", message: "Wpisz User Id albo email." });
      return false;
    }

    if (!accessToken || !ownerUserId) {
      setNotice({ kind: "error", message: "Brakuje aktywnej sesji potrzebnej do dodania kontaktu." });
      return false;
    }

    setNotice(null);

    try {
      await addContactMutation.mutateAsync(trimmedLookupValue);
      return true;
    } catch {
      return false;
    }
  };

  const addContactByUserIdAction = async (userId: string): Promise<boolean> => {
    const trimmedUserId = userId.trim();

    if (!trimmedUserId) {
      setNotice({ kind: "error", message: "Brakuje identyfikatora uzytkownika." });
      return false;
    }

    if (!accessToken || !ownerUserId) {
      setNotice({ kind: "error", message: "Brakuje aktywnej sesji potrzebnej do dodania kontaktu." });
      return false;
    }

    setNotice(null);

    try {
      await addContactByUserIdMutation.mutateAsync(trimmedUserId);
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

  const searchUsersAction = async (
    criteria: SearchUsersCriteria,
    signal?: AbortSignal,
  ): Promise<SearchUserResult[]> => {
    if (!accessToken || !ownerUserId) {
      throw new Error("Brakuje aktywnej sesji potrzebnej do wyszukiwania uzytkownikow.");
    }

    return searchUsers(criteria, accessToken, signal);
  };

  return {
    contacts,
    isAddingContact: addContactMutation.isPending || addContactByUserIdMutation.isPending,
    isLoadingContacts,
    notice,
    addContactByLookup,
    addContactByUserId: addContactByUserIdAction,
    applyPresenceChanged,
    clearNotice,
    updateContactConversationId,
    searchUsers: searchUsersAction,
  };
}
