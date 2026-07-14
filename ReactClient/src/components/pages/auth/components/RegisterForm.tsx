import type { FormEvent } from "react";
import { Input } from "../../../UI/atoms/Input";
import { FormField } from "../../../UI/molecules/FormField";
import { AuthActions } from "../../../UI/organisms/AuthActions";
import type { RegisterFormValues } from "../../../../types/auth";

interface RegisterFormProps {
  values: RegisterFormValues;
  pending: boolean;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  onFieldChange: (field: keyof RegisterFormValues, value: string) => void;
  onSwitchToLogin: () => void;
}

export function RegisterForm({
  values,
  pending,
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

      <FormField label="Imie" htmlFor="registerFirstName">
        <Input
          id="registerFirstName"
          autoComplete="given-name"
          value={values.firstName}
          onChange={(event) => onFieldChange("firstName", event.target.value)}
        />
      </FormField>

      <FormField label="Nazwisko" htmlFor="registerLastName">
        <Input
          id="registerLastName"
          autoComplete="family-name"
          value={values.lastName}
          onChange={(event) => onFieldChange("lastName", event.target.value)}
        />
      </FormField>

      <FormField label="Organizacja" htmlFor="registerOrganization">
        <Input
          id="registerOrganization"
          autoComplete="organization"
          value={values.organization}
          onChange={(event) => onFieldChange("organization", event.target.value)}
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
