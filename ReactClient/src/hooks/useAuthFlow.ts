import type { FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { toast } from "sonner";
import { useAuthStore } from "../store/authStore";
import type { AuthMode, LoginFormValues, RegisterFormValues } from "../types/auth";
import { useLoginMutation } from "./mutations/useLoginMutation";
import { useRegisterMutation } from "./mutations/useRegisterMutation";

interface UseAuthFlowOptions {
  mode: AuthMode;
  onSwitchToLogin: () => void;
  onSwitchToRegister: () => void;
  onLoginSucceeded: () => void;
  onRegisterSucceeded: () => void;
}

export function useAuthFlow({ mode, onSwitchToLogin, onSwitchToRegister, onLoginSucceeded, onRegisterSucceeded }: UseAuthFlowOptions) {
  const signIn = useAuthStore((s) => s.signIn);
  const navigate = useNavigate();

  const loginMutation = useLoginMutation({
    onSuccess: (session) => {
      signIn(session);
      onLoginSucceeded();
      toast.success("Zalogowano poprawnie.");
      navigate("/chat", { replace: true });
    },
    onError: (message) => toast.error(message),
  });

  const registerMutation = useRegisterMutation({
    onSuccess: () => {
      onRegisterSucceeded();
      toast.success("Konto utworzone. Potwierdz email, a potem zaloguj sie.");
      onSwitchToLogin();
    },
    onError: (message) => toast.error(message),
  });

  const submitLogin = (event: FormEvent<HTMLFormElement>, values: LoginFormValues) => {
    event.preventDefault();
    loginMutation.mutate(values);
  };

  const submitRegister = (event: FormEvent<HTMLFormElement>, values: RegisterFormValues) => {
    event.preventDefault();
    registerMutation.mutate(values);
  };

  return {
    mode,
    pending: loginMutation.isPending || registerMutation.isPending,
    switchToLogin: onSwitchToLogin,
    switchToRegister: onSwitchToRegister,
    submitLogin,
    submitRegister,
  };
}
