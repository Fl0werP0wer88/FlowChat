import { UsersRound } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

import { Button } from '@/components/ui/button';
import { useCreateGroup } from '@/features/chat/conversations/group/api/create-group';
import { useNavigationContext } from '@/features/chat/navigation/context/use-navigation-context';
import type { UserProfile } from '@/features/chat/user-profiles/api/search-user-profiles';
import { toApiError } from '@/lib/api-error';

import { ConversationCreatorLayout } from './conversation-creator-layout';
import { UserPicker } from './user-picker/user-picker';

export function GroupConversationCreator() {
  const { showConversations } = useNavigationContext();
  const [selectedUsers, setSelectedUsers] = useState<UserProfile[]>([]);
  const [groupName, setGroupName] = useState('');
  const groupMutation = useCreateGroup({
    mutationConfig: {
      onSuccess: () => {
        toast.success('Group conversation created.');
        showConversations();
      },
      onError: (error) => toast.error(toApiError(error).message),
    },
  });
  const canCreateGroup =
    groupName.trim().length > 0 && selectedUsers.length > 0 && !groupMutation.isPending;

  const handleUserSelect = (userProfile: UserProfile) => {
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
    <ConversationCreatorLayout title="Create a group" disabled={groupMutation.isPending}>
      <div className="border-b border-slate-200 px-5 pt-5 pb-3 sm:px-6">
        <div className="grid gap-1.5">
          <label className="text-sm font-semibold text-slate-800" htmlFor="group-name">
            Group name
          </label>
          <input
            className="min-h-11 w-full rounded-xl border border-slate-200 bg-white px-3.5 text-sm text-slate-950 outline-none transition placeholder:text-slate-400 focus:border-blue-600 focus:ring-2 focus:ring-blue-100 disabled:bg-slate-100"
            id="group-name"
            disabled={groupMutation.isPending}
            onChange={(event) => setGroupName(event.target.value)}
            placeholder="e.g. Product team"
            type="text"
            value={groupName}
          />
        </div>
      </div>

      <UserPicker
        mode="multiple"
        selectedUsers={selectedUsers}
        disabled={groupMutation.isPending}
        onSelect={handleUserSelect}
        onRemove={(userProfileId) =>
          setSelectedUsers((current) =>
            current.filter((userProfile) => userProfile.id !== userProfileId),
          )
        }
      />

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
    </ConversationCreatorLayout>
  );
}
