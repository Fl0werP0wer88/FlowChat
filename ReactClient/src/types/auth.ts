import type { NoticeKind } from "./common";

export type AuthMode = "login" | "register";

export interface LoginFormValues {
  login: string;
  password: string;
}

export interface RegisterFormValues {
  email: string;
  userName: string;
  firstName: string;
  lastName: string;
  password: string;
}

export interface AuthSession {
  accessToken: string;
  login: string;
  expiresAtUtc: string | null;
}

export interface AuthNotice {
  kind: NoticeKind;
  message: string;
}

export interface LoginResponseDto {
  isSuccess?: boolean;
  IsSuccess?: boolean;
  accessToken?: string;
  AccessToken?: string;
  expiresAtUtc?: string | null;
  ExpiresAtUtc?: string | null;
}
