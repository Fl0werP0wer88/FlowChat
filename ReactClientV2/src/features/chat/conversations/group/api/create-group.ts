import { useMutation, useQueryClient, type UseMutationOptions } from '@tanstack/react-query';
import { z } from 'zod';

import { api } from '@/lib/api-client';

import { getGroupsQueryOptions } from './get-groups';

const participantSchema = z.object({
  userId: z.uuid(),
  displayName: z.string().nullable(),
  avatarUrl: z.string().nullable(),
  participantUserId: z.uuid(),
});

const createGroupResponseSchema = z.object({
  conversationId: z.uuid(),
  name: z.string(),
  participants: z.array(participantSchema),
});

export const createGroupInputSchema = z.object({
  participantUserIds: z.array(z.uuid()).min(1, 'Choose at least one participant.'),
  name: z.string().trim().min(1, 'Enter a group name.'),
});

export type CreateGroupInput = z.infer<typeof createGroupInputSchema>;
export type CreateGroupResponse = z.infer<typeof createGroupResponseSchema>;

type CreateGroupVariables = {
  data: CreateGroupInput;
};

export async function createGroup({ data }: CreateGroupVariables): Promise<CreateGroupResponse> {
  const input = createGroupInputSchema.parse(data);
  const response = await api.post('/api/conversations/group', {
    conversationId: crypto.randomUUID(),
    participantUserIds: input.participantUserIds,
    name: input.name,
  });

  return createGroupResponseSchema.parse(response.data);
}

type UseCreateGroupOptions = {
  mutationConfig?: Omit<
    UseMutationOptions<CreateGroupResponse, Error, CreateGroupVariables>,
    'mutationFn'
  >;
};

export function useCreateGroup({ mutationConfig }: UseCreateGroupOptions = {}) {
  const queryClient = useQueryClient();
  const { onSuccess, ...restConfig } = mutationConfig ?? {};

  return useMutation({
    ...restConfig,
    mutationFn: createGroup,
    onSuccess: (...args) => {
      void queryClient.invalidateQueries({
        queryKey: getGroupsQueryOptions().queryKey,
      });
      onSuccess?.(...args);
    },
  });
}
