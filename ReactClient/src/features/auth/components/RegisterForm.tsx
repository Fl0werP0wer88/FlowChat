import type { FormEvent } from "react";
import { Input } from "../../../components/atoms/Input";
import { AlertMessage } from "../../../components/molecules/AlertMessage";
import { FormField } from "../../../components/molecules/FormField";
import { AuthActions } from "../../../components/organisms/AuthActions";
import type { AuthNotice, RegisterFormValues } from "../../../types/auth";

interface RegisterFormProps {
  values: RegisterFormValues;
  pending: boolean;
  notice: AuthNotice | null;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  onFieldChange: (field: keyof RegisterFormValues, value: string) => void;
  onSwitchToLogin: () => void;
}

export function RegisterForm({
  values,
  pending,
  notice,
  onSubmit,
  onFieldChange,
  onSwitchToLogin,
}: RegisterFormProps) {
  return (
    <form className="auth-form" onSubmit={onSubmit}>
      <FormField label="Email" htmlFor="registerEmail">
        <Input
          id="registerEmail"
          type="email"
          autoComplete="email"
          required
          value={values.email}
          onChange={(event) => onFieldChange("email", event.target.value)}
        />
      </FormField>

      <FormField label="FriendlyUserId" htmlFor="registerFriendlyUserId">
        <Input
          id="registerFriendlyUserId"
          autoComplete="username"
          required
          value={values.friendlyUserId}
          onChange={(event) => onFieldChange("friendlyUserId", event.target.value)}
        />
      </FormField>

      <FormField label="Haslo" htmlFor="registerPassword" hint="Minimum 8 znakow.">
        <Input
          id="registerPassword"
          type="password"
          autoComplete="new-password"
          minLength={8}
          required
          value={values.password}
          onChange={(event) => onFieldChange("password", event.target.value)}
        />
      </FormField>

      <AlertMessage notice={notice} />

      <AuthActions
        pending={pending}
        submitLabel="Register"
        pendingSubmitLabel="Rejestrowanie..."
        secondaryLabel="Powrot do logowania"
        onSecondaryClick={onSwitchToLogin}
      />
    </form>
  );
}
