import { useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
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
  activeContact: Contact | null;
  isAddingContact: boolean;
  isLoadingContacts: boolean;
  notice: ContactsNotice | null;
  addContactByEmail: (email: string) => Promise<boolean>;
  addContactByFriendlyId: (friendlyUserId: string) => Promise<boolean>;
  addContactByUserId: (userId: string) => Promise<boolean>;
  selectContact: (contact: Contact) => void;
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

  const addContactByEmail = async (email: string): Promise<boolean> => {
    const trimmedEmail = email.trim();

    if (!trimmedEmail) {
      setNotice({ kind: "error", message: "Wpisz email uzytkownika." });
      return false;
    }

    if (!accessToken || !ownerUserId) {
      setNotice({ kind: "error", message: "Brakuje aktywnej sesji potrzebnej do dodania kontaktu." });
      return false;
    }

    setNotice(null);

    try {
      await addContactMutation.mutateAsync(trimmedEmail);
      return true;
    } catch {
      return false;
    }
  };

  const addContactByFriendlyId = async (friendlyUserId: string): Promise<boolean> => {
    const trimmedFriendlyUserId = friendlyUserId.trim();

    if (!trimmedFriendlyUserId) {
      setNotice({ kind: "error", message: "Wpisz User Id uzytkownika." });
      return false;
    }

    if (!accessToken || !ownerUserId) {
      setNotice({ kind: "error", message: "Brakuje aktywnej sesji potrzebnej do dodania kontaktu." });
      return false;
    }

    setNotice(null);

    try {
      await addContactMutation.mutateAsync(trimmedFriendlyUserId);
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

  const selectContact = (contact: Contact) => {
    setActiveContactId(contact.id);
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
    activeContact,
    isAddingContact: addContactMutation.isPending || addContactByUserIdMutation.isPending,
    isLoadingContacts,
    notice,
    addContactByEmail,
    addContactByFriendlyId,
    addContactByUserId: addContactByUserIdAction,
    selectContact,
    applyPresenceChanged,
    clearNotice,
    updateContactConversationId,
    searchUsers: searchUsersAction,
  };
}
