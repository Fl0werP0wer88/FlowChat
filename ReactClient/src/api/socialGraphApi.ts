import { putJson } from "./httpClient";
import { isEmail } from "../utils/stringUtils";

interface AddContactPayload {
  id: string;
  userId?: string;
  friendlyUserId?: string;
  email?: string;
}

interface AddContactResponseDto {
  contactId?: string;
}

export async function addContact(
  emailOrFriendlyId: string,
  accessToken: string,
): Promise<string | null> {
  const trimmedEmailOrFriendlyId = emailOrFriendlyId.trim();
  const payload: AddContactPayload = {
    id: crypto.randomUUID(),
    ...(isEmail(trimmedEmailOrFriendlyId)
      ? { email: trimmedEmailOrFriendlyId }
      : { friendlyUserId: trimmedEmailOrFriendlyId }),
  };

  const response = await putJson<AddContactResponseDto, AddContactPayload>("/api/contacts", payload, {
    accessToken,
  });

  return response.contactId ?? null;
}

export async function addContactByUserId(
  userId: string,
  accessToken: string,
): Promise<string | null> {
  const payload: AddContactPayload = {
    id: crypto.randomUUID(),
    userId,
  };

  const response = await putJson<AddContactResponseDto, typeof payload>("/api/contacts", payload, {
    accessToken,
  });

  return response.contactId ?? null;
}
