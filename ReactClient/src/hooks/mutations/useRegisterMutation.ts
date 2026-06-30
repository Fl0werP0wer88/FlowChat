import { useMutation } from "@tanstack/react-query";
import type { RegisterFormValues } from "../../types/auth";
import { registerUser } from "../../api/authApi";

interface UseRegisterMutationCallbacks {
  onSuccess: () => void;
  onError: (message: string) => void;
}

export function useRegisterMutation(callbacks: UseRegisterMutationCallbacks) {
  return useMutation({
    mutationFn: (values: RegisterFormValues) => registerUser(values),
    onSuccess: callbacks.onSuccess,
    onError: (error) => {
      callbacks.onError(error instanceof Error ? error.message : "Nie udalo sie utworzyc konta.");
    },
  });
}
