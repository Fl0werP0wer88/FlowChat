import { useEffect, useRef, useState } from "react";
import type { KeyboardEventHandler } from "react";
import { useAuthStore } from "../../../store/authStore";
import { getUserProfile, searchUsers, type SearchUserResult, type SearchUsersCriteria } from "../api";

interface UserSearchProps {
  isDisabled?: boolean;
  isOpen: boolean;
  notification?: { kind: "error" | "info"; message: string; } | null;
  onClearNotice: () => void;
  onClose: () => void;
  onProcessUser: (user: SearchUserResult) => Promise<boolean>;
}

export function UserSearch({
  isDisabled = false,
  isOpen,
  notification = null,
  onClearNotice,
  onClose,
  onProcessUser,
}: UserSearchProps) {
  const accessToken = useAuthStore((s) => s.accessToken) ?? "";
  const [isSearchExpanded, setIsSearchExpanded] = useState(false);
  const [emailOrFriendlyId, setEmailOrFriendlyId] = useState("");
  const [searchCriteria, setSearchCriteria] = useState<SearchUsersCriteria>({
    firstName: "",
    lastName: "",
    organization: "",
  });
  const [searchResults, setSearchResults] = useState<SearchUserResult[]>([]);
  const [isSearchingUsers, setIsSearchingUsers] = useState(false);
  const [processingUserProfileId, setProcessingUserProfileId] = useState<string | null>(null);
  const [searchNotice, setSearchNotice] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const firstNameInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (isOpen) {
      window.requestAnimationFrame(() => inputRef.current?.focus());
    }
  }, [isOpen]);

  useEffect(() => {
    if (!isOpen || !isSearchExpanded) {
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
  }, [accessToken, isOpen, isSearchExpanded, searchCriteria]);

  const resetSearch = () => {
    setIsSearchExpanded(false);
    setEmailOrFriendlyId("");
    setSearchCriteria({
      firstName: "",
      lastName: "",
      organization: "",
    });
    setSearchResults([]);
    setSearchNotice(null);
  };

  const closeSearch = () => {
    resetSearch();
    onClearNotice();
    onClose();
  };

  const isGuidLookup = (value: string): boolean =>
    /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value.trim());

  const submitLookup = async () => {
    const trimmedLookup = emailOrFriendlyId.trim();
    let user: SearchUserResult = {
      userProfileId: "",
      friendlyUserId: trimmedLookup,
      displayName: "",
      firstName: null,
      lastName: null,
      organization: null,
    };

    try {
      if (isGuidLookup(trimmedLookup)) {
        if (!accessToken) {
          setSearchNotice("Brakuje aktywnej sesji potrzebnej do pobrania profilu uzytkownika.");
          return;
        }

        user = await getUserProfile(trimmedLookup, accessToken);
      }

      const wasAdded = await onProcessUser(user);

      if (wasAdded) {
        resetSearch();
        onClose();
      }
    } catch (error) {
      const message = error instanceof Error ? error.message : "Nie udalo sie pobrac profilu uzytkownika.";
      setSearchNotice(message);
    }
  };

  const handleLookupKeyDown: KeyboardEventHandler<HTMLInputElement> = async (event) => {
    if (event.key !== "Enter") {
      return;
    }

    event.preventDefault();
    await submitLookup();
  };

  const toggleSearch = () => {
    setIsSearchExpanded((current) => {
      const next = !current;

      if (!next) {
        setSearchCriteria({
          firstName: "",
          lastName: "",
          organization: "",
        });
        setSearchResults([]);
        setSearchNotice(null);
      } else {
        window.requestAnimationFrame(() => firstNameInputRef.current?.focus());
      }

      return next;
    });
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
      const userProfile = await getUserProfile(result.userProfileId, accessToken);
      const wasAdded = await onProcessUser(userProfile);
      if (wasAdded) {
        resetSearch();
        onClose();
      }
    } catch (error) {
      const message = error instanceof Error ? error.message : "Nie udalo sie pobrac profilu uzytkownika.";
      setSearchNotice(message);
    } finally {
      setProcessingUserProfileId(null);
    }
  };

  return (
    <div className="contacts-composer" aria-hidden={!isOpen}>
      <div className="contacts-composer__header">
        <button
          aria-label="Wroc do kontaktow"
          className="contacts-composer__back-button"
          onClick={closeSearch}
          type="button"
        >
          <span aria-hidden="true" className="material-symbols-rounded">arrow_back</span>
        </button>
        <strong>Nowy kontakt</strong>
      </div>

      <div className="contacts-composer__search">
        <button
          aria-label="Dodaj kontakt z podanej wartosci"
          className="contacts-composer__search-button"
          disabled={isDisabled}
          onClick={() => void submitLookup()}
          type="button"
        >
          <span aria-hidden="true" className="material-symbols-rounded">person_add</span>
        </button>
        <input
          className="contacts-composer__input"
          disabled={isDisabled}
          onChange={(event) => setEmailOrFriendlyId(event.target.value)}
          onKeyDown={(event) => void handleLookupKeyDown(event)}
          placeholder="User Id or email"
          ref={inputRef}
          type="text"
          value={emailOrFriendlyId}
        />
      </div>

      {notification
        ? (
          <p className={`alert ${notification.kind === "error" ? "alert-error" : "alert-info"}`}>
            {notification.message}
          </p>
        )
        : null}

      <button
        className={`contacts-composer__action ${isSearchExpanded ? "contacts-composer__action--active" : ""}`}
        onClick={toggleSearch}
        type="button"
      >
        <span aria-hidden="true" className="material-symbols-rounded">person_search</span>
        <span>Search User</span>
      </button>

      <div className={`contacts-composer__typeahead ${isSearchExpanded ? "contacts-composer__typeahead--open" : ""}`}>
        <div className="contacts-composer__typeahead-fields">
          <input
            className="contacts-composer__typeahead-input"
            onChange={(event) => handleSearchFieldChange("firstName", event.target.value)}
            placeholder="First name"
            ref={firstNameInputRef}
            type="text"
            value={searchCriteria.firstName}
          />
          <input
            className="contacts-composer__typeahead-input"
            onChange={(event) => handleSearchFieldChange("lastName", event.target.value)}
            placeholder="Last name"
            type="text"
            value={searchCriteria.lastName}
          />
          <input
            className="contacts-composer__typeahead-input"
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
                      disabled={isDisabled || processingUserProfileId !== null}
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
  );
}
