import { Search } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';

import { useAuthStore } from '@/stores/auth-store';

import {
  searchUserProfilesInputSchema,
  type SearchUserProfilesInput,
  type UserProfile,
} from '../../api/search-user-profile';

import { SelectedUsers } from './selected-users';
import { UserPickerField } from './user-picker-field';
import { UserPickerLoading, UserPickerStatus } from './user-picker-status';
import { UserSearchResults } from './user-search-results';

type SearchCriteria = {
  firstName: string;
  lastName: string;
  organization: string;
};

const initialSearchCriteria: SearchCriteria = {
  firstName: '',
  lastName: '',
  organization: '',
};

interface UserPickerProps {
  mode: 'single' | 'multiple';
  selectedUsers: UserProfile[];
  disabled: boolean;
  processingUserId?: string;
  onSelect: (userProfile: UserProfile) => void;
  onRemove: (userProfileId: string) => void;
}

export function UserPicker({
  mode,
  selectedUsers,
  disabled,
  processingUserId,
  onSelect,
  onRemove,
}: UserPickerProps) {
  const currentUserId = useAuthStore((state) => state.session?.user.id);
  const scrollContainerRef = useRef<HTMLDivElement>(null);
  const [criteria, setCriteria] = useState<SearchCriteria>(initialSearchCriteria);
  const [debouncedCriteria, setDebouncedCriteria] = useState<SearchUserProfilesInput | null>(null);

  useEffect(() => {
    const parsedCriteria = searchUserProfilesInputSchema.safeParse(criteria);
    if (!parsedCriteria.success) return;

    const timeoutId = window.setTimeout(() => {
      setDebouncedCriteria(parsedCriteria.data);
    }, 280);

    return () => window.clearTimeout(timeoutId);
  }, [criteria]);

  const updateCriterion = (field: keyof SearchCriteria, value: string) => {
    setDebouncedCriteria(null);
    setCriteria((current) => ({ ...current, [field]: value }));
  };
  const hasSearchCriteria = Object.values(criteria).some((value) => value.trim().length > 0);
  const selectedUserIds = new Set(selectedUsers.map((userProfile) => userProfile.id));

  return (
    <div className="min-h-0 flex-1 overflow-y-auto" ref={scrollContainerRef}>
      <div className="grid gap-5 px-5 py-5 sm:px-6">
        {mode === 'multiple' ? (
          <SelectedUsers users={selectedUsers} disabled={disabled} onRemove={onRemove} />
        ) : null}

        <section className="grid gap-3" aria-labelledby="search-users-heading">
          <div className="flex items-center gap-2">
            <Search className="size-4 text-blue-700" aria-hidden="true" />
            <h2 className="m-0 text-sm font-semibold text-slate-900" id="search-users-heading">
              Search users
            </h2>
          </div>
          <div className="grid gap-3">
            <UserPickerField
              id="search-first-name"
              label="First name"
              maximumLength={100}
              value={criteria.firstName}
              disabled={disabled}
              onChange={(value) => updateCriterion('firstName', value)}
            />
            <UserPickerField
              id="search-last-name"
              label="Last name"
              maximumLength={100}
              value={criteria.lastName}
              disabled={disabled}
              onChange={(value) => updateCriterion('lastName', value)}
            />
            <UserPickerField
              id="search-organization"
              label="Organization"
              maximumLength={200}
              value={criteria.organization}
              disabled={disabled}
              onChange={(value) => updateCriterion('organization', value)}
            />
          </div>
        </section>
      </div>

      <div className="border-t border-slate-200 px-5 sm:px-6">
        {!hasSearchCriteria ? (
          <UserPickerStatus>
            Enter a first name, last name, or organization to find users.
          </UserPickerStatus>
        ) : !debouncedCriteria ? (
          <UserPickerLoading />
        ) : (
          <UserSearchResults
            criteria={debouncedCriteria}
            currentUserId={currentUserId}
            selectedUserIds={selectedUserIds}
            mode={mode}
            disabled={disabled}
            processingUserId={processingUserId}
            scrollContainerRef={scrollContainerRef}
            onSelect={onSelect}
          />
        )}
      </div>
    </div>
  );
}
