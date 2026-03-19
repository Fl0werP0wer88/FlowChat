import { postJson } from "../../api/httpClient";
import type { AuthSession, LoginFormValues, LoginResponseDto, RegisterFormValues } from "../../types/auth";

interface LoginPayload {
  login: string;
  password: string;
}

interface RegisterPayload {
  email: string;
  userName: string;
  password: string;
  firstName?: string;
  lastName?: string;
}

export async function loginUser(values: LoginFormValues): Promise<AuthSession> {
  const response = await postJson<LoginResponseDto, LoginPayload>("/api/users/login", {
    login: values.login.trim(),
    password: values.password,
  });

  const accessToken = response.accessToken ?? response.AccessToken;
  if (!accessToken) {
    throw new Error("Login response does not contain access token.");
  }

  return {
    accessToken,
    login: values.login.trim(),
    expiresAtUtc: response.expiresAtUtc ?? response.ExpiresAtUtc ?? null,
  };
}

export async function registerUser(values: RegisterFormValues): Promise<void> {
  const payload: RegisterPayload = {
    email: values.email.trim(),
    userName: values.userName.trim(),
    password: values.password,
  };

  if (values.firstName.trim().length > 0) {
    payload.firstName = values.firstName.trim();
  }

  if (values.lastName.trim().length > 0) {
    payload.lastName = values.lastName.trim();
  }

  await postJson<unknown, RegisterPayload>("/api/users", payload);
}
