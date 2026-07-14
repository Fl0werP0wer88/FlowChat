import type { AuthSession } from "../../types/auth";
import type { LoginResponseDto } from "./user/commands/loginUser/LoginResponseDto";
import type { RefreshTokenResponseDto } from "./user/commands/refreshToken/RefreshTokenResponseDto";

type AuthTokenResponseDto = LoginResponseDto | RefreshTokenResponseDto;

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

export function mapToAuthSession(response: AuthTokenResponseDto, login: string): AuthSession {
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
