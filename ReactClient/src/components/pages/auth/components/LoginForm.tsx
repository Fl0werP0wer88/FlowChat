import type { FormEvent } from "react";
import { Input } from "../../../UI/atoms/Input";
import { FormField } from "../../../UI/molecules/FormField";
import { AuthActions } from "../../../UI/organisms/AuthActions";
import type { LoginFormValues } from "../../../../types/auth";

interface LoginFormProps {
  values: LoginFormValues;
  pending: boolean;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  onFieldChange: (field: keyof LoginFormValues, value: string) => void;
  onSwitchToRegister: () => void;
}

export function LoginForm({
  values,
  pending,
  onSubmit,
  onFieldChange,
  onSwitchToRegister,
}: LoginFormProps) {
  return (
    <form className="auth-form" onSubmit={onSubmit}>
      <FormField label="Email lub FriendlyUserId" htmlFor="loginField">
        <Input
          id="loginField"
          autoComplete="username"
          required
          value={values.login}
          onChange={(event) => onFieldChange("login", event.target.value)}
        />
      </FormField>

      <FormField label="Haslo" htmlFor="passwordField">
        <Input
          id="passwordField"
          type="password"
          autoComplete="current-password"
          required
          value={values.password}
          onChange={(event) => onFieldChange("password", event.target.value)}
        />
      </FormField>

      <AuthActions
        pending={pending}
        submitLabel="Login"
        pendingSubmitLabel="Logowanie..."
        secondaryLabel="Rejestracja"
        onSecondaryClick={onSwitchToRegister}
      />
    </form>
  );
}
