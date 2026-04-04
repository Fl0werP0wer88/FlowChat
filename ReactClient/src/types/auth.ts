import type { NoticeKind } from "./common";

export type AuthMode = "login" | "register";

export interface LoginFormValues {
  login: string;
  password: string;
}

export interface RegisterFormValues {
  email: string;
  friendlyUserId: string;
  password: string;
}

export interface AuthSession {
  accessToken: string;
  login: string;
  expiresAtUtc: string | null;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string | null;
}

export interface AuthNotice {
  kind: NoticeKind;
  message: string;
}

export interface AuthTokenResponseDto {
  access_token?: string;
  accessToken?: string;
  AccessToken?: string;
  expires_in?: number;
  expiresAtUtc?: string | null;
  ExpiresAtUtc?: string | null;
  refresh_token?: string;
  refreshToken?: string;
  RefreshToken?: string;
  token_type?: string;
  scope?: string;
  refreshTokenExpiresAtUtc?: string | null;
  RefreshTokenExpiresAtUtc?: string | null;
}

export type LoginResponseDto = AuthTokenResponseDto;
export type RefreshTokenResponseDto = AuthTokenResponseDto;
