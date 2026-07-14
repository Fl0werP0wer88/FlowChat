import { useMutation, useQueryClient } from "@tanstack/react-query";
import { openDuetConversation } from "../../api/gatewayService";

interface UseAddContactMutationCallbacks {
  onSuccess: () => void;
  onError: (message: string) => void;
}

export function useAddContactMutation(accessToken: string, callbacks: UseAddContactMutationCallbacks) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (partnerUserId: string) => openDuetConversation(partnerUserId, null, accessToken),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["contacts"] });
      callbacks.onSuccess();
    },
    onError: (error) => {
      const message = error instanceof Error ? error.message : "Nie udalo sie dodac kontaktu.";
      callbacks.onError(message);
    },
  });
}
