import { toast } from 'sonner';

import { useCreateDuet } from '@/features/chat/conversations/duet/api/create-duet';
import { useNavigationContext } from '@/features/chat/navigation/context/use-navigation-context';
import type { UserProfile } from '@/features/chat/user-profiles/api/search-user-profiles';
import { toApiError } from '@/lib/api-error';

import { ConversationCreatorLayout } from './conversation-creator-layout';
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
    <ConversationCreatorLayout title="Create a duet" disabled={duetMutation.isPending}>
      <UserPicker
        mode="single"
        selectedUsers={[]}
        disabled={duetMutation.isPending}
        processingUserId={duetMutation.variables?.data.partnerUserId}
        onSelect={handleUserSelect}
        onRemove={() => undefined}
      />
    </ConversationCreatorLayout>
  );
}
