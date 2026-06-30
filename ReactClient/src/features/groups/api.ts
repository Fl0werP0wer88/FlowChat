import { getJson, postJson } from "../../api/httpClient";

interface GroupConversationSummaryDto {
  conversationId?: string;
  ConversationId?: string;
  name?: string;
  Name?: string;
  participantCount?: number;
  ParticipantCount?: number;
}

interface GetGroupConversationsResponseDto {
  groupConversations?: GroupConversationSummaryDto[];
  GroupConversations?: GroupConversationSummaryDto[];
}

export interface GroupConversation {
  conversationId: string;
  name: string;
  participantCount: number;
}

function mapGroupConversation(dto: GroupConversationSummaryDto): GroupConversation {
  return {
    conversationId: dto.conversationId ?? dto.ConversationId ?? "",
    name: dto.name ?? dto.Name ?? "",
    participantCount: dto.participantCount ?? dto.ParticipantCount ?? 0,
  };
}

export async function fetchGroupConversations(
  accessToken: string,
  signal?: AbortSignal,
): Promise<GroupConversation[]> {
  const response = await getJson<GetGroupConversationsResponseDto>(
    "/api/conversations/group",
    { accessToken, signal },
  );

  const items = response.groupConversations ?? response.GroupConversations ?? [];
  return items.map(mapGroupConversation);
}

interface CreateGroupConversationRequestDto {
  conversationId: string;
  participantUserIds: string[];
  name: string;
}

interface CreateGroupConversationResponseDto {
  conversationId?: string;
  ConversationId?: string;
  name?: string;
  Name?: string;
}

export async function createGroupConversation(
  participantUserIds: string[],
  name: string,
  accessToken: string,
): Promise<GroupConversation> {
  const conversationId = crypto.randomUUID();

  const response = await postJson<CreateGroupConversationResponseDto, CreateGroupConversationRequestDto>(
    "/api/conversations/group",
    { conversationId, participantUserIds, name },
    { accessToken },
  );

  return {
    conversationId: response.conversationId ?? response.ConversationId ?? conversationId,
    name: response.name ?? response.Name ?? name,
    participantCount: participantUserIds.length,
  };
}
