import type { FormEvent } from "react";
import { useState } from "react";
import type { AuthMode, AuthNotice, AuthSession, LoginFormValues, RegisterFormValues } from "../../../types/auth";
import { loginWithGateway, registerWithGateway } from "../api";

interface UseAuthFlowOptions {
  onLoginSuccess: (session: AuthSession) => void;
}

const emptyLoginFormValues: LoginFormValues = {
  login: "",
  password: ""
};

const emptyRegisterFormValues: RegisterFormValues = {
  email: "",
  userName: "",
  firstName: "",
  lastName: "",
  password: ""
};

export function useAuthFlow({ onLoginSuccess }: UseAuthFlowOptions) {
  const [mode, setMode] = useState<AuthMode>("login");
  const [pending, setPending] = useState(false);
  const [notice, setNotice] = useState<AuthNotice | null>(null);
  const [loginValues, setLoginValues] = useState<LoginFormValues>(emptyLoginFormValues);
  const [registerValues, setRegisterValues] = useState<RegisterFormValues>(emptyRegisterFormValues);

  const clearNotice = () => {
    setNotice(null);
  };

  const switchToLogin = () => {
    clearNotice();
    setMode("login");
  };

  const switchToRegister = () => {
    clearNotice();
    setMode("register");
  };

  const updateLoginValue = (field: keyof LoginFormValues, value: string) => {
    setLoginValues((current) => ({
      ...current,
      [field]: value
    }));
  };

  const updateRegisterValue = (field: keyof RegisterFormValues, value: string) => {
    setRegisterValues((current) => ({
      ...current,
      [field]: value
    }));
  };

  const submitLogin = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    clearNotice();
    setPending(true);

    try {
      const session = await loginWithGateway(loginValues);
      onLoginSuccess(session);
      setLoginValues((current) => ({
        ...current,
        password: ""
      }));
      setNotice({ kind: "info", message: "Zalogowano poprawnie." });
    } catch (error) {
      const message = error instanceof Error ? error.message : "Nie udalo sie zalogowac.";
      setNotice({ kind: "error", message });
    } finally {
      setPending(false);
    }
  };

  const submitRegister = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    clearNotice();
    setPending(true);

    try {
      await registerWithGateway(registerValues);
      setLoginValues((current) => ({
        ...current,
        login: registerValues.email.trim(),
        password: ""
      }));
      setRegisterValues(emptyRegisterFormValues);
      setMode("login");
      setNotice({ kind: "info", message: "Konto utworzone. Potwierdz email, a potem zaloguj sie." });
    } catch (error) {
      const message = error instanceof Error ? error.message : "Nie udalo sie utworzyc konta.";
      setNotice({ kind: "error", message });
    } finally {
      setPending(false);
    }
  };

  return {
    mode,
    pending,
    notice,
    loginValues,
    registerValues,
    updateLoginValue,
    updateRegisterValue,
    switchToLogin,
    switchToRegister,
    submitLogin,
    submitRegister
  };
}
