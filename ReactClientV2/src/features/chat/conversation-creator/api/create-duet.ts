import { useMutation, useQueryClient, type UseMutationOptions } from '@tanstack/react-query';
import { z } from 'zod';

import { getDuetsWithPresenceQueryOptions } from '@/features/chat/conversations/duet/api/get-duets-with-presence';
import { api } from '@/lib/api-client';

const participantSchema = z.object({
  userId: z.uuid(),
  displayName: z.string().nullable(),
  avatarUrl: z.string().nullable(),
  participantUserId: z.uuid(),
});

const createDuetResponseSchema = z.object({
  conversationId: z.uuid(),
  participants: z.array(participantSchema),
});

export const createDuetInputSchema = z.object({
  partnerUserId: z.uuid('Choose a valid conversation partner.'),
});

export type CreateDuetInput = z.infer<typeof createDuetInputSchema>;
export type CreateDuetResponse = z.infer<typeof createDuetResponseSchema>;

type CreateDuetVariables = {
  data: CreateDuetInput;
};

export async function createDuet({ data }: CreateDuetVariables): Promise<CreateDuetResponse> {
  const input = createDuetInputSchema.parse(data);
  const response = await api.put('/api/conversations/duet', input);

  return createDuetResponseSchema.parse(response.data);
}

type UseCreateDuetOptions = {
  mutationConfig?: Omit<
    UseMutationOptions<CreateDuetResponse, Error, CreateDuetVariables>,
    'mutationFn'
  >;
};

export function useCreateDuet({ mutationConfig }: UseCreateDuetOptions = {}) {
  const queryClient = useQueryClient();
  const { onSuccess, ...restConfig } = mutationConfig ?? {};

  return useMutation({
    ...restConfig,
    mutationFn: createDuet,
    onSuccess: (...args) => {
      void queryClient.invalidateQueries({
        queryKey: getDuetsWithPresenceQueryOptions().queryKey,
      });
      onSuccess?.(...args);
    },
  });
}
