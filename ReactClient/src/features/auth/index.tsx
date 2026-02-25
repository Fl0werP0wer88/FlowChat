import { AuthTemplate } from "../../components/templates/AuthTemplate";
import type { AuthSession } from "../../types/auth";
import { LoginForm } from "./components/LoginForm";
import { RegisterForm } from "./components/RegisterForm";
import { useAuthFlow } from "./hooks/useAuthFlow";

interface AuthFeatureProps {
  onLoginSuccess: (session: AuthSession) => void;
}

export function AuthFeature({ onLoginSuccess }: AuthFeatureProps) {
  const auth = useAuthFlow({ onLoginSuccess });

  if (auth.mode === "login") {
    return (
      <AuthTemplate title="FlowChat" subtitle="Zaloguj sie przez Gateway API.">
        <LoginForm
          values={auth.loginValues}
          pending={auth.pending}
          notice={auth.notice}
          onSubmit={auth.submitLogin}
          onFieldChange={auth.updateLoginValue}
          onSwitchToRegister={auth.switchToRegister}
        />
      </AuthTemplate>
    );
  }

  return (
    <AuthTemplate title="Rejestracja" subtitle="Utworz konto i potwierdz email.">
      <RegisterForm
        values={auth.registerValues}
        pending={auth.pending}
        notice={auth.notice}
        onSubmit={auth.submitRegister}
        onFieldChange={auth.updateRegisterValue}
        onSwitchToLogin={auth.switchToLogin}
      />
    </AuthTemplate>
  );
}
