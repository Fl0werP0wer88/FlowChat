import { ArrowLeft, Check, LoaderCircle, Search, UserPlus, UsersRound, X } from 'lucide-react';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

import { Button } from '@/components/ui/button';
import { useChatContext } from '@/features/chat/context/use-chat-context';
import { toApiError } from '@/lib/api-error';
import { useAuthStore } from '@/stores/auth-store';
import { cn } from '@/utils/cn';

import { useCreateDuet } from '../api/create-duet';
import { useCreateGroup } from '../api/create-group';
import {
  searchUserProfilesInputSchema,
  type SearchUserProfilesInput,
  type UserProfile,
  useSearchUserProfiles,
} from '../api/search-user-profile';

type ConversationType = 'duet' | 'group';

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

function getDisplayName(userProfile: UserProfile) {
  const name = [userProfile.firstName, userProfile.lastName]
    .map((value) => value?.trim())
    .filter(Boolean)
    .join(' ');

  return name || `@${userProfile.friendlyUserId}`;
}

function getInitials(userProfile: UserProfile) {
  const name = [userProfile.firstName, userProfile.lastName]
    .map((value) => value?.trim())
    .filter(Boolean);

  if (name.length) {
    return name
      .slice(0, 2)
      .map((value) => value?.[0])
      .join('')
      .toUpperCase();
  }

  return userProfile.friendlyUserId.slice(0, 2).toUpperCase() || '?';
}

function UserAvatar({
  userProfile,
  size = 'regular',
}: {
  userProfile: UserProfile;
  size?: 'small' | 'regular';
}) {
  const sizeClasses = size === 'small' ? 'size-8 text-xs' : 'size-11 text-sm';

  return userProfile.avatarUrl ? (
    <img
      className={cn('shrink-0 rounded-full bg-slate-200 object-cover', sizeClasses)}
      src={userProfile.avatarUrl}
      alt=""
      loading="lazy"
      decoding="async"
    />
  ) : (
    <span
      className={cn(
        'grid shrink-0 place-items-center rounded-full bg-blue-100 font-bold text-blue-800',
        sizeClasses,
      )}
      aria-hidden="true"
    >
      {getInitials(userProfile)}
    </span>
  );
}

function SearchField({
  id,
  label,
  value,
  maximumLength,
  disabled,
  onChange,
}: {
  id: string;
  label: string;
  value: string;
  maximumLength: number;
  disabled: boolean;
  onChange: (value: string) => void;
}) {
  return (
    <div className="grid gap-1.5">
      <label className="text-xs font-bold text-slate-700" htmlFor={id}>
        {label}
      </label>
      <input
        className="min-h-10 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-blue-600 focus:ring-2 focus:ring-blue-100 disabled:bg-slate-100 disabled:text-slate-500"
        id={id}
        maxLength={maximumLength}
        disabled={disabled}
        onChange={(event) => onChange(event.target.value)}
        type="search"
        value={value}
      />
    </div>
  );
}

function SearchStatus({ children }: { children: string }) {
  return (
    <div className="grid min-h-32 place-items-center px-4 py-8 text-center" role="status">
      <p className="m-0 max-w-56 text-sm leading-6 text-slate-500">{children}</p>
    </div>
  );
}

interface UserSearchResultsProps {
  criteria: SearchUserProfilesInput;
  currentUserId?: string;
  selectedUserIds: ReadonlySet<string>;
  conversationType: ConversationType;
  disabled: boolean;
  processingUserId?: string;
  onSelect: (userProfile: UserProfile) => void;
}

