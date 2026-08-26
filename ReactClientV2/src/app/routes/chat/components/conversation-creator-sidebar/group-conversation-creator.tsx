import { UsersRound } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

import { SidebarLayout } from '@/components/layouts/sidebar-layout';
import { Button } from '@/components/ui/button';
import { InputField } from '@/components/ui/input-field';
import { useCreateGroup } from '@/features/chat/conversations/group/api/create-group';
import { useNavigationContext } from '@/features/chat/navigation/context/use-navigation-context';
import { ShowConversationsButton } from '@/features/chat/navigation/show-conversations-button';
import type { UserProfile } from '@/features/chat/user-profiles/api/search-user-profiles';
import { toApiError } from '@/lib/api-error';

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
    <SidebarLayout
      backAction={<ShowConversationsButton disabled={groupMutation.isPending} />}
      eyebrow="New conversation"
      headingId="conversation-creator-heading"
      title="Create a group"
    >
      <div className="border-b border-slate-200 px-5 pt-5 pb-3 sm:px-6">
        <InputField
          id="group-name"
          label="Group name"
          variant="compact"
          disabled={groupMutation.isPending}
          onChange={(event) => setGroupName(event.target.value)}
          placeholder="e.g. Product team"
          type="text"
          value={groupName}
        />
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
    </SidebarLayout>
  );
}
