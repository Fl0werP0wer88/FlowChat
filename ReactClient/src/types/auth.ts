export type AuthMode = "login" | "register";

export interface LoginFormValues {
  login: string;
  password: string;
}

export interface RegisterFormValues {
  email: string;
  friendlyUserId: string;
  password: string;
  firstName: string;
  lastName: string;
  organization: string;
}

export interface AuthSession {
  accessToken: string;
  login: string;
  expiresAtUtc: string | null;
}

export interface AuthTokenResponseDto {
  access_token?: string;
  accessToken?: string;
  expires_in?: number;
  expiresAtUtc?: string | null;
  token_type?: string;
  scope?: string;
}

export type LoginResponseDto = AuthTokenResponseDto;
export type RefreshTokenResponseDto = AuthTokenResponseDto;
