import { ArrowRight, LogOut, MessageCircleMore } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';

import { BrandMark } from '@/components/brand/brand-mark';
import { Button } from '@/components/ui/button';
import { paths } from '@/config/paths';
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
    <main className="min-h-svh bg-[#f4f7fb] px-5 py-6 sm:px-8 sm:py-8">
      <div className="mx-auto grid min-h-[calc(100svh-4rem)] max-w-6xl grid-rows-[auto_1fr] overflow-hidden rounded-[2rem] border border-slate-200 bg-white shadow-[0_24px_90px_-58px_rgba(17,40,90,0.7)]">
        <header className="flex items-center justify-between gap-4 border-b border-slate-100 px-5 py-4 sm:px-8">
          <div className="flex items-center gap-3 font-display font-bold tracking-tight text-slate-950">
            <BrandMark className="size-8" />
            <span>FlowChat</span>
          </div>
          <Button type="button" variant="ghost" onClick={() => void logout()}>
            <LogOut className="size-4" aria-hidden="true" />
            Sign out
          </Button>
        </header>

        <section className="grid place-items-center px-6 py-16 text-center">
          <div className="grid max-w-xl justify-items-center gap-6">
            <span className="grid size-16 place-items-center rounded-full bg-blue-50 text-blue-700 ring-8 ring-blue-50/60">
              <MessageCircleMore className="size-7" aria-hidden="true" />
            </span>
            <div className="grid gap-3">
              <p className="m-0 text-xs font-extrabold uppercase tracking-[0.18em] text-blue-700">
                Signed in as {user?.friendlyUserId ?? 'FlowChat user'}
              </p>
              <h1 className="m-0 font-display text-4xl font-semibold tracking-[-0.045em] text-slate-950 sm:text-5xl">
                Your chat workspace is next.
              </h1>
              <p className="m-0 text-base leading-7 text-slate-600">
                Authentication is ready. Conversations, contacts, and realtime messaging will arrive
                in the next migration stage.
              </p>
            </div>
            <span className="inline-flex items-center gap-2 text-sm font-semibold text-slate-500">
              Migration stage one complete
              <ArrowRight className="size-4" aria-hidden="true" />
            </span>
          </div>
        </section>
      </div>
    </main>
  );
}
