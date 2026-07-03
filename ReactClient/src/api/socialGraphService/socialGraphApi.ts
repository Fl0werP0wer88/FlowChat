import { putJson } from "../httpClient";
import { isEmail } from "../../utils/stringUtils";
import type { AddContactRequest } from "./contact/commands/addContact/AddContactRequest";
import type { AddContactResponseDto } from "./contact/queries/addContact/AddContactResponseDto";

export async function addContact(
  emailOrFriendlyId: string,
  accessToken: string,
): Promise<string | null> {
  const trimmedEmailOrFriendlyId = emailOrFriendlyId.trim();
  const payload: AddContactRequest = {
    id: crypto.randomUUID(),
    ...(isEmail(trimmedEmailOrFriendlyId)
      ? { email: trimmedEmailOrFriendlyId }
      : { friendlyUserId: trimmedEmailOrFriendlyId }),
  };

  const response = await putJson<AddContactResponseDto, AddContactRequest>("/api/contacts", payload, {
    accessToken,
  });

  return response.contactId ?? null;
}

export async function addContactByUserId(
  userId: string,
  accessToken: string,
): Promise<string | null> {
  const payload: AddContactRequest = {
    id: crypto.randomUUID(),
    userId,
  };

  const response = await putJson<AddContactResponseDto, AddContactRequest>("/api/contacts", payload, {
    accessToken,
  });

  return response.contactId ?? null;
}
