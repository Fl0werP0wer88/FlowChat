import { useMutation, useQueryClient } from "@tanstack/react-query";
import { addContact } from "../api";

interface UseAddContactMutationCallbacks {
  onSuccess: () => void;
  onError: (message: string) => void;
}

export function useAddContactMutation(accessToken: string, callbacks: UseAddContactMutationCallbacks) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (lookupValue: string) => addContact(lookupValue, accessToken),
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
