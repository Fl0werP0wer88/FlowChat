import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { AuthTemplate } from "../../templates";
import type { AuthMode, LoginFormValues, RegisterFormValues } from "../../../types/auth";
import { LoginForm } from "./components/LoginForm";
import { RegisterForm } from "./components/RegisterForm";
import { useAuthFlow } from "../../../hooks";

interface AuthFeatureProps {
  mode: AuthMode;
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

export function AuthFeature({ mode }: AuthFeatureProps) {
  const navigate = useNavigate();
  const [loginValues, setLoginValues] = useState<LoginFormValues>(emptyLoginFormValues);
  const [registerValues, setRegisterValues] = useState<RegisterFormValues>(emptyRegisterFormValues);

  const updateLoginValue = (field: keyof LoginFormValues, value: string) =>
    setLoginValues((current) => ({ ...current, [field]: value }));

  const updateRegisterValue = (field: keyof RegisterFormValues, value: string) =>
    setRegisterValues((current) => ({ ...current, [field]: value }));

  const auth = useAuthFlow({
    mode,
    onSwitchToLogin: () => navigate("/login"),
    onSwitchToRegister: () => navigate("/register"),
    onLoginSucceeded: () => setLoginValues((current) => ({ ...current, password: "" })),
    onRegisterSucceeded: () => {
      setLoginValues((current) => ({ ...current, login: registerValues.email.trim(), password: "" }));
      setRegisterValues(emptyRegisterFormValues);
    },
  });

  if (auth.mode === "login") {
    return (
      <AuthTemplate title="FlowChat" subtitle="Zaloguj sie do FlowChat.">
        <LoginForm
          values={loginValues}
          pending={auth.pending}
          onSubmit={(event) => auth.submitLogin(event, loginValues)}
          onFieldChange={updateLoginValue}
          onSwitchToRegister={auth.switchToRegister}
        />
      </AuthTemplate>
    );
  }

  return (
    <AuthTemplate title="Rejestracja" subtitle="Utworz konto i potwierdz email.">
      <RegisterForm
        values={registerValues}
        pending={auth.pending}
        onSubmit={(event) => auth.submitRegister(event, registerValues)}
        onFieldChange={updateRegisterValue}
        onSwitchToLogin={auth.switchToLogin}
      />
    </AuthTemplate>
  );
}
