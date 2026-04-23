import { postForm, postJson } from "../../api/httpClient";
import type {
  AuthSession,
  AuthTokenResponseDto,
  LoginFormValues,
  LoginResponseDto,
  RefreshTokenResponseDto,
  RegisterFormValues,
} from "../../types/auth";

interface LoginPayload {
  grant_type: string;
  username: string;
  password: string;
}

interface RegisterPayload {
  id: string;
  email: string;
  friendlyUserId: string;
  password: string;
  firstName?: string;
  lastName?: string;
  organization?: string;
}

interface RefreshTokenPayload {
  grant_type: string;
  refresh_token: string;
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

  const refreshToken = response.refresh_token ?? response.refreshToken ?? response.RefreshToken;
  if (!refreshToken) {
    throw new Error("Authentication response does not contain refresh token.");
  }

  return {
    accessToken,
    login,
    expiresAtUtc: resolveExpiresAtUtc(response),
    refreshToken,
    refreshTokenExpiresAtUtc: response.refreshTokenExpiresAtUtc ?? response.RefreshTokenExpiresAtUtc ?? null,
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

export async function refreshUserSession(
  session: Pick<AuthSession, "accessToken" | "refreshToken" | "login">,
): Promise<AuthSession> {
  const response = await postForm<RefreshTokenResponseDto>("/api/users/refresh-token", {
    grant_type: "refresh_token",
    refresh_token: session.refreshToken,
  });

  return mapToAuthSession(response, session.login);
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

  await postJson<unknown, RegisterPayload>("/api/users", payload);
}
