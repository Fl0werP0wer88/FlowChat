import { UsersRound } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

import { Button } from '@/components/ui/button';
import { useNavigationContext } from '@/features/chat/navigation/context/use-navigation-context';
import { ShowConversationsButton } from '@/features/chat/navigation/show-conversations-button';
import { toApiError } from '@/lib/api-error';

import { useCreateDuet } from '../api/create-duet';
import { useCreateGroup } from '../api/create-group';
import type { UserProfile } from '../api/search-user-profile';

import { UserPicker } from './user-picker/user-picker';

type ConversationType = 'duet' | 'group';

function ActiveConversationCreator({
  conversationType,
  showConversations,
}: {
  conversationType: ConversationType;
  showConversations: () => void;
}) {
  const isGroup = conversationType === 'group';
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
        <ShowConversationsButton disabled={isCreating} />
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

      {isGroup ? (
        <div className="border-b border-slate-200 px-5 pt-5 pb-3 sm:px-6">
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
        </div>
      ) : null}

      <UserPicker
        mode={isGroup ? 'multiple' : 'single'}
        selectedUsers={selectedUsers}
        disabled={isCreating}
        processingUserId={processingUserId}
        onSelect={handleUserSelect}
        onRemove={(userProfileId) =>
          setSelectedUsers((current) =>
            current.filter((userProfile) => userProfile.id !== userProfileId),
          )
        }
      />

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
  const { activeView, showConversations } = useNavigationContext();

  if (activeView.sidebar.view !== 'conversationCreator') return null;

  return (
    <ActiveConversationCreator
      conversationType={activeView.sidebar.conversationType}
      showConversations={showConversations}
    />
  );
}