function UserSearchResults({
  criteria,
  currentUserId,
  selectedUserIds,
  conversationType,
  disabled,
  processingUserId,
  onSelect,
}: UserSearchResultsProps) {
  const search = useSearchUserProfiles({ input: criteria });

  if (search.isPending) {
    return (
      <div
        className="flex min-h-32 items-center justify-center gap-2 px-4 py-8 text-sm text-slate-500"
        role="status"
      >
        <LoaderCircle className="size-4 animate-spin" aria-hidden="true" />
        Searching users...
      </div>
    );
  }

  if (search.isError) {
    return (
      <div className="grid min-h-32 place-items-center px-4 py-8 text-center" role="alert">
        <div className="grid max-w-60 justify-items-center gap-3">
          <div className="grid gap-1.5">
            <p className="m-0 text-sm font-semibold text-slate-900">Search unavailable</p>
            <p className="m-0 break-words text-xs leading-5 text-rose-700">
              {toApiError(search.error).message}
            </p>
          </div>
          <Button
            className="min-h-9 px-4"
            type="button"
            variant="secondary"
            isLoading={search.isFetching}
            onClick={() => void search.refetch()}
          >
            Try again
          </Button>
        </div>
      </div>
    );
  }

  const userProfiles = search.data.userProfiles.filter(
    (userProfile) => userProfile.id !== currentUserId,
  );

  if (!userProfiles.length) {
    return <SearchStatus>No users match these search criteria.</SearchStatus>;
  }

  return (
    <ul className="m-0 list-none divide-y divide-slate-200 p-0" aria-label="User search results">
      {userProfiles.map((userProfile) => {
        const displayName = getDisplayName(userProfile);
        const isSelected = selectedUserIds.has(userProfile.id);
        const isProcessing = processingUserId === userProfile.id;

        return (
          <li key={userProfile.id}>
            <button
              className={cn(
                'flex min-h-16 w-full items-center gap-3 px-1 py-2.5 text-left transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-blue-600 disabled:cursor-not-allowed disabled:opacity-60',
                isSelected ? 'bg-blue-50/80' : 'hover:bg-white',
              )}
              type="button"
              disabled={disabled}
              aria-pressed={conversationType === 'group' ? isSelected : undefined}
              onClick={() => onSelect(userProfile)}
            >
              <UserAvatar userProfile={userProfile} />
              <span className="grid min-w-0 flex-1 gap-0.5">
                <strong className="truncate text-sm font-semibold text-slate-900">
                  {displayName}
                </strong>
                {displayName !== `@${userProfile.friendlyUserId}` ? (
                  <span className="truncate text-xs text-slate-500">
                    @{userProfile.friendlyUserId}
                  </span>
                ) : null}
                {userProfile.organization ? (
                  <span className="truncate text-xs text-slate-500">
                    {userProfile.organization}
                  </span>
                ) : null}
              </span>
              <span
                className={cn(
                  'grid size-8 shrink-0 place-items-center rounded-full',
                  isSelected ? 'bg-blue-700 text-white' : 'bg-blue-50 text-blue-700',
                )}
                aria-hidden="true"
              >
                {isProcessing ? (
                  <LoaderCircle className="size-4 animate-spin" />
                ) : isSelected ? (
                  <Check className="size-4" />
                ) : (
                  <UserPlus className="size-4" />
                )}
              </span>
            </button>
          </li>
        );
      })}
    </ul>
  );
}

function SelectedUsers({
  users,
  disabled,
  onRemove,
}: {
  users: UserProfile[];
  disabled: boolean;
  onRemove: (userProfileId: string) => void;
}) {
  if (!users.length) return null;

  return (
    <section className="grid gap-2.5" aria-labelledby="selected-members-heading">
      <div className="flex items-center justify-between gap-3">
        <h2
          className="m-0 text-xs font-extrabold uppercase tracking-[0.12em] text-slate-600"
          id="selected-members-heading"
        >
          Selected members
        </h2>
        <span className="text-xs font-semibold text-slate-500">{users.length}</span>
      </div>
      <ul className="m-0 flex list-none flex-wrap gap-2 p-0">
        {users.map((userProfile) => {
          const displayName = getDisplayName(userProfile);

          return (
            <li
              className="flex min-w-0 items-center gap-2 rounded-full border border-slate-200 bg-white py-1 pr-1 pl-1.5"
              key={userProfile.id}
            >
              <UserAvatar userProfile={userProfile} size="small" />
              <span className="max-w-28 truncate text-xs font-semibold text-slate-800">
                {displayName}
              </span>
              <button
                className="grid size-7 shrink-0 place-items-center rounded-full text-slate-500 transition hover:bg-rose-50 hover:text-rose-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-blue-600 disabled:opacity-50"
                type="button"
                disabled={disabled}
                aria-label={`Remove ${displayName}`}
                onClick={() => onRemove(userProfile.id)}
              >
                <X className="size-3.5" aria-hidden="true" />
              </button>
            </li>
          );
        })}
      </ul>
    </section>
  );
}

