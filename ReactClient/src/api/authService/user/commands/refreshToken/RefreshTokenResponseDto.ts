export interface RefreshTokenResponseDto {
  access_token?: string;
  accessToken?: string;
  expires_in?: number;
  expiresAtUtc?: string | null;
  token_type?: string;
  scope?: string;
}
