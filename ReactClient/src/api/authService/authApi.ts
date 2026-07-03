import { postForm, putJson } from "../httpClient";
import type {
  AuthSession,
  AuthTokenResponseDto,
  LoginFormValues,
  LoginResponseDto,
  RefreshTokenResponseDto,
  RegisterFormValues,
} from "../../types/auth";
import type { LoginUserRequest } from "./user/commands/loginUser/LoginUserRequest";
import type { RefreshTokenRequest } from "./user/commands/refreshToken/RefreshTokenRequest";
import type { RegisterUserRequest } from "./user/commands/registerUser/RegisterUserRequest";

function resolveExpiresAtUtc(response: AuthTokenResponseDto): string | null {
  const explicitExpiration = response.expiresAtUtc ?? null;
  if (explicitExpiration) {
    return explicitExpiration;
  }

  if (typeof response.expires_in !== "number" || !Number.isFinite(response.expires_in)) {
    return null;
  }

  return new Date(Date.now() + (response.expires_in * 1000)).toISOString();
}

function mapToAuthSession(response: AuthTokenResponseDto, login: string): AuthSession {
  const accessToken = response.access_token ?? response.accessToken;
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
  const payload: LoginUserRequest = {
    grant_type: "password",
    username: values.login.trim(),
    password: values.password,
  };

  const response = await postForm<LoginResponseDto>("/api/users/login", payload);

  return mapToAuthSession(response, values.login.trim());
}

export async function refreshUserSession(login: string): Promise<AuthSession> {
  // refresh_token is sent automatically as an HttpOnly cookie
  const payload: RefreshTokenRequest = {
    grant_type: "refresh_token",
  };

  const response = await postForm<RefreshTokenResponseDto>("/api/users/refresh-token", payload);

  return mapToAuthSession(response, login);
}

export async function logoutUser(): Promise<void> {
  // Asks the server to clear the HttpOnly refresh token cookie
  await postForm<unknown>("/api/users/logout", {});
}

export async function registerUser(values: RegisterFormValues): Promise<void> {
  const payload: RegisterUserRequest = {
    id: crypto.randomUUID(),
    email: values.email.trim(),
    friendlyUserId: values.friendlyUserId.trim(),
    password: values.password,
    firstName: values.firstName.trim() || undefined,
    lastName: values.lastName.trim() || undefined,
    organization: values.organization.trim() || undefined,
  };

  await putJson<unknown, RegisterUserRequest>("/api/users", payload);
}