function ActiveConversationCreator({
  conversationType,
  showConversations,
}: {
  conversationType: ConversationType;
  showConversations: () => void;
}) {
  const isGroup = conversationType === 'group';
  const currentUserId = useAuthStore((state) => state.session?.user.id);
  const [criteria, setCriteria] = useState<SearchCriteria>(initialSearchCriteria);
  const [debouncedCriteria, setDebouncedCriteria] = useState<SearchUserProfilesInput | null>(null);
  const [selectedUsers, setSelectedUsers] = useState<UserProfile[]>([]);
  const [groupName, setGroupName] = useState('');

  const duetMutation = useCreateDuet({
    mutationConfig: {
      onSuccess: () => {
        toast.success('Duet conversation created.');
        showConversations();
      },
      onError: (error) => toast.error(toApiError(error).message),
    },
  });
  const groupMutation = useCreateGroup({
    mutationConfig: {
      onSuccess: () => {
        toast.success('Group conversation created.');
        showConversations();
      },
      onError: (error) => toast.error(toApiError(error).message),
    },
  });
  const isCreating = duetMutation.isPending || groupMutation.isPending;

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
  const processingUserId = duetMutation.variables?.data.partnerUserId;
  const canCreateGroup = groupName.trim().length > 0 && selectedUsers.length > 0 && !isCreating;

  const handleUserSelect = (userProfile: UserProfile) => {
    if (!isGroup) {
      duetMutation.mutate({ data: { partnerUserId: userProfile.id } });
      return;
    }

    setSelectedUsers((current) =>
      current.some((selectedUser) => selectedUser.id === userProfile.id)
        ? current.filter((selectedUser) => selectedUser.id !== userProfile.id)
        : [...current, userProfile],
    );
  };

  const handleCreateGroup = () => {
    if (!canCreateGroup) return;

    groupMutation.mutate({
      data: {
        name: groupName,
        participantUserIds: [...new Set(selectedUsers.map((userProfile) => userProfile.id))],
      },
    });
  };

  return (
    <aside
      className="flex min-h-64 flex-col border-b border-slate-200 bg-slate-50/70 lg:min-h-0 lg:border-r lg:border-b-0"
      aria-labelledby="conversation-creator-heading"
    >
      <div className="border-b border-slate-200 px-5 py-5 sm:px-6">
        <Button
          className="-ml-3 mb-4 px-3"
          type="button"
          variant="ghost"
          disabled={isCreating}
          onClick={showConversations}
        >
          <ArrowLeft className="size-4" aria-hidden="true" />
          Back
        </Button>
        <p className="m-0 text-[0.68rem] font-extrabold uppercase tracking-[0.18em] text-blue-700">
          New conversation
        </p>
        <h1
          className="mt-1 mb-0 font-display text-2xl font-semibold tracking-[-0.035em] text-slate-950"
          id="conversation-creator-heading"
        >
          {isGroup ? 'Create a group' : 'Create a duet'}
        </h1>
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto">
        <div className="grid gap-5 px-5 py-5 sm:px-6">
          {isGroup ? (
            <div className="grid gap-5">
              <div className="grid gap-1.5">
                <label className="text-sm font-semibold text-slate-800" htmlFor="group-name">
                  Group name
                </label>
                <input
                  className="min-h-11 w-full rounded-xl border border-slate-200 bg-white px-3.5 text-sm text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-blue-600 focus:ring-2 focus:ring-blue-100 disabled:bg-slate-100"
                  id="group-name"
                  disabled={isCreating}
                  onChange={(event) => setGroupName(event.target.value)}
                  placeholder="e.g. Product team"
                  type="text"
                  value={groupName}
                />
              </div>
              <SelectedUsers
                users={selectedUsers}
                disabled={isCreating}
                onRemove={(userProfileId) =>
                  setSelectedUsers((current) =>
                    current.filter((userProfile) => userProfile.id !== userProfileId),
                  )
                }
              />
            </div>
          ) : null}

          <section className="grid gap-3" aria-labelledby="search-users-heading">
            <div className="flex items-center gap-2">
              <Search className="size-4 text-blue-700" aria-hidden="true" />
              <h2 className="m-0 text-sm font-semibold text-slate-900" id="search-users-heading">
                Search users
              </h2>
            </div>
            <div className="grid gap-3">
              <SearchField
                id="search-first-name"
                label="First name"
                maximumLength={100}
                value={criteria.firstName}
                disabled={isCreating}
                onChange={(value) => updateCriterion('firstName', value)}
              />
              <SearchField
                id="search-last-name"
                label="Last name"
                maximumLength={100}
                value={criteria.lastName}
                disabled={isCreating}
                onChange={(value) => updateCriterion('lastName', value)}
              />
              <SearchField
                id="search-organization"
                label="Organization"
                maximumLength={200}
                value={criteria.organization}
                disabled={isCreating}
                onChange={(value) => updateCriterion('organization', value)}
              />
            </div>
          </section>
        </div>

        <div className="border-t border-slate-200 px-5 sm:px-6">
          {!hasSearchCriteria ? (
            <SearchStatus>
              Enter a first name, last name, or organization to find users.
            </SearchStatus>
          ) : !debouncedCriteria ? (
            <div
              className="flex min-h-32 items-center justify-center gap-2 px-4 py-8 text-sm text-slate-500"
              role="status"
            >
              <LoaderCircle className="size-4 animate-spin" aria-hidden="true" />
              Searching users...
            </div>
          ) : (
            <UserSearchResults
              criteria={debouncedCriteria}
              currentUserId={currentUserId}
              selectedUserIds={selectedUserIds}
              conversationType={conversationType}
              disabled={isCreating}
              processingUserId={processingUserId}
              onSelect={handleUserSelect}
            />
          )}
        </div>
      </div>

      {isGroup ? (
        <div className="border-t border-slate-200 bg-white/80 px-5 py-4 sm:px-6">
          <Button
            className="w-full"
            type="button"
            disabled={!canCreateGroup}
            isLoading={groupMutation.isPending}
            onClick={handleCreateGroup}
          >
            <UsersRound className="size-4" aria-hidden="true" />
            Create group
          </Button>
        </div>
      ) : null}
    </aside>
  );
}

export function ConversationCreator() {
  const { activeView, showConversations } = useChatContext();

  if (activeView.view !== 'conversationCreator') return null;

  return (
    <ActiveConversationCreator
      conversationType={activeView.conversationType}
      showConversations={showConversations}
    />
  );
}
