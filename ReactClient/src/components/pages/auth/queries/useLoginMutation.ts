import { useMutation } from "@tanstack/react-query";
import type { AuthSession } from "../../../../types/auth";
import type { LoginFormValues } from "../../../../types/auth";
import { loginUser } from "../../../../api/authApi";

interface UseLoginMutationCallbacks {
  onSuccess: (session: AuthSession) => void;
  onError: (message: string) => void;
}

export function useLoginMutation(callbacks: UseLoginMutationCallbacks) {
  return useMutation({
    mutationFn: (values: LoginFormValues) => loginUser(values),
    onSuccess: callbacks.onSuccess,
    onError: (error) => {
      callbacks.onError(error instanceof Error ? error.message : "Nie udalo sie zalogowac.");
    },
  });
}
