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
} from "../../../users/api";
import { createGroupConversation } from "../../api";

type GroupBuilderNotice = { kind: "error" | "info"; message: string };

interface GroupBuilderProps {
  isOpen: boolean;
  onClose: () => void;
}

export function GroupBuilder({
  isOpen,
  onClose,
}: GroupBuilderProps) {
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
  const [isLookupProcessing, setIsLookupProcessing] = useState(false);
  const [processingUserProfileId, setProcessingUserProfileId] = useState<string | null>(null);
  const [searchNotice, setSearchNotice] = useState<string | null>(null);
  const [processNotice, setProcessNotice] = useState<GroupBuilderNotice | null>(null);
  const [selectedMembers, setSelectedMembers] = useState<SearchUserResult[]>([]);
  const [groupName, setGroupName] = useState("");
  const [isCreatingGroup, setIsCreatingGroup] = useState(false);
  const [createNotice, setCreateNotice] = useState<GroupBuilderNotice | null>(null);
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

  const addMember = (user: SearchUserResult): GroupBuilderNotice => {
    if (selectedMembers.some((member) => member.userProfileId === user.userProfileId)) {
      return { kind: "info", message: "Uzytkownik jest juz na liscie." };
    }

    setSelectedMembers((current) => [...current, user]);
    return { kind: "info", message: "Dodano do grupy." };
  };

  const removeMember = (userProfileId: string) => {
    setSelectedMembers((current) => current.filter((member) => member.userProfileId !== userProfileId));
  };

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
    setProcessNotice(null);
    setIsLookupProcessing(false);
    setSelectedMembers([]);
    setGroupName("");
    setCreateNotice(null);
    setIsCreatingGroup(false);
  };

  const closeSearch = () => {
    resetSearch();
    onClose();
  };

  const handleCreateGroup = async () => {
    const trimmedName = groupName.trim();
    if (selectedMembers.length === 0 || !trimmedName) {
      return;
    }

    if (!accessToken) {
      setCreateNotice({ kind: "error", message: "Brakuje aktywnej sesji potrzebnej do utworzenia grupy." });
      return;
    }

    setIsCreatingGroup(true);
    setCreateNotice(null);
    try {
      await createGroupConversation(
        selectedMembers.map((member) => member.userProfileId),
        trimmedName,
        accessToken,
      );
      closeSearch();
    } catch (error) {
      const message = error instanceof Error ? error.message : "Nie udalo sie utworzyc grupy.";
      setCreateNotice({ kind: "error", message });
    } finally {
      setIsCreatingGroup(false);
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

      setProcessNotice(addMember(user));
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
      const userProfile = await getUserProfileById(result.userProfileId, accessToken);
      setProcessNotice(addMember(userProfile));
    } catch (error) {
      const message = error instanceof Error ? error.message : "Nie udalo sie pobrac profilu uzytkownika.";
      setSearchNotice(message);
    } finally {
      setProcessingUserProfileId(null);
    }
  };

  return (
    <div className={`contacts-composer ${isOpen ? "contacts-composer--open" : ""}`} aria-hidden={!isOpen}>
      <div className="contacts-composer__header">
        <button
          aria-label="Wroc do grup"
          className="contacts-composer__back-button"
          onClick={closeSearch}
          type="button"
        >
          <span aria-hidden="true" className="material-symbols-rounded">arrow_back</span>
        </button>
        <strong>Nowa grupa</strong>
      </div>

      {selectedMembers.length > 0
        ? (
          <ul className="group-builder__selected-list">
            {selectedMembers.map((member) => (
              <li className="group-builder__selected-item" key={member.userProfileId}>
                <span>{member.displayName}</span>
                <button
                  aria-label={`Usun ${member.displayName} z grupy`}
                  className="group-builder__remove-button"
                  onClick={() => removeMember(member.userProfileId)}
                  type="button"
                >
                  <span aria-hidden="true" className="material-symbols-rounded">person_remove</span>
                </button>
              </li>
            ))}
          </ul>
        )
        : null}

      <div className="contacts-composer__search">
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

      <input
        className="contacts-composer__input group-builder__name-input"
        disabled={isCreatingGroup}
        onChange={(event) => setGroupName(event.target.value)}
        placeholder="Nazwa grupy"
        type="text"
        value={groupName}
      />

      {createNotice
        ? (
          <p className={`alert ${createNotice.kind === "error" ? "alert-error" : "alert-info"}`}>
            {createNotice.message}
          </p>
        )
        : null}

      <button
        className="group-builder__create-button"
        disabled={selectedMembers.length === 0 || !groupName.trim() || isCreatingGroup}
        onClick={() => void handleCreateGroup()}
        type="button"
      >
        <span aria-hidden="true" className="material-symbols-rounded">
          {isCreatingGroup ? "progress_activity" : "group_add"}
        </span>
        <span>Stworz grupe</span>
      </button>
    </div>
  );
}
