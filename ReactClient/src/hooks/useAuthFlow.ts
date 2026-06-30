import type { FormEvent } from "react";
import { useState } from "react";
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
}

const emptyLoginFormValues: LoginFormValues = {
  login: "",
  password: "",
};

const emptyRegisterFormValues: RegisterFormValues = {
  email: "",
  friendlyUserId: "",
  password: "",
  firstName: "",
  lastName: "",
  organization: "",
};

export function useAuthFlow({ mode, onSwitchToLogin, onSwitchToRegister }: UseAuthFlowOptions) {
  const signIn = useAuthStore((s) => s.signIn);
  const navigate = useNavigate();
  const [loginValues, setLoginValues] = useState<LoginFormValues>(emptyLoginFormValues);
  const [registerValues, setRegisterValues] = useState<RegisterFormValues>(emptyRegisterFormValues);

  const loginMutation = useLoginMutation({
    onSuccess: (session) => {
      signIn(session);
      setLoginValues((current) => ({ ...current, password: "" }));
      toast.success("Zalogowano poprawnie.");
      navigate("/chat", { replace: true });
    },
    onError: (message) => toast.error(message),
  });

  const registerMutation = useRegisterMutation({
    onSuccess: () => {
      setLoginValues((current) => ({
        ...current,
        login: registerValues.email.trim(),
        password: "",
      }));
      setRegisterValues(emptyRegisterFormValues);
      toast.success("Konto utworzone. Potwierdz email, a potem zaloguj sie.");
      onSwitchToLogin();
    },
    onError: (message) => toast.error(message),
  });

  const submitLogin = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    loginMutation.mutate(loginValues);
  };

  const submitRegister = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    registerMutation.mutate(registerValues);
  };

  return {
    mode,
    pending: loginMutation.isPending || registerMutation.isPending,
    loginValues,
    registerValues,
    updateLoginValue: (field: keyof LoginFormValues, value: string) =>
      setLoginValues((current) => ({ ...current, [field]: value })),
    updateRegisterValue: (field: keyof RegisterFormValues, value: string) =>
      setRegisterValues((current) => ({ ...current, [field]: value })),
    switchToLogin: onSwitchToLogin,
    switchToRegister: onSwitchToRegister,
    submitLogin,
    submitRegister,
  };
}
