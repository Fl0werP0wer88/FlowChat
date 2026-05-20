import { getJson } from "../../../api/httpClient";

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
