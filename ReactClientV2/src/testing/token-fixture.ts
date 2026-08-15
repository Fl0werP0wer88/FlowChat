interface TokenFixtureOptions {
  subject?: string;
  email?: string;
  friendlyUserId?: string;
  expiresInSeconds?: number;
}

function encode(value: object): string {
  return Buffer.from(JSON.stringify(value)).toString('base64url');
}

export function createAccessToken({
  subject = '82b0c1ca-d57a-43b0-a871-39a97056af89',
  email = 'alex@example.com',
  friendlyUserId = 'alex.morgan',
  expiresInSeconds = 3_600,
}: TokenFixtureOptions = {}): string {
  return [
    encode({ alg: 'none', typ: 'JWT' }),
    encode({
      sub: subject,
      email,
      preferred_username: friendlyUserId,
      unique_name: friendlyUserId,
      exp: Math.floor(Date.now() / 1_000) + expiresInSeconds,
      role: ['User'],
    }),
    'test-signature',
  ].join('.');
}
