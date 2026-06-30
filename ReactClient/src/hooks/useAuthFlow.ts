import type { FormEvent } from "react";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuthStore } from "../store/authStore";
import type { AuthMode, AuthNotice, LoginFormValues, RegisterFormValues } from "../types/auth";
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
  const [notice, setNotice] = useState<AuthNotice | null>(null);
  const [loginValues, setLoginValues] = useState<LoginFormValues>(emptyLoginFormValues);
  const [registerValues, setRegisterValues] = useState<RegisterFormValues>(emptyRegisterFormValues);

  const clearNotice = () => setNotice(null);

  const switchToLogin = () => {
    clearNotice();
    onSwitchToLogin();
  };

  const switchToRegister = () => {
    clearNotice();
    onSwitchToRegister();
  };

  const updateLoginValue = (field: keyof LoginFormValues, value: string) => {
    setLoginValues((current) => ({ ...current, [field]: value }));
  };

  const updateRegisterValue = (field: keyof RegisterFormValues, value: string) => {
    setRegisterValues((current) => ({ ...current, [field]: value }));
  };

  const loginMutation = useLoginMutation({
    onSuccess: (session) => {
      signIn(session);
      setLoginValues((current) => ({ ...current, password: "" }));
      setNotice({ kind: "info", message: "Zalogowano poprawnie." });
      navigate("/chat", { replace: true });
    },
    onError: (message) => setNotice({ kind: "error", message }),
  });

  const registerMutation = useRegisterMutation({
    onSuccess: () => {
      setLoginValues((current) => ({
        ...current,
        login: registerValues.email.trim(),
        password: "",
      }));
      setRegisterValues(emptyRegisterFormValues);
      setNotice({ kind: "info", message: "Konto utworzone. Potwierdz email, a potem zaloguj sie." });
      onSwitchToLogin();
    },
    onError: (message) => setNotice({ kind: "error", message }),
  });

  const submitLogin = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    clearNotice();
    loginMutation.mutate(loginValues);
  };

  const submitRegister = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    clearNotice();
    registerMutation.mutate(registerValues);
  };

  return {
    mode,
    pending: loginMutation.isPending || registerMutation.isPending,
    notice,
    loginValues,
    registerValues,
    updateLoginValue,
    updateRegisterValue,
    switchToLogin,
    switchToRegister,
    submitLogin,
    submitRegister,
  };
}
