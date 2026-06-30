import { postForm, putJson } from "./httpClient";
import type {
  AuthSession,
  AuthTokenResponseDto,
  LoginFormValues,
  LoginResponseDto,
  RefreshTokenResponseDto,
  RegisterFormValues,
} from "../types/auth";

interface RegisterPayload {
  id: string;
  email: string;
  friendlyUserId: string;
  password: string;
  firstName?: string;
  lastName?: string;
  organization?: string;
}

function resolveExpiresAtUtc(response: AuthTokenResponseDto): string | null {
  const explicitExpiration = response.expiresAtUtc ?? response.ExpiresAtUtc ?? null;
  if (explicitExpiration) {
    return explicitExpiration;
  }

  if (typeof response.expires_in !== "number" || !Number.isFinite(response.expires_in)) {
    return null;
  }

  return new Date(Date.now() + (response.expires_in * 1000)).toISOString();
}

function mapToAuthSession(response: AuthTokenResponseDto, login: string): AuthSession {
  const accessToken = response.access_token ?? response.accessToken ?? response.AccessToken;
  if (!accessToken) {
    throw new Error("Authentication response does not contain access token.");
  }

  return {
    accessToken,
    login,
    expiresAtUtc: resolveExpiresAtUtc(response),
  };
}

export async function loginUser(values: LoginFormValues): Promise<AuthSession> {
  const response = await postForm<LoginResponseDto>("/api/users/login", {
    grant_type: "password",
    username: values.login.trim(),
    password: values.password,
  });

  return mapToAuthSession(response, values.login.trim());
}

export async function refreshUserSession(login: string): Promise<AuthSession> {
  // refresh_token is sent automatically as an HttpOnly cookie
  const response = await postForm<RefreshTokenResponseDto>("/api/users/refresh-token", {
    grant_type: "refresh_token",
  });

  return mapToAuthSession(response, login);
}

export async function logoutUser(): Promise<void> {
  // Asks the server to clear the HttpOnly refresh token cookie
  await postForm<unknown>("/api/users/logout", {});
}

export async function registerUser(values: RegisterFormValues): Promise<void> {
  const payload: RegisterPayload = {
    id: crypto.randomUUID(),
    email: values.email.trim(),
    friendlyUserId: values.friendlyUserId.trim(),
    password: values.password,
    firstName: values.firstName.trim() || undefined,
    lastName: values.lastName.trim() || undefined,
    organization: values.organization.trim() || undefined,
  };

  await putJson<unknown, RegisterPayload>("/api/users", payload);
}
