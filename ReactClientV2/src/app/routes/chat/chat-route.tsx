import { LogOut, MessageCircleMore } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';

import { AppLayout } from '@/components/layouts/app-layout';
import { Button } from '@/components/ui/button';
import { paths } from '@/config/paths';
import { ChatHeader } from '@/features/chat/components/chat-header';
import { Conversations } from '@/features/chat/conversations/components/conversations';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { logoutSession } from '@/lib/auth-session';
import { useAuthStore } from '@/stores/auth-store';

export function Component() {
  useDocumentTitle('Chat');
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.session?.user);

  const logout = async () => {
    try {
      await logoutSession();
    } catch {
      toast.error(
        'The server could not clear the refresh cookie. You have been signed out locally.',
      );
    } finally {
      navigate(paths.auth.login.getHref(), { replace: true });
    }
  };

  return (
    <AppLayout
      contentLabel="Active conversation"
      header={
        <ChatHeader
          actions={
            <>
              <span className="hidden max-w-48 truncate text-sm font-semibold text-slate-500 sm:block">
                {user?.friendlyUserId ?? 'FlowChat user'}
              </span>
              <Button type="button" variant="ghost" onClick={() => void logout()}>
                <LogOut className="size-4" aria-hidden="true" />
                <span className="hidden sm:inline">Sign out</span>
              </Button>
            </>
          }
        />
      }
      sidebar={
        <Conversations>
          <div className="grid min-h-48 place-items-center px-4 py-8 text-center">
            <div className="grid max-w-56 justify-items-center gap-3">
              <span className="grid size-11 place-items-center rounded-full bg-blue-50 text-blue-700">
                <MessageCircleMore className="size-5" aria-hidden="true" />
              </span>
              <div className="grid gap-3">
                <h2 className="m-0 font-display text-base font-semibold text-slate-900">
                  No conversations yet
                </h2>
                <p className="m-0 text-sm leading-6 text-slate-500">
                  Your recent conversations will appear here.
                </p>
              </div>
            </div>
          </div>
        </Conversations>
      }
    >
      <div className="grid h-full min-h-[32rem] place-items-center px-6 py-16 text-center">
        <div className="grid max-w-md justify-items-center gap-5">
          <span className="grid size-16 place-items-center rounded-full bg-blue-50 text-blue-700 ring-8 ring-blue-50/60">
            <MessageCircleMore className="size-7" aria-hidden="true" />
          </span>
          <div className="grid gap-3">
            <p className="m-0 text-xs font-extrabold uppercase tracking-[0.18em] text-blue-700">
              Active conversation
            </p>
            <h2 className="m-0 font-display text-3xl font-semibold tracking-[-0.04em] text-slate-950 sm:text-4xl">
              Choose a conversation
            </h2>
            <p className="m-0 text-base leading-7 text-slate-600">
              Select a conversation from the list to open its messages here.
            </p>
          </div>
        </div>
      </div>
    </AppLayout>
  );
}
