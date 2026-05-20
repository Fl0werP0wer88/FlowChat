import { useQuery } from "@tanstack/react-query";
import { fetchGroupConversations } from "../api";

export function useGroupConversationsQuery(accessToken: string, enabled: boolean) {
  return useQuery({
    queryKey: ["groupConversations"],
    queryFn: ({ signal }) => fetchGroupConversations(accessToken, signal),
    enabled,
    staleTime: 5 * 60 * 1000,
  });
}
