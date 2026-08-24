import { ArrowLeft, Search, UsersRound } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { toast } from 'sonner';

import { Button } from '@/components/ui/button';
import { useChatContext } from '@/features/chat/context/use-chat-context';
import { toApiError } from '@/lib/api-error';
import { useAuthStore } from '@/stores/auth-store';

import { useCreateDuet } from '../api/create-duet';
import { useCreateGroup } from '../api/create-group';
import {
  searchUserProfilesInputSchema,
  type SearchUserProfilesInput,
  type UserProfile,
} from '../api/search-user-profile';

import { ConversationSearchField } from './conversation-search-field';
import { ConversationSearchLoading, ConversationSearchStatus } from './conversation-search-status';
import { SelectedUsers } from './selected-users';
import { UserSearchResults } from './user-search-results';

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
  const scrollContainerRef = useRef<HTMLDivElement>(null);

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

      <div className="min-h-0 flex-1 overflow-y-auto" ref={scrollContainerRef}>
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
              <ConversationSearchField
                id="search-first-name"
                label="First name"
                maximumLength={100}
                value={criteria.firstName}
                disabled={isCreating}
                onChange={(value) => updateCriterion('firstName', value)}
              />
              <ConversationSearchField
                id="search-last-name"
                label="Last name"
                maximumLength={100}
                value={criteria.lastName}
                disabled={isCreating}
                onChange={(value) => updateCriterion('lastName', value)}
              />
              <ConversationSearchField
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
            <ConversationSearchStatus>
              Enter a first name, last name, or organization to find users.
            </ConversationSearchStatus>
          ) : !debouncedCriteria ? (
            <ConversationSearchLoading />
          ) : (
            <UserSearchResults
              criteria={debouncedCriteria}
              currentUserId={currentUserId}
              selectedUserIds={selectedUserIds}
              conversationType={conversationType}
              disabled={isCreating}
              processingUserId={processingUserId}
              scrollContainerRef={scrollContainerRef}
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
