import { toast } from 'sonner';

import { SidebarLayout } from '@/components/layouts/sidebar-layout';
import { useCreateDuet } from '@/features/chat/conversations/duet/api/create-duet';
import { useNavigationContext } from '@/features/chat/navigation/context/use-navigation-context';
import { ShowConversationsButton } from '@/features/chat/navigation/show-conversations-button';
import type { UserProfile } from '@/features/chat/user-profiles/api/search-user-profiles';
import { toApiError } from '@/lib/api-error';

import { UserPicker } from './user-picker/user-picker';

export function DuetConversationCreator() {
  const { showConversations } = useNavigationContext();
  const duetMutation = useCreateDuet({
    mutationConfig: {
      onSuccess: () => {
        toast.success('Duet conversation created.');
        showConversations();
      },
      onError: (error) => toast.error(toApiError(error).message),
    },
  });

  const handleUserSelect = (userProfile: UserProfile) => {
    duetMutation.mutate({ data: { partnerUserId: userProfile.id } });
  };

  return (
    <SidebarLayout
      backAction={<ShowConversationsButton disabled={duetMutation.isPending} />}
      eyebrow="New conversation"
      headingId="conversation-creator-heading"
      title="Create a duet"
    >
      <UserPicker
        mode="single"
        selectedUsers={[]}
        disabled={duetMutation.isPending}
        processingUserId={duetMutation.variables?.data.partnerUserId}
        onSelect={handleUserSelect}
        onRemove={() => undefined}
      />
    </SidebarLayout>
  );
}
