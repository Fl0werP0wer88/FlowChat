import { postForm, putJson } from "../httpClient";
import { mapToAuthSession } from "./mappers";
import type { AuthSession, LoginFormValues, RegisterFormValues } from "../../types/auth";
import type { LoginUserRequest } from "./user/commands/loginUser/LoginUserRequest";
import type { LoginResponseDto } from "./user/commands/loginUser/LoginResponseDto";
import type { RefreshTokenRequest } from "./user/commands/refreshToken/RefreshTokenRequest";
import type { RefreshTokenResponseDto } from "./user/commands/refreshToken/RefreshTokenResponseDto";
import type { RegisterUserRequest } from "./user/commands/registerUser/RegisterUserRequest";

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
