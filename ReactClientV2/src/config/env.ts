import { z } from 'zod';

const envSchema = z.object({
  VITE_GATEWAY_API_URL: z.url().default('https://localhost:7270'),
});

const parsedEnv = envSchema.safeParse(import.meta.env);

if (!parsedEnv.success) {
  const issues = parsedEnv.error.issues
    .map((issue) => `${issue.path.join('.') || 'environment'}: ${issue.message}`)
    .join('\n');

  throw new Error(`Invalid environment configuration:\n${issues}`);
}

export const env = {
  gatewayApiUrl: parsedEnv.data.VITE_GATEWAY_API_URL.replace(/\/+$/, ''),
} as const;
