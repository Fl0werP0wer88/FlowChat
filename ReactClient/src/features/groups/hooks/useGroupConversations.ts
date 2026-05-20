import { useAuthStore } from "../../../store/authStore";
import { resolveOwnerUserId } from "../../../utils/authUtils";
import type { GroupConversation } from "../api";
import { useGroupConversationsQuery } from "../queries/useGroupConversationsQuery";

export interface UseGroupConversationsResult {
  groupConversations: GroupConversation[];
  isLoadingGroupConversations: boolean;
}

export function useGroupConversations(): UseGroupConversationsResult {
  const accessToken = useAuthStore((s) => s.accessToken) ?? "";
  const ownerUserId = resolveOwnerUserId(accessToken);

  const { data: groupConversations = [], isLoading: isLoadingGroupConversations } =
    useGroupConversationsQuery(accessToken, Boolean(accessToken && ownerUserId));

  return { groupConversations, isLoadingGroupConversations };
}
