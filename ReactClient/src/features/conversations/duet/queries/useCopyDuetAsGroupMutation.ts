import { useMutation, useQueryClient } from "@tanstack/react-query";
import { copyDuetAsGroup } from "../api";

interface UseCopyDuetAsGroupMutationCallbacks {
  onSuccess: () => void;
  onError: (message: string) => void;
}

export function useCopyDuetAsGroupMutation(
  accessToken: string,
  callbacks: UseCopyDuetAsGroupMutationCallbacks,
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (partnerUserId: string) => copyDuetAsGroup(partnerUserId, accessToken),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["groupConversations"] });
      callbacks.onSuccess();
    },
    onError: (error) => {
      const message = error instanceof Error ? error.message : "Nie udalo sie utworzyc grupy.";
      callbacks.onError(message);
    },
  });
}
