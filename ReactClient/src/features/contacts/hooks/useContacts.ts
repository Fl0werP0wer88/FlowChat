import { useEffect, useRef, useState } from "react";
import { useAuthStore } from "../../../store/authStore";
import type { Contact } from "../../../types/contacts";
import type { PresenceChangedEvent } from "../../../types/realtime";
import { resolveOwnerUserId } from "../../../utils/authUtils";
import { addContact, addContactByUserId, fetchContacts, searchUsers } from "../api";
import type { SearchUserResult, SearchUsersCriteria } from "../api";

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

type ContactPresenceMap = Record<string, Contact["status"]>;

export function useContacts(): UseContactsResult {
  const accessToken = useAuthStore((s) => s.accessToken) ?? "";
  const [contacts, setContacts] = useState<Contact[]>([]);
  const [isLoadingContacts, setIsLoadingContacts] = useState(false);
  const [isAddingContact, setIsAddingContact] = useState(false);
  const [notice, setNotice] = useState<ContactsNotice | null>(null);
  const presenceStatusesRef = useRef<ContactPresenceMap>({});
  const ownerUserId = resolveOwnerUserId(accessToken);

  const clearNotice = () => {
    setNotice(null);
  };

  const applyStatusesToContacts = (loadedContacts: Contact[], statuses: ContactPresenceMap = presenceStatusesRef.current) =>
    loadedContacts.map((contact) => ({
      ...contact,
      status: statuses[contact.userId] ?? contact.status ?? "Invisible",
    }));

  useEffect(() => {
    let isActive = true;

    async function loadContacts() {
      if (!accessToken || !ownerUserId) {
        if (isActive) {
          setContacts([]);
          presenceStatusesRef.current = {};
          setNotice({
            kind: "error",
            message: "Nie udalo sie odczytac identyfikatora uzytkownika z sesji.",
          });
        }
        return;
      }

      if (isActive) {
        setIsLoadingContacts(true);
      }

      try {
        const loadedContacts = await fetchContacts(accessToken);
        if (!isActive) {
          return;
        }

        setContacts(applyStatusesToContacts(loadedContacts));
      } catch (error) {
        if (!isActive) {
          return;
        }

        const message = error instanceof Error ? error.message : "Nie udalo sie pobrac kontaktow.";
        setNotice({ kind: "error", message });
      } finally {
        if (isActive) {
          setIsLoadingContacts(false);
        }
      }
    }

    void loadContacts();

    return () => {
      isActive = false;
    };
  }, [accessToken, ownerUserId]);

  const applyPresenceChanged = (payload: PresenceChangedEvent) => {
    presenceStatusesRef.current = {
      ...presenceStatusesRef.current,
      [payload.userId]: payload.status,
    };

    setContacts((current) =>
      current.map((contact) =>
        contact.userId === payload.userId
          ? {
            ...contact,
            status: payload.status,
          }
          : contact
      ));
  };

  const updateContactConversationId = (contactUserId: string, conversationId: string) => {
    setContacts((current) =>
      current.map((contact) =>
        contact.userId === contactUserId
          ? {
            ...contact,
            conversationId,
          }
          : contact
      ));
  };

  const addContactByLookup = async (lookupValue: string): Promise<boolean> => {
    const trimmedLookupValue = lookupValue.trim();

    if (!trimmedLookupValue) {
      setNotice({ kind: "error", message: "Wpisz User Id albo email." });
      return false;
    }

    if (!accessToken || !ownerUserId) {
      setNotice({
        kind: "error",
        message: "Brakuje aktywnej sesji potrzebnej do dodania kontaktu.",
      });
      return false;
    }

    setIsAddingContact(true);
    setNotice(null);

    try {
      await addContact(trimmedLookupValue, accessToken);
      const loadedContacts = await fetchContacts(accessToken);
      setContacts(applyStatusesToContacts(loadedContacts));
      setNotice({ kind: "info", message: "Kontakt zostal dodany." });
      return true;
    } catch (error) {
      const message = error instanceof Error ? error.message : "Nie udalo sie dodac kontaktu.";
      setNotice({ kind: "error", message });
      return false;
    } finally {
      setIsAddingContact(false);
    }
  };

  const addContactByUserIdAction = async (userId: string): Promise<boolean> => {
    const trimmedUserId = userId.trim();

    if (!trimmedUserId) {
      setNotice({ kind: "error", message: "Brakuje identyfikatora uzytkownika." });
      return false;
    }

    if (!accessToken || !ownerUserId) {
      setNotice({
        kind: "error",
        message: "Brakuje aktywnej sesji potrzebnej do dodania kontaktu.",
      });
      return false;
    }

    setIsAddingContact(true);
    setNotice(null);

    try {
      await addContactByUserId(trimmedUserId, accessToken);
      const loadedContacts = await fetchContacts(accessToken);
      setContacts(applyStatusesToContacts(loadedContacts));
      setNotice({ kind: "info", message: "Kontakt zostal dodany." });
      return true;
    } catch (error) {
      const message = error instanceof Error ? error.message : "Nie udalo sie dodac kontaktu.";
      setNotice({ kind: "error", message });
      return false;
    } finally {
      setIsAddingContact(false);
    }
  };

  const searchUsersAction = async (
    criteria: SearchUsersCriteria,
    signal?: AbortSignal,
  ): Promise<SearchUserResult[]> => {
    if (!accessToken || !ownerUserId) {
      throw new Error("Brakuje aktywnej sesji potrzebnej do wyszukiwania uzytkownikow.");
    }

    return await searchUsers(criteria, accessToken, signal);
  };

  return {
    contacts,
    isAddingContact,
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
