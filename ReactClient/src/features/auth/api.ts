import { postJson } from "../../api/httpClient";
import type {
  AuthSession,
  AuthTokenResponseDto,
  LoginFormValues,
  LoginResponseDto,
  RefreshTokenResponseDto,
  RegisterFormValues,
} from "../../types/auth";

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

interface RefreshTokenPayload {
  accessToken: string;
  refreshToken: string;
}

function mapToAuthSession(response: AuthTokenResponseDto, login: string): AuthSession {
  const accessToken = response.accessToken ?? response.AccessToken;
  if (!accessToken) {
    throw new Error("Authentication response does not contain access token.");
  }

  const refreshToken = response.refreshToken ?? response.RefreshToken;
  if (!refreshToken) {
    throw new Error("Authentication response does not contain refresh token.");
  }

  return {
    accessToken,
    login,
    expiresAtUtc: response.expiresAtUtc ?? response.ExpiresAtUtc ?? null,
    refreshToken,
    refreshTokenExpiresAtUtc: response.refreshTokenExpiresAtUtc ?? response.RefreshTokenExpiresAtUtc ?? null,
  };
}

export async function loginUser(values: LoginFormValues): Promise<AuthSession> {
  const response = await postJson<LoginResponseDto, LoginPayload>("/api/users/login", {
    login: values.login.trim(),
    password: values.password,
  });

  return mapToAuthSession(response, values.login.trim());
}

export async function refreshUserSession(
  session: Pick<AuthSession, "accessToken" | "refreshToken" | "login">,
): Promise<AuthSession> {
  const response = await postJson<RefreshTokenResponseDto, RefreshTokenPayload>("/api/users/refresh-token", {
    accessToken: session.accessToken,
    refreshToken: session.refreshToken,
  });

  return mapToAuthSession(response, session.login);
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
