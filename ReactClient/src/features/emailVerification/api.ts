import { postJson } from "../../api/httpClient";

interface ConfirmEmailVerificationPayload {
  token: string;
}

export async function confirmEmailVerification(token: string): Promise<void> {
  await postJson<unknown, ConfirmEmailVerificationPayload>("/api/userprofiles/email-verification/confirm", {
    token: token.trim(),
  });
}
