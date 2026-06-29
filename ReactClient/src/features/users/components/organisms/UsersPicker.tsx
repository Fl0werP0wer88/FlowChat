import { useEffect, useRef, useState } from "react";
import type { KeyboardEventHandler } from "react";
import { useAuthStore } from "../../../../store/authStore";
import { isEmail } from "../../../../utils/stringUtils";
import {
  getUserProfileByEmail,
  getUserProfileByFriendlyUserId,
  getUserProfileById,
  searchUsers,
  type SearchUserResult,
  type SearchUsersCriteria,
} from "../../api";
import { UsersPickerFooter } from "../molecules/UsersPickerFooter";
import { UsersPickerHeader } from "../molecules/UsersPickerHeader";

type UsersPickerNotice = { kind: "error" | "info"; message: string };

interface UsersPickerProps {
  confirmLabel?: string;
  isOpen: boolean;
  onConfirm: (selectedUsers: SearchUserResult[]) => Promise<UsersPickerNotice | void>;
  singlePick?: boolean;
}

export function UsersPicker({
  confirmLabel = "Wybierz",
  isOpen,
  onConfirm,
  singlePick = false,
}: UsersPickerProps) {
  const accessToken = useAuthStore((s) => s.accessToken) ?? "";
  const [emailOrFriendlyId, setEmailOrFriendlyId] = useState("");
  const [searchCriteria, setSearchCriteria] = useState<SearchUsersCriteria>({
    firstName: "",
    lastName: "",
    organization: "",
  });
  const [searchResults, setSearchResults] = useState<SearchUserResult[]>([]);
  const [isSearchingUsers, setIsSearchingUsers] = useState(false);
  const [isLookupProcessing, setIsLookupProcessing] = useState(false);
  const [processingUserProfileId, setProcessingUserProfileId] = useState<string | null>(null);
  const [searchNotice, setSearchNotice] = useState<string | null>(null);
  const [processNotice, setProcessNotice] = useState<UsersPickerNotice | null>(null);
  const [selectedMembers, setSelectedMembers] = useState<SearchUserResult[]>([]);
  const [isConfirming, setIsConfirming] = useState(false);
  const [confirmNotice, setConfirmNotice] = useState<UsersPickerNotice | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const firstNameInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (isOpen) {
      window.requestAnimationFrame(() => inputRef.current?.focus());
    }
  }, [isOpen]);

  useEffect(() => {
    if (isOpen) {
      return;
    }

    setEmailOrFriendlyId("");
    setSearchCriteria({
      firstName: "",
      lastName: "",
      organization: "",
    });
    setSearchResults([]);
    setSearchNotice(null);
    setProcessNotice(null);
    setIsLookupProcessing(false);
    setSelectedMembers([]);
    setConfirmNotice(null);
    setIsConfirming(false);
  }, [isOpen]);

  useEffect(() => {
    if (!isOpen) {
      setSearchResults([]);
      setSearchNotice(null);
      setIsSearchingUsers(false);
      return;
    }

    const hasAnyCriteria = Object.values(searchCriteria).some((value) => value.trim().length > 0);
    if (!hasAnyCriteria) {
      setSearchResults([]);
      setSearchNotice(null);
      setIsSearchingUsers(false);
      return;
    }

    const abortController = new AbortController();
    const timeoutId = window.setTimeout(() => {
      setIsSearchingUsers(true);
      setSearchNotice(null);

      if (!accessToken) {
        setSearchResults([]);
        setSearchNotice("Brakuje aktywnej sesji potrzebnej do wyszukiwania uzytkownikow.");
        setIsSearchingUsers(false);
        return;
      }

      void searchUsers(searchCriteria, accessToken, abortController.signal)
        .then((results) => {
          setSearchResults(results);
          if (results.length === 0) {
            setSearchNotice("Nie znaleziono uzytkownikow dla podanych danych.");
          }
        })
        .catch((error) => {
          if (abortController.signal.aborted) {
            return;
          }

          const message = error instanceof Error ? error.message : "Nie udalo sie wyszukac uzytkownikow.";
          setSearchResults([]);
          setSearchNotice(message);
        })
        .finally(() => {
          if (!abortController.signal.aborted) {
            setIsSearchingUsers(false);
          }
        });
    }, 280);

    return () => {
      abortController.abort();
      window.clearTimeout(timeoutId);
    };
  }, [accessToken, isOpen, searchCriteria]);

  const addMember = (user: SearchUserResult): UsersPickerNotice => {
    if (selectedMembers.some((member) => member.userProfileId === user.userProfileId)) {
      return { kind: "info", message: "Uzytkownik jest juz na liscie." };
    }

    setSelectedMembers((current) => [...current, user]);
    return { kind: "info", message: "Dodano do grupy." };
  };

  const removeMember = (userProfileId: string) => {
    setSelectedMembers((current) => current.filter((member) => member.userProfileId !== userProfileId));
  };

  const handleConfirm = async (users: SearchUserResult[]) => {
    setIsConfirming(true);
    setConfirmNotice(null);
    try {
      const notice = await onConfirm(users);
      if (notice) {
        setConfirmNotice(notice);
      }
    } catch (error) {
      const message = error instanceof Error ? error.message : "Nie udalo sie wykonac akcji.";
      setConfirmNotice({ kind: "error", message });
    } finally {
      setIsConfirming(false);
    }
  };

  const submitLookup = async () => {
    const trimmedLookup = emailOrFriendlyId.trim();
    if (!trimmedLookup) {
      return;
    }

    if (!accessToken) {
      setSearchNotice("Brakuje aktywnej sesji potrzebnej do pobrania profilu uzytkownika.");
      return;
    }

    setIsLookupProcessing(true);
    try {
      const user = isEmail(trimmedLookup)
        ? await getUserProfileByEmail(trimmedLookup, accessToken)
        : await getUserProfileByFriendlyUserId(trimmedLookup, accessToken);

      if (singlePick) {
        await handleConfirm([user]);
      } else {
        setProcessNotice(addMember(user));
      }
    } catch (error) {
      const message = error instanceof Error ? error.message : "Nie udalo sie pobrac profilu uzytkownika.";
      setSearchNotice(message);
    } finally {
      setIsLookupProcessing(false);
    }
  };

  const handleLookupKeyDown: KeyboardEventHandler<HTMLInputElement> = async (event) => {
    if (event.key !== "Enter") {
      return;
    }

    event.preventDefault();
    await submitLookup();
  };

  const handleSearchFieldChange = (field: keyof SearchUsersCriteria, value: string) => {
    setSearchCriteria((current) => ({
      ...current,
      [field]: value,
    }));
  };

  const handleSearchResultClick = async (result: SearchUserResult) => {
    if (!accessToken) {
      setSearchNotice("Brakuje aktywnej sesji potrzebnej do pobrania profilu uzytkownika.");
      return;
    }

    setProcessingUserProfileId(result.userProfileId);
    setSearchNotice(null);

    try {
      const userProfile = await getUserProfileById(result.userProfileId, accessToken);
      if (singlePick) {
        await handleConfirm([userProfile]);
      } else {
        setProcessNotice(addMember(userProfile));
      }
    } catch (error) {
      const message = error instanceof Error ? error.message : "Nie udalo sie pobrac profilu uzytkownika.";
      setSearchNotice(message);
    } finally {
      setProcessingUserProfileId(null);
    }
  };

  return (
    <div className="users-picker">
      {singlePick ? null : <UsersPickerHeader onRemoveMember={removeMember} selectedMembers={selectedMembers} />}

      <div className="users-picker__scroll">
        <div className="users-picker__search-label">
          <span aria-hidden="true" className="material-symbols-rounded">person_add</span>
          <span>Dodaj Uzytkownika</span>
        </div>

        <div className="field-shell contacts-composer__search">
          <button
            aria-label="Dodaj uzytkownika z podanej wartosci"
            className="contacts-composer__search-button"
            disabled={isLookupProcessing}
            onClick={() => void submitLookup()}
            type="button"
          >
            <span aria-hidden="true" className="material-symbols-rounded">person_add</span>
          </button>
          <input
            className="contacts-composer__input"
            disabled={isLookupProcessing}
            onChange={(event) => setEmailOrFriendlyId(event.target.value)}
            onKeyDown={(event) => void handleLookupKeyDown(event)}
            placeholder="User Id or email"
            ref={inputRef}
            type="text"
            value={emailOrFriendlyId}
          />
        </div>

        {processNotice
          ? (
            <p className={`alert ${processNotice.kind === "error" ? "alert-error" : "alert-info"}`}>
              {processNotice.message}
            </p>
          )
          : null}

        <div className="users-picker__search-label">
          <span aria-hidden="true" className="material-symbols-rounded">person_search</span>
          <span>Szukaj uzytkownika</span>
        </div>

        <div className="contacts-composer__typeahead contacts-composer__typeahead--open">
          <div className="contacts-composer__typeahead-fields">
            <input
              className="field-shell contacts-composer__typeahead-input"
              onChange={(event) => handleSearchFieldChange("firstName", event.target.value)}
              placeholder="First name"
              ref={firstNameInputRef}
              type="text"
              value={searchCriteria.firstName}
            />
            <input
              className="field-shell contacts-composer__typeahead-input"
              onChange={(event) => handleSearchFieldChange("lastName", event.target.value)}
              placeholder="Last name"
              type="text"
              value={searchCriteria.lastName}
            />
            <input
              className="field-shell contacts-composer__typeahead-input"
              onChange={(event) => handleSearchFieldChange("organization", event.target.value)}
              placeholder="Organization"
              type="text"
              value={searchCriteria.organization}
            />
          </div>

          <div className="contacts-composer__typeahead-results">
            {isSearchingUsers
              ? <p className="contacts-composer__typeahead-status">Szukanie uzytkownikow...</p>
              : searchResults.length > 0
              ? (
                <ul className="contacts-composer__results-list">
                  {searchResults.map((result) => (
                    <li key={result.userProfileId}>
                      <button
                        className="contacts-composer__result"
                        disabled={processingUserProfileId !== null}
                        onClick={() => void handleSearchResultClick(result)}
                        type="button"
                      >
                        <span className="contacts-composer__result-copy">
                          <strong>{result.displayName}</strong>
                          <span>@{result.friendlyUserId}</span>
                          {result.organization
                            ? <span>{result.organization}</span>
                            : null}
                        </span>
                        <span aria-hidden="true" className="material-symbols-rounded">
                          {processingUserProfileId === result.userProfileId ? "progress_activity" : "person_add"}
                        </span>
                      </button>
                    </li>
                  ))}
                </ul>
              )
              : searchNotice
              ? <p className="contacts-composer__typeahead-status">{searchNotice}</p>
              : null}
          </div>
        </div>
      </div>

      {confirmNotice
        ? (
          <p className={`alert ${confirmNotice.kind === "error" ? "alert-error" : "alert-info"}`}>
            {confirmNotice.message}
          </p>
        )
        : null}

      {singlePick
        ? null
        : (
          <UsersPickerFooter
            disabled={selectedMembers.length === 0 || isConfirming}
            isPending={isConfirming}
            label={confirmLabel}
            onClick={() => void handleConfirm(selectedMembers)}
          />
        )}
    </div>
  );
}
