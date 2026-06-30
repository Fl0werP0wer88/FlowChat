import { useMutation, useQueryClient } from "@tanstack/react-query";
import { addContactByUserId } from "../../../api/socialGraphApi";

interface UseAddContactByUserIdMutationCallbacks {
  onSuccess: () => void;
  onError: (message: string) => void;
}

export function useAddContactByUserIdMutation(
  accessToken: string,
  callbacks: UseAddContactByUserIdMutationCallbacks,
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (userId: string) => addContactByUserId(userId, accessToken),
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
