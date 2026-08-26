import type { ReactNode } from 'react';

import { SidebarLayout } from '@/components/layouts/sidebar-layout';
import { ShowConversationsButton } from '@/features/chat/navigation/show-conversations-button';

interface ConversationCreatorLayoutProps {
  children: ReactNode;
  disabled: boolean;
  title: string;
}

//Review-0: to jest chyba niepotrzebne
export function ConversationCreatorLayout({
  children,
  disabled,
  title,
}: ConversationCreatorLayoutProps) {
  return (
    <SidebarLayout
      backAction={<ShowConversationsButton disabled={disabled} />}
      eyebrow="New conversation"
      headingId="conversation-creator-heading"
      title={title}
    >
      {children}
    </SidebarLayout>
  );
}
